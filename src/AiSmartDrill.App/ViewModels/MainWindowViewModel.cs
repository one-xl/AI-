using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using AiSmartDrill.App;
using AiSmartDrill.App.CareerPath;
using Microsoft.Win32;
using AiSmartDrill.App.Drill.Ai;
using AiSmartDrill.App.Drill.Ai.Config;
using AiSmartDrill.App.Drill.Grading;
using AiSmartDrill.App.Drill.Import;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.ViewModels;

/// <summary>
/// 主窗口视图模型：聚合题库管理、考试引擎、错题闭环与 AI 调用链。
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// 考试会话成功启动后触发，供主窗口切换到「考试 / 刷题」标签页。
    /// </summary>
    public event EventHandler? ExamStarted;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAiTutorService _aiTutor;
    private readonly IQuestionRecommendationService _recommendation;
    private readonly IStudyPlanService _studyPlan;
    private readonly QuestionImportService _importService;
    private readonly IQuestionBankAiGenerationService _questionBankAi;
    private readonly IExamQuestionAiExplainService _examQuestionAi;
    private readonly AiCallTrace _aiTrace;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly DoubaoModelConfig _doubaoModelConfig;

    /// <summary>
    /// 考试计时器：每秒递减剩余秒数；到达 0 时自动交卷。
    /// </summary>
    private readonly DispatcherTimer _examTimer;

    /// <summary>
    /// 每分钟刷新日出日落时间线，并在开启自动策略时校正主题。
    /// </summary>
    private readonly DispatcherTimer _sunScheduleTimer;

    /// <summary>
    /// 每秒更新顶栏「当前时间」显示（与色条同区展示）。
    /// </summary>
    private readonly DispatcherTimer _headerClockTimer;

    /// <summary>
    /// 当前考试会话 Id（一次组卷对应一个 Guid）。
    /// </summary>
    private Guid _examSessionId = Guid.Empty;

    /// <summary>
    /// 当前考试题目队列（内存态）。
    /// </summary>
    private readonly List<Question> _examQuestions = new();

    /// <summary>
    /// 防止倒计时与用户操作并发触发重复交卷。
    /// </summary>
    private int _submitGate;

    /// <summary>
    /// 保护 <see cref="_aiRequestCts"/> 的分配与取消，供「取消 AI 请求」与并发 Ark 调用协调。
    /// </summary>
    private readonly object _aiRequestLock = new();

    private CancellationTokenSource? _aiRequestCts;

    /// <summary>
    /// 最近一次交卷产生的错题上下文（供展示与兼容；AI 解析以错题本勾选题为准）。
    /// </summary>
    private List<WrongQuestionInsightDto> _pendingWrongInsightsForAi = new();

    /// <summary>
    /// 短时记忆：勾选题目的 Id，切换领域筛选后仍保留勾选意图。
    /// </summary>
    private readonly HashSet<long> _wrongBookAnalysisSelectionIds = new();

    /// <summary>
    /// 当前考试是否来自「错题再练」；交卷后为本次卷内题目写入「已重做」时间。
    /// </summary>
    private bool _examFromWrongBookRedoSession;

    /// <summary>
    /// 当前题目开始作答时间（用于估算 DurationMs）。
    /// </summary>
    private DateTime _questionStartedAtUtc = DateTime.UtcNow;

    /// <summary>
    /// 单次调用大模型时的出题数量上限（须低于服务内硬上限），减小单次 JSON 体积与超时风险。
    /// </summary>
    private const int BankAiGenerationChunkSize = 6;

    /// <summary>
    /// 多批次合计生成题量上限，防止误输入导致长时间占用。
    /// </summary>
    private const int BankAiGenerationMaxTotal = 96;
    private const int CareerPathMissingQuestionDefaultCount = 20;

    /// <summary>
    /// 复用题库 AI 批量生成核心流程时的运行参数。
    /// </summary>
    private sealed class BankAiGenerationSessionOptions
    {
        /// <summary>
        /// 获取或设置目标领域。
        /// </summary>
        public required QuestionDomain Domain { get; init; }

        /// <summary>
        /// 获取或设置领域显示名。
        /// </summary>
        public required string DomainDisplayName { get; init; }

        /// <summary>
        /// 获取或设置固定题型。
        /// </summary>
        public required QuestionType TemplateType { get; init; }

        /// <summary>
        /// 获取或设置目标题量。
        /// </summary>
        public required int RequestedCount { get; init; }

        /// <summary>
        /// 获取或设置生成提示约束。
        /// </summary>
        public required QuestionBankGenerationHints Hints { get; init; }

        /// <summary>
        /// 获取或设置 AI 管线与日志标题。
        /// </summary>
        public required string OperationTitle { get; init; }

        /// <summary>
        /// 获取或设置完成弹窗标题。
        /// </summary>
        public string CompletionDialogTitle { get; init; } = "AI 出题";

        /// <summary>
        /// 获取或设置是否展示完成弹窗。
        /// </summary>
        public bool ShowCompletionDialog { get; init; } = true;
    }

    /// <summary>
    /// 初始化 <see cref="MainWindowViewModel"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="aiTutor">AI 错题解析服务。</param>
    /// <param name="recommendation">AI 推荐服务。</param>
    /// <param name="studyPlan">AI 学习计划服务。</param>
    /// <param name="importService">题库导入服务。</param>
    /// <param name="questionBankAi">题库 AI 按模板生成服务。</param>
    /// <param name="examQuestionAi">考试中单题 AI 讲解服务。</param>
    /// <param name="aiTrace">AI 调用轨迹。</param>
    /// <param name="configuration">应用配置（日出日落经纬度等）。</param>
    /// <param name="doubaoModelConfig">方舟多档案模型配置（顶栏切换）。</param>
    /// <param name="logger">日志记录器。</param>
    public MainWindowViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IAiTutorService aiTutor,
        IQuestionRecommendationService recommendation,
        IStudyPlanService studyPlan,
        QuestionImportService importService,
        IQuestionBankAiGenerationService questionBankAi,
        IExamQuestionAiExplainService examQuestionAi,
        AiCallTrace aiTrace,
        IConfiguration configuration,
        DoubaoModelConfig doubaoModelConfig,
        ILogger<MainWindowViewModel> logger)
    {
        _dbFactory = dbFactory;
        _aiTutor = aiTutor;
        _recommendation = recommendation;
        _studyPlan = studyPlan;
        _importService = importService;
        _questionBankAi = questionBankAi;
        _examQuestionAi = examQuestionAi;
        _aiTrace = aiTrace;
        _configuration = configuration;
        _doubaoModelConfig = doubaoModelConfig;
        _logger = logger;

        _examTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _examTimer.Tick += OnExamTimerTick;

        _sunScheduleTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _sunScheduleTimer.Tick += OnSunScheduleTimerTick;

        _headerClockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _headerClockTimer.Tick += OnHeaderClockTimerTick;
        _headerClockTimer.Start();
        UpdateHeaderClockText();

        TypeFilterOptions = new ObservableCollection<string> { "全部", "单选", "多选", "判断", "简答", "填空" };
        DifficultyFilterOptions = new ObservableCollection<string> { "全部", "简单", "中等", "困难" };
        DomainFilterOptions = new ObservableCollection<string> { "全部", "未分类", "Python", "C", "C++", "C#", "Rust", "Java", "JavaScript", "Go", "数据结构与算法", "数据库", "操作系统", "计算机网络" };
        EditorTypeOptions = new ObservableCollection<string> { "单选", "多选", "判断", "简答", "填空" };
        EditorDifficultyOptions = new ObservableCollection<string> { "简单", "中等", "困难" };
        EditorDomainOptions = new ObservableCollection<string> { "未分类", "Python", "C", "C++", "C#", "Rust", "Java", "JavaScript", "Go", "数据结构与算法", "数据库", "操作系统", "计算机网络" };

        foreach (var label in EditorDomainOptions)
        {
            ExamDomainPicks.Add(new ExamDomainPickVm(MapUiToDomain(label), label));
        }

        BankAiTemplateOptions = new ObservableCollection<string>
        {
            "单选题模板",
            "多选题模板",
            "判断题模板",
            "简答题模板",
            "填空题模板"
        };

        WrongBookDomainFilterOptions = new ObservableCollection<string>(DomainFilterOptions);
        WrongBookDomainFilter = "全部";
        WrongBookSelectAllInFilterCaption = "本领域全选";

        SelectedTypeFilter = "全部";
        SelectedDifficultyFilter = "全部";

        ExamQuestionCount = 5;
        ExamMinutes = 10;
        ExamUserAnswer = string.Empty;
        StatusMessage = "就绪：请先刷新题库。";
        _wrongBookFilterEventsEnabled = true;

        var themePrefs = ThemePreferenceStore.LoadPreferencesOrDefault();
        UseSunAutoTheme = themePrefs.UseSunAutoTheme;
        IsDarkTheme = AppTheme.IsDark;

        RefreshSunScheduleAndApplyTheme();
        _sunScheduleTimer.Start();

        foreach (var item in _doubaoModelConfig.ListProfileItems())
            AiModelProfileOptions.Add(item);

        var activeId = _doubaoModelConfig.ActiveProfileId;
        SelectedAiModelProfile = AiModelProfileOptions.FirstOrDefault(x =>
            string.Equals(x.Id, activeId, StringComparison.OrdinalIgnoreCase))
            ?? AiModelProfileOptions.FirstOrDefault();
    }

    /// <summary>
    /// 为 true 后，错题本领域下拉变更才会自动刷新列表（避免构造阶段重复查询）。
    /// </summary>
    private bool _wrongBookFilterEventsEnabled;

    /// <summary>
    /// 获取题型筛选下拉选项。
    /// </summary>
    public ObservableCollection<string> TypeFilterOptions { get; }

    /// <summary>
    /// 获取难度筛选下拉选项。
    /// </summary>
    public ObservableCollection<string> DifficultyFilterOptions { get; }

    /// <summary>
    /// 获取编辑器题型下拉选项。
    /// </summary>
    public ObservableCollection<string> EditorTypeOptions { get; }

    /// <summary>
    /// 获取编辑器难度下拉选项。
    /// </summary>
    public ObservableCollection<string> EditorDifficultyOptions { get; }

    /// <summary>
    /// 获取领域筛选下拉选项。
    /// </summary>
    public ObservableCollection<string> DomainFilterOptions { get; }

    /// <summary>
    /// 错题本领域筛选下拉选项（与题库领域名称一致）。
    /// </summary>
    public ObservableCollection<string> WrongBookDomainFilterOptions { get; }

    /// <summary>
    /// 获取编辑器领域下拉选项。
    /// </summary>
    public ObservableCollection<string> EditorDomainOptions { get; }

    /// <summary>
    /// 题库管理页：AI 出题模板下拉（与 <see cref="MapBankAiTemplateToType"/> 对应）。
    /// </summary>
    public ObservableCollection<string> BankAiTemplateOptions { get; }

    /// <summary>
    /// 顶栏：可选方舟模型档案（<c>appsettings.json</c> 中 <c>DoubaoModel:Profiles</c>）。
    /// </summary>
    public ObservableCollection<DoubaoModelProfileListItem> AiModelProfileOptions { get; } = new();

    /// <summary>
    /// 顶栏：当前选中的模型档案。
    /// </summary>
    [ObservableProperty]
    private DoubaoModelProfileListItem? _selectedAiModelProfile;

    /// <summary>
    /// 题库管理页：当前选中的 AI 出题模板显示名。
    /// </summary>
    [ObservableProperty]
    private string _selectedBankAiTemplate = "单选题模板";

    /// <summary>
    /// 题库管理页：希望生成的题目数量（服务端会裁剪到安全上限）。
    /// </summary>
    [ObservableProperty]
    private int _bankAiGenCount = 3;

    /// <summary>
    /// 题库管理页：AI 出题请求进行中（用于禁用按钮）。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseBankAiGen))]
    [NotifyCanExecuteChangedFor(nameof(FillEditorQuestionFromAiCommand))]
    private bool _isBankAiGenerating;

    /// <summary>
    /// 新建题目向导：向 AI 请求单题并填入编辑器时的忙碌状态。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FillEditorQuestionFromAiCommand))]
    private bool _isEditorAiFillBusy;

    /// <summary>
    /// 编辑器「AI 出题」：是否在请求提示词中附带当前分类标签与检索关键词。
    /// </summary>
    [ObservableProperty]
    private bool _editorAiSendTopicTagsToPrompt = true;

    /// <summary>
    /// 编辑器 AI 出题过程中展示的阶段文案（与不确定进度条配合）。
    /// </summary>
    [ObservableProperty]
    private string _editorAiProgressPhase = string.Empty;

    /// <summary>
    /// 批量「AI 按模板生成」过程中的阶段文案。
    /// </summary>
    [ObservableProperty]
    private string _bankAiProgressPhase = string.Empty;

    /// <summary>
    /// 批量 AI 生成：目标题量（本次任务）。
    /// </summary>
    [ObservableProperty]
    private int _bankAiProgressTarget;

    /// <summary>
    /// 批量 AI 生成：已成功入库题量。
    /// </summary>
    [ObservableProperty]
    private int _bankAiProgressGenerated;

    /// <summary>
    /// 批量 AI 生成：尚未入库的剩余目标题量。
    /// </summary>
    [ObservableProperty]
    private int _bankAiProgressRemaining;

    /// <summary>
    /// 批量 AI 生成：整体完成度（0～100），用于确定型进度条。
    /// </summary>
    [ObservableProperty]
    private double _bankAiProgressPercent;

    /// <summary>
    /// 批量 AI 生成：当前是否正在等待单批模型响应（用于细条不确定进度动画）。
    /// </summary>
    [ObservableProperty]
    private bool _bankAiBatchRequestActive;

    /// <summary>
    /// 批量 AI 生成：已生成 / 目标 / 未生成的一行统计文案。
    /// </summary>
    [ObservableProperty]
    private string _bankAiProgressStatsLine = string.Empty;

    /// <summary>
    /// 考试页：模型返回的当前题讲解文本。
    /// </summary>
    [ObservableProperty]
    private string _examAiExplanationText = string.Empty;

    /// <summary>
    /// 考试页：模型返回的最简结论（先于详解展示）。
    /// </summary>
    [ObservableProperty]
    private string _examAiConclusionText = string.Empty;

    /// <summary>
    /// 考试页：单题讲解请求进行中。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskExamAi))]
    private bool _isExamAiExplainBusy;

    /// <summary>
    /// 「AI 按模板生成并入库」按钮是否可用。
    /// </summary>
    public bool CanUseBankAiGen => !IsBankAiGenerating;

    /// <summary>
    /// 「不会，问 AI」按钮是否可用：考试中且未在请求讲解。
    /// </summary>
    public bool CanAskExamAi => IsExamRunning && !IsExamAiExplainBusy;

    /// <summary>
    /// 刷题组卷时可多选的领域列表；若至少勾选一项，则仅从勾选领域抽题；若全部未勾选，则领域范围沿用「题库管理」页的领域下拉（「全部」表示不限）。
    /// </summary>
    public ObservableCollection<ExamDomainPickVm> ExamDomainPicks { get; } = new();

    /// <summary>
    /// 题库列表（当前筛选结果）。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Question> _bankQuestions = new();

    /// <summary>
    /// 当前选中的题库题目（用于编辑/删除）。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FillEditorQuestionFromAiCommand))]
    private Question? _selectedBankQuestion;

    /// <summary>
    /// 编辑器：题干。
    /// </summary>
    [ObservableProperty]
    private string _editorStem = string.Empty;

    /// <summary>
    /// 编辑器：标准答案。
    /// </summary>
    [ObservableProperty]
    private string _editorStandardAnswer = string.Empty;

    /// <summary>
    /// 编辑器：选项 JSON（客观题）。
    /// </summary>
    [ObservableProperty]
    private string _editorOptionsJson = string.Empty;

    /// <summary>
    /// 编辑器：细粒度知识点（逗号分隔），须可测可教；领域以领域下拉为准。
    /// </summary>
    [ObservableProperty]
    private string _editorKnowledgeTags = string.Empty;

    /// <summary>
    /// 编辑器：主知识点（与细知识点中一项一致，供推荐严格匹配）。
    /// </summary>
    [ObservableProperty]
    private string _editorPrimaryKnowledgePoint = string.Empty;

    /// <summary>
    /// 编辑器：模块/大标签（逗号分隔），参与学习计划模块弱项与推荐辅助筛选。
    /// </summary>
    [ObservableProperty]
    private string _editorTopicTags = string.Empty;

    /// <summary>
    /// 编辑器：检索关键词（逗号或分号分隔），参与 AI 推荐与题干匹配。
    /// </summary>
    [ObservableProperty]
    private string _editorTopicKeywords = string.Empty;

    /// <summary>
    /// 分类标签快捷词表（与 <see cref="TopicTagCatalog.Presets"/> 一致），供题目编辑器一键追加。
    /// </summary>
    public IReadOnlyList<string> TopicTagPresetOptions => TopicTagCatalog.Presets;

    /// <summary>
    /// 编辑器：题型（字符串与 ComboBox 对齐）。
    /// </summary>
    [ObservableProperty]
    private string _editorType = "单选";

    /// <summary>
    /// 编辑器：难度（字符串与 ComboBox 对齐）。
    /// </summary>
    [ObservableProperty]
    private string _editorDifficulty = "简单";

    /// <summary>
    /// 编辑器：领域（字符串与 ComboBox 对齐）。
    /// </summary>
    [ObservableProperty]
    private string _editorDomain = "未分类";

    /// <summary>
    /// 题库筛选：题型。
    /// </summary>
    [ObservableProperty]
    private string _selectedTypeFilter = "全部";

    /// <summary>
    /// 题库筛选：难度。
    /// </summary>
    [ObservableProperty]
    private string _selectedDifficultyFilter = "全部";

    /// <summary>
    /// 题库筛选：领域。
    /// </summary>
    [ObservableProperty]
    private string _selectedDomainFilter = "全部";

    /// <summary>
    /// 组卷：题目数量。
    /// </summary>
    [ObservableProperty]
    private int _examQuestionCount;

    /// <summary>
    /// 组卷：考试时长（分钟）。
    /// </summary>
    [ObservableProperty]
    private int _examMinutes;

    /// <summary>
    /// 考试剩余秒数（倒计时显示）。
    /// </summary>
    [ObservableProperty]
    private int _examRemainingSeconds;

    /// <summary>
    /// 是否处于考试中（控制 UI 可用性）。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskExamAi))]
    private bool _isExamRunning;

    /// <summary>
    /// 当前考试题号（从 1 开始显示）。
    /// </summary>
    [ObservableProperty]
    private int _examDisplayIndex;

    /// <summary>
    /// 当前考试题干。
    /// </summary>
    [ObservableProperty]
    private string _examStem = string.Empty;

    /// <summary>
    /// 当前考试题型显示文本。
    /// </summary>
    [ObservableProperty]
    private string _examTypeText = string.Empty;

    /// <summary>
    /// 当前考试难度显示文本。
    /// </summary>
    [ObservableProperty]
    private string _examDifficultyText = string.Empty;

    /// <summary>
    /// 当前考试选项展示文本（从 JSON 解析）。
    /// </summary>
    [ObservableProperty]
    private string _examOptionsDisplay = string.Empty;

    /// <summary>
    /// 考试进度与当前题题库 Id（卷内序号/总题数），避免题干相似或重复感时误以为翻页无效。
    /// </summary>
    [ObservableProperty]
    private string _examProgressLabel = string.Empty;

    /// <summary>
    /// 用户当前作答输入。
    /// </summary>
    [ObservableProperty]
    private string _examUserAnswer = string.Empty;

    /// <summary>
    /// 当前题为单选题时，选项按钮列表（与 <see cref="ExamShowMcqSinglePanel"/> 同时有效）。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ExamOptionItemVm> _examMcqOptions = new();

    /// <summary>
    /// 是否显示单选题选项按钮区。
    /// </summary>
    [ObservableProperty]
    private bool _examShowMcqSinglePanel;

    /// <summary>
    /// 是否显示多选题选项按钮区。
    /// </summary>
    [ObservableProperty]
    private bool _examShowMcqMultiPanel;

    /// <summary>
    /// 是否显示判断题「对 / 错」按钮。
    /// </summary>
    [ObservableProperty]
    private bool _examShowTrueFalsePanel;

    /// <summary>
    /// 是否显示简答 / 填空等文本输入框。
    /// </summary>
    [ObservableProperty]
    private bool _examShowTextAnswerPanel;

    /// <summary>
    /// 最近一次交卷得分（正确题数/总题数）。
    /// </summary>
    [ObservableProperty]
    private string _lastScoreText = "尚未交卷";

    /// <summary>
    /// AI 解析/推荐/计划的输出文本。
    /// </summary>
    [ObservableProperty]
    private string _aiOutputText = string.Empty;

    /// <summary>
    /// 错题本表格行集合。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WrongBookRowVm> _wrongBookRows = new();

    /// <summary>
    /// 领域旁全选按钮文案：当前列表已全选时为「全部不选」，否则为「本领域全选」。
    /// </summary>
    [ObservableProperty]
    private string _wrongBookSelectAllInFilterCaption = "本领域全选";

    /// <summary>
    /// 错题本领域筛选（「全部」表示不限领域）。
    /// </summary>
    [ObservableProperty]
    private string _wrongBookDomainFilter = "全部";

    /// <summary>
    /// 是否将错题详情（含错误作答快照）长期写入本地数据库；关闭后仍累计错次但不保存错答文本。
    /// </summary>
    [ObservableProperty]
    private bool _persistWrongBookDetails = true;

    partial void OnSelectedAiModelProfileChanged(DoubaoModelProfileListItem? value)
    {
        if (value is null)
            return;

        try
        {
            _doubaoModelConfig.SetActiveProfile(value.Id);
            StatusMessage = "已切换模型。";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "切换 AI 模型档案失败");
        }
    }

    /// <summary>
    /// 「增加模型」按钮。
    /// </summary>
    [RelayCommand]
    private void AddAiModelProfile()
    {
        var dialog = new AddModelProfileDialog { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var profileId = GenerateProfileId(dialog.ProfileDisplayName, dialog.ProfileModelName);
            var profile = new DoubaoModelProfileOptions
            {
                DisplayName = string.IsNullOrWhiteSpace(dialog.ProfileDisplayName) ? dialog.ProfileModelName : dialog.ProfileDisplayName,
                ApiKey = dialog.ProfileApiKey,
                ModelName = dialog.ProfileModelName,
                BaseUrl = dialog.ProfileBaseUrl,
                EnableThinking = dialog.ProfileEnableThinking
            };

            UserDoubaoProfileStore.Upsert(profileId, profile);
            var listItem = _doubaoModelConfig.AddProfileAtRuntime(profileId, profile);
            AiModelProfileOptions.Add(listItem);
            SelectedAiModelProfile = listItem;
            StatusMessage = "已添加模型。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加模型档案失败");
            StatusMessage = "添加模型失败：" + ex.Message;
        }
    }

    private static string GenerateProfileId(string displayName, string modelName)
    {
        var raw = string.IsNullOrWhiteSpace(displayName) ? modelName : displayName;
        var clean = new string(raw.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        if (string.IsNullOrWhiteSpace(clean))
            clean = "model";

        return clean.Length > 24 ? clean[..24] : clean;
    }

    partial void OnWrongBookDomainFilterChanged(string value)
    {
        if (!_wrongBookFilterEventsEnabled)
        {
            return;
        }

        _ = RefreshWrongBookAsync();
    }

    /// <summary>
    /// 学习计划输出。
    /// </summary>
    [ObservableProperty]
    private string _studyPlanText = string.Empty;

    /// <summary>
    /// 错题本页「AI 输出」区域是否显示加载遮罩（错题解析 / 题目推荐进行中）。
    /// </summary>
    [ObservableProperty]
    private bool _aiMainOutputBusy;

    /// <summary>
    /// 加载遮罩上展示的说明（发送阶段、步骤名等）。
    /// </summary>
    [ObservableProperty]
    private string _aiMainOutputBusyMessage = string.Empty;

    /// <summary>
    /// 学习计划页大文本框是否显示加载遮罩。
    /// </summary>
    [ObservableProperty]
    private bool _studyPlanOutputBusy;

    /// <summary>
    /// 学习计划加载遮罩说明。
    /// </summary>
    [ObservableProperty]
    private string _studyPlanOutputBusyMessage = string.Empty;

    /// <summary>
    /// 状态栏提示信息。
    /// </summary>
    [ObservableProperty]
    private string _statusMessage;

    /// <summary>
    /// 当前是否为深色主题（与 <see cref="AppTheme.IsDark"/> 同步）。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeToggleToolTip))]
    [NotifyPropertyChangedFor(nameof(ShowSunGlyph))]
    [NotifyPropertyChangedFor(nameof(ShowMoonGlyph))]
    private bool _isDarkTheme;

    /// <summary>
    /// 主题按钮提示：下一步将切换到的模式。
    /// </summary>
    public string ThemeToggleToolTip => IsDarkTheme ? "浅色" : "深色";

    /// <summary>
    /// 深色模式下显示太阳图标（将切到浅色）。
    /// </summary>
    public bool ShowSunGlyph => IsDarkTheme;

    /// <summary>
    /// 浅色模式下显示月亮图标（将切到深色）。
    /// </summary>
    public bool ShowMoonGlyph => !IsDarkTheme;

    /// <summary>
    /// 是否根据配置与日出日落自动切换主题（用户可关闭）。
    /// </summary>
    [ObservableProperty]
    private bool _useSunAutoTheme = true;

    /// <summary>
    /// appsettings 中是否启用了日出日落功能（经纬度计算）。
    /// </summary>
    [ObservableProperty]
    private bool _sunScheduleFeatureEnabled;

    /// <summary>
    /// 当日日出日落是否已成功计算（极区等情况可能失败）。
    /// </summary>
    [ObservableProperty]
    private bool _sunTimesValid;

    /// <summary>
    /// 今日日出时间展示（HH:mm，本地）。
    /// </summary>
    [ObservableProperty]
    private string _sunriseDisplayText = "—";

    /// <summary>
    /// 今日日落时间展示（HH:mm，本地）。
    /// </summary>
    [ObservableProperty]
    private string _sunsetDisplayText = "—";

    /// <summary>
    /// 时间线左侧「午夜—日出」段宽度（像素）。
    /// </summary>
    [ObservableProperty]
    private int _sunBarNight1Width;

    /// <summary>
    /// 时间线「日出—日落」白昼段宽度（像素）。
    /// </summary>
    [ObservableProperty]
    private int _sunBarDayWidth;

    /// <summary>
    /// 时间线「日落—午夜」段宽度（像素）。
    /// </summary>
    [ObservableProperty]
    private int _sunBarNight2Width;

    /// <summary>
    /// 当前时刻在时间线上的水平偏移（像素，用于指示线）。
    /// </summary>
    [ObservableProperty]
    private double _sunIndicatorOffset;

    /// <summary>
    /// 当前处于白昼或夜晚的简短说明。
    /// </summary>
    [ObservableProperty]
    private string _sunPhaseHint = string.Empty;

    /// <summary>
    /// 单行摘要：日出、日落与当前相位。
    /// </summary>
    [ObservableProperty]
    private string _sunSummaryLine = string.Empty;

    /// <summary>
    /// 观测点经纬度说明（来自配置）。
    /// </summary>
    [ObservableProperty]
    private string _sunLocationHint = string.Empty;

    /// <summary>
    /// 本机当前时间（与色条并列展示，每秒刷新）。
    /// </summary>
    [ObservableProperty]
    private string _headerClockText = string.Empty;

    /// <summary>
    /// 是否展示日出日落时间线区域（配置启用且计算成功）。
    /// </summary>
    public bool ShowSunTimeline => SunScheduleFeatureEnabled && SunTimesValid;

    /// <summary>
    /// 配置启用了日出日落模块但计算失败时，用于展示提示文案。
    /// </summary>
    public bool ShowSunScheduleMessage => SunScheduleFeatureEnabled && !SunTimesValid;

    partial void OnSunTimesValidChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSunTimeline));
        OnPropertyChanged(nameof(ShowSunScheduleMessage));
    }

    partial void OnSunScheduleFeatureEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSunTimeline));
        OnPropertyChanged(nameof(ShowSunScheduleMessage));
    }

    partial void OnUseSunAutoThemeChanged(bool value)
    {
        ThemePreferenceStore.SavePreferences(new ThemePreferenceState(IsDarkTheme, value));
        if (value)
        {
            RefreshSunScheduleAndApplyTheme();
        }
    }

    /// <summary>
    /// 定时器：刷新日出日落时间线并按策略校正主题。
    /// </summary>
    private void OnSunScheduleTimerTick(object? sender, EventArgs e) => RefreshSunScheduleAndApplyTheme();

    /// <summary>
    /// 定时器：更新顶栏当前时间文本。
    /// </summary>
    private void OnHeaderClockTimerTick(object? sender, EventArgs e) => UpdateHeaderClockText();

    /// <summary>
    /// 将 <see cref="HeaderClockText"/> 设为当前本机时间字符串。
    /// </summary>
    private void UpdateHeaderClockText()
    {
        HeaderClockText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 读取本机日期与配置经纬度，更新日出/日落展示与时间线比例；若启用自动主题则切换昼夜资源。
    /// </summary>
    private void RefreshSunScheduleAndApplyTheme()
    {
        var sun = _configuration.GetSection("SunSchedule").Get<SunScheduleOptions>() ?? new SunScheduleOptions();
        SunScheduleFeatureEnabled = sun.EnableAutoTheme;
        SunLocationHint = FormatLatLonHint(sun.Latitude, sun.Longitude);

        if (!sun.EnableAutoTheme)
        {
            SunTimesValid = false;
            SunriseDisplayText = "—";
            SunsetDisplayText = "—";
            SunBarNight1Width = SunBarDayWidth = SunBarNight2Width = 0;
            SunIndicatorOffset = 0;
            SunPhaseHint = "日出日落自动主题已在配置中关闭。";
            SunSummaryLine = string.Empty;
            return;
        }

        if (!SunCalcLite.TryGetSunriseSunsetLocal(DateTime.Today, sun.Latitude, sun.Longitude, out var rise, out var set))
        {
            SunTimesValid = false;
            SunriseDisplayText = "—";
            SunsetDisplayText = "—";
            SunBarNight1Width = SunBarDayWidth = SunBarNight2Width = 0;
            SunIndicatorOffset = 0;
            SunPhaseHint = "当前纬度/日期下无法计算日出日落（例如极昼极夜区），请调整 SunSchedule 纬度或关闭自动。";
            SunSummaryLine = SunPhaseHint;
            return;
        }

        SunTimesValid = true;
        SunriseDisplayText = rise.ToString("HH:mm");
        SunsetDisplayText = set.ToString("HH:mm");

        const int barTotal = 400;
        var dayStart = DateTime.Today;
        var now = DateTime.Now;
        var h24 = 24.0;
        var night1H = Math.Max(0, (rise - dayStart).TotalHours);
        var dayH = Math.Max(0, (set - rise).TotalHours);
        var night2H = Math.Max(0, h24 - night1H - dayH);
        var scale = barTotal / h24;
        SunBarNight1Width = (int)Math.Round(night1H * scale);
        SunBarDayWidth = (int)Math.Round(dayH * scale);
        SunBarNight2Width = barTotal - SunBarNight1Width - SunBarDayWidth;
        var posH = Math.Clamp((now - dayStart).TotalHours, 0, h24);
        SunIndicatorOffset = Math.Clamp(posH * scale - 1, 0, barTotal - 2);

        if (now < rise)
        {
            SunPhaseHint = $"夜晚（距日出约 {FormatDuration(rise - now)}）";
        }
        else if (now < set)
        {
            SunPhaseHint = $"白昼（距日落约 {FormatDuration(set - now)}）";
        }
        else
        {
            SunPhaseHint = $"夜晚（距明日日出见时间线；已过日落 {FormatDuration(now - set)}）";
        }

        SunSummaryLine = $"今日日出 {SunriseDisplayText} · 日落 {SunsetDisplayText} · {SunPhaseHint}";

        if (!UseSunAutoTheme || !sun.EnableAutoTheme)
        {
            return;
        }

        var wantDark = DayNightThemeBootstrap.IsNight(now, rise, set);
        if (wantDark == IsDarkTheme)
        {
            return;
        }

        IsDarkTheme = wantDark;
        AppTheme.Apply(wantDark);
        ThemePreferenceStore.SavePreferences(new ThemePreferenceState(wantDark, true));
    }

    private static string FormatLatLonHint(double lat, double lon)
    {
        var ns = lat >= 0 ? "N" : "S";
        var ew = lon >= 0 ? "E" : "W";
        return $"观测点 {Math.Abs(lat):F2}°{ns}，{Math.Abs(lon):F2}°{ew}（appsettings.json → SunSchedule）";
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours} 小时 {ts.Minutes} 分";
        }

        return $"{Math.Max(1, ts.Minutes)} 分";
    }

    /// <summary>
    /// AI 请求当前阶段（空闲 / 正在发送 / 成功 / 失败）。
    /// </summary>
    [ObservableProperty]
    private string _aiPipelinePhase = "空闲";

    /// <summary>
    /// AI 请求补充说明（当前步骤或错误摘要）。
    /// </summary>
    [ObservableProperty]
    private string _aiPipelineDetail = string.Empty;

    /// <summary>
    /// 最近一次 AI 调用的按时间追加日志，便于调试。
    /// </summary>
    [ObservableProperty]
    private string _aiPipelineLog = string.Empty;

    /// <summary>
    /// 存储推荐的题目ID列表，用于后续开始刷题。
    /// </summary>
    private List<long> _recommendedQuestionIds = new();

    /// <summary>
    /// 防止 CareerPath 启动参数与 IPC 重复投递并发执行同一套刷题/推荐流程。
    /// </summary>
    private readonly SemaphoreSlim _careerPathImportGate = new(1, 1);

    /// <inheritdoc />
    public void Dispose()
    {
        _examTimer.Tick -= OnExamTimerTick;
        _examTimer.Stop();
        _sunScheduleTimer.Tick -= OnSunScheduleTimerTick;
        _sunScheduleTimer.Stop();
        _headerClockTimer.Tick -= OnHeaderClockTimerTick;
        _headerClockTimer.Stop();
        _careerPathImportGate.Dispose();
    }

    /// <summary>
    /// 在 UI 线程更新绑定属性（异步回调可能在线程池上）。
    /// </summary>
    private void Ui(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null || d.CheckAccess())
            action();
        else
            d.Invoke(action);
    }

    /// <summary>
    /// 登记当前 Ark 请求的可取消源：若已有进行中的请求则先取消其标记。
    /// </summary>
    private void RegisterAiCancellation(CancellationTokenSource cts)
    {
        CancellationTokenSource? previous;
        lock (_aiRequestLock)
        {
            previous = _aiRequestCts;
            _aiRequestCts = cts;
        }

        previous?.Cancel();
        Ui(() => CancelActiveAiRequestCommand.NotifyCanExecuteChanged());
    }

    /// <summary>
    /// 结束一次 Ark 请求登记：若仍是当前源则清空字段并释放。
    /// </summary>
    private void UnregisterAiCancellation(CancellationTokenSource owned)
    {
        lock (_aiRequestLock)
        {
            if (ReferenceEquals(_aiRequestCts, owned))
            {
                _aiRequestCts = null;
            }
        }

        owned.Dispose();
        Ui(() => CancelActiveAiRequestCommand.NotifyCanExecuteChanged());
    }

    /// <summary>
    /// 取消按钮是否可用：存在未取消的当前请求源。
    /// </summary>
    private bool CanCancelActiveAiRequest()
    {
        lock (_aiRequestLock)
        {
            return _aiRequestCts is not null && !_aiRequestCts.IsCancellationRequested;
        }
    }

    /// <summary>
    /// 放弃当前进行中的 Ark 请求，不采用随后返回的结果。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelActiveAiRequest))]
    private void CancelActiveAiRequest()
    {
        lock (_aiRequestLock)
        {
            _aiRequestCts?.Cancel();
        }

        CancelActiveAiRequestCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 设置 AI 流水线可见状态。
    /// </summary>
    private void SetAiPipeline(string phase, string? detail = null)
    {
        Ui(() =>
        {
            AiPipelinePhase = phase;
            AiPipelineDetail = detail ?? string.Empty;
        });
    }

    /// <summary>
    /// 控制错题本页右侧「AI 输出」区域的加载动画与提示文案。
    /// </summary>
    private void SetAiMainOutputLoading(bool busy, string? message = null)
    {
        Ui(() =>
        {
            AiMainOutputBusy = busy;
            AiMainOutputBusyMessage = busy ? (message ?? "正在请求 AI…") : string.Empty;
        });
    }

    /// <summary>
    /// 控制学习计划页大文本框上的加载动画与提示文案。
    /// </summary>
    private void SetStudyPlanOutputLoading(bool busy, string? message = null)
    {
        Ui(() =>
        {
            StudyPlanOutputBusy = busy;
            StudyPlanOutputBusyMessage = busy ? (message ?? "正在请求 AI…") : string.Empty;
        });
    }

    /// <summary>
    /// 追加一行 AI 调试日志（带时间戳，过长时截断）。
    /// </summary>
    private void AppendAiLog(string line)
    {
        var t = DateTime.Now.ToString("HH:mm:ss");
        Ui(() =>
        {
            AiPipelineLog += $"[{t}] {line}\r\n";
            if (AiPipelineLog.Length > 16000)
                AiPipelineLog = AiPipelineLog[^12000..];
        });
    }

    /// <summary>
    /// 将异常格式化为单段说明（含内部异常）。
    /// </summary>
    private static string FormatExceptionMessage(Exception ex)
    {
        var s = ex.Message;
        if (ex.InnerException != null)
            s += " → " + ex.InnerException.Message;
        return s;
    }

    /// <summary>
    /// 规范化主知识点：优先编辑器主知识点；否则取细知识点中首个非停用词；再否则为「未分类」。
    /// </summary>
    private string NormalizeEditorPrimaryKnowledgePoint() =>
        NormalizePrimaryKnowledgePoint(EditorPrimaryKnowledgePoint, EditorKnowledgeTags);

    private static string NormalizePrimaryKnowledgePoint(string? primaryField, string knowledgeTagsCsv)
    {
        var raw = (primaryField ?? string.Empty).Trim();
        if (raw.Length > 128)
            raw = raw[..128];

        if (raw.Length > 0)
            return raw;

        foreach (var t in RecommendationMatcher.Tokenize(knowledgeTagsCsv))
        {
            if (!KnowledgeTagStopwords.IsStopword(t))
                return t.Length > 128 ? t[..128] : t;
        }

        return "未分类";
    }

    /// <summary>
    /// 根据 <see cref="AiCallTrace"/> 刷新「AI 请求状态」与日志，区分方舟是否真正返回可用结果。
    /// </summary>
    private void ApplyAiTraceToUi(string stepChineseName)
    {
        var ark = _aiTrace.LastUsedArk;
        var step = _aiTrace.LastStep;
        AppendAiLog($"{stepChineseName}：{(ark ? "方舟 API 已调用" : "未使用方舟（本地回退）")} [{step}]");
        if (!ark)
            SetAiPipeline("已回退本地", step);
        else
            SetAiPipeline("成功", $"{stepChineseName}（云端）");
    }

    /// <summary>
    /// 当题库选中项变化时，同步到编辑器字段。
    /// </summary>
    /// <param name="value">新选中题目。</param>
    partial void OnSelectedBankQuestionChanged(Question? value)
    {
        if (value is null)
        {
            return;
        }

        EditorStem = value.Stem;
        EditorStandardAnswer = value.StandardAnswer ?? string.Empty;
        EditorOptionsJson = value.OptionsJson ?? string.Empty;
        EditorKnowledgeTags = value.KnowledgeTags;
        EditorPrimaryKnowledgePoint = value.PrimaryKnowledgePoint ?? string.Empty;
        EditorTopicTags = value.TopicTags ?? string.Empty;
        EditorTopicKeywords = value.TopicKeywords ?? string.Empty;
        EditorType = MapTypeToUi(value.Type);
        EditorDifficulty = MapDifficultyToUi(value.Difficulty);
        EditorDomain = MapDomainToUi(value.Domain);
    }

    /// <summary>
    /// 在浅色与深色主题之间切换并写入本地偏好。
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        UseSunAutoTheme = false;
        IsDarkTheme = !IsDarkTheme;
        AppTheme.Apply(IsDarkTheme);
        ThemePreferenceStore.SavePreferences(new ThemePreferenceState(IsDarkTheme, false));
    }

    /// <summary>
    /// 刷新题库列表（应用题型+难度+领域 AND 筛选）。
    /// </summary>
    [RelayCommand]
    private async Task RefreshBankAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var q = db.Questions.AsNoTracking().Where(x => x.IsEnabled);

            var type = MapUiToTypeOrNull(SelectedTypeFilter);
            if (type is not null)
            {
                q = q.Where(x => x.Type == type);
            }

            var diff = MapUiToDifficultyOrNull(SelectedDifficultyFilter);
            if (diff is not null)
            {
                q = q.Where(x => x.Difficulty == diff);
            }

            var domain = MapUiToDomainOrNull(SelectedDomainFilter);
            if (domain is not null)
            {
                q = q.Where(x => x.Domain == domain);
            }

            var list = await q.OrderByDescending(x => x.Id).ToListAsync().ConfigureAwait(false);
            
            BankQuestions = new ObservableCollection<Question>(list);
            StatusMessage = $"题库已刷新：{list.Count} 条（筛选：{SelectedTypeFilter} + {SelectedDifficultyFilter} + {SelectedDomainFilter}）。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新题库失败");
            StatusMessage = "刷新题库失败：" + ex.Message;
        }
    }

    /// <summary>
    /// 保存题目（新增或更新）。
    /// </summary>
    [RelayCommand]
    private async Task SaveQuestionAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditorStem) || string.IsNullOrWhiteSpace(EditorStandardAnswer))
            {
                StatusMessage = "题干与标准答案不能为空。";
                return;
            }

            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var type = MapUiToType(EditorType);
            var diff = MapUiToDifficulty(EditorDifficulty);
            var domain = MapUiToDomain(EditorDomain);

            if (SelectedBankQuestion is null)
            {
                db.Questions.Add(new Question
                {
                    Stem = EditorStem.Trim(),
                    StandardAnswer = EditorStandardAnswer.Trim(),
                    OptionsJson = string.IsNullOrWhiteSpace(EditorOptionsJson) ? null : EditorOptionsJson.Trim(),
                    KnowledgeTags = string.IsNullOrWhiteSpace(EditorKnowledgeTags) ? "未分类" : EditorKnowledgeTags.Trim(),
                    PrimaryKnowledgePoint = NormalizeEditorPrimaryKnowledgePoint(),
                    TopicTags = string.IsNullOrWhiteSpace(EditorTopicTags) ? string.Empty : EditorTopicTags.Trim(),
                    TopicKeywords = string.IsNullOrWhiteSpace(EditorTopicKeywords) ? string.Empty : EditorTopicKeywords.Trim(),
                    Type = type,
                    Difficulty = diff,
                    Domain = domain,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                var entity = await db.Questions.FirstOrDefaultAsync(x => x.Id == SelectedBankQuestion.Id).ConfigureAwait(false);
                if (entity is null)
                {
                    StatusMessage = "未找到要更新的题目。";
                    return;
                }

                entity.Stem = EditorStem.Trim();
                entity.StandardAnswer = EditorStandardAnswer.Trim();
                entity.OptionsJson = string.IsNullOrWhiteSpace(EditorOptionsJson) ? null : EditorOptionsJson.Trim();
                entity.KnowledgeTags = string.IsNullOrWhiteSpace(EditorKnowledgeTags) ? "未分类" : EditorKnowledgeTags.Trim();
                entity.PrimaryKnowledgePoint = NormalizeEditorPrimaryKnowledgePoint();
                entity.TopicTags = string.IsNullOrWhiteSpace(EditorTopicTags) ? string.Empty : EditorTopicTags.Trim();
                entity.TopicKeywords = string.IsNullOrWhiteSpace(EditorTopicKeywords) ? string.Empty : EditorTopicKeywords.Trim();
                entity.Type = type;
                entity.Difficulty = diff;
                entity.Domain = domain;
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
            StatusMessage = "题目已保存。";
            await RefreshBankAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存题目失败");
            StatusMessage = "保存题目失败：" + ex.Message;
            MessageBox.Show("保存题目失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 将快捷词追加到编辑器中的分类标签（逗号分隔，忽略大小写重复）。
    /// </summary>
    /// <param name="token">预设标签文本。</param>
    [RelayCommand]
    private void AppendTopicTag(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var t = token.Trim();
        var parts = ParseTopicTagsToList(EditorTopicTags);
        if (parts.Exists(p => string.Equals(p, t, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"标签「{t}」已在当前编辑内容中。";
            return;
        }

        parts.Add(t);
        EditorTopicTags = string.Join(",", parts);
    }

    /// <summary>
    /// 仅更新一道题的 <see cref="Question.TopicTags"/>（用于表格内联编辑提交）。
    /// </summary>
    /// <param name="questionId">题目主键。</param>
    /// <param name="topicTagsRaw">原始标签串（逗号/分号/中英文标点分隔）。</param>
    public async Task SaveBankQuestionTopicTagsAsync(long questionId, string topicTagsRaw)
    {
        var normalized = string.IsNullOrWhiteSpace(topicTagsRaw) ? string.Empty : topicTagsRaw.Trim();
        if (normalized.Length > 512)
        {
            normalized = normalized[..512];
            StatusMessage = "分类标签已截断至 512 字以符合库表限制。";
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Questions.FirstOrDefaultAsync(x => x.Id == questionId);
            if (entity is null)
            {
                StatusMessage = $"未找到题目 Id={questionId}，分类标签未保存。";
                return;
            }

            entity.TopicTags = normalized;
            await db.SaveChangesAsync();

            var row = BankQuestions.FirstOrDefault(x => x.Id == questionId);
            if (row is not null)
            {
                row.TopicTags = normalized;
            }

            if (SelectedBankQuestion?.Id == questionId)
            {
                EditorTopicTags = normalized;
            }

            StatusMessage = $"已保存题目 {questionId} 的分类标签。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存分类标签失败 Id={QuestionId}", questionId);
            StatusMessage = "保存分类标签失败：" + ex.Message;
            MessageBox.Show("保存分类标签失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 将分类标签字符串拆成有序列表（去首尾空白，保留大小写）。
    /// </summary>
    private static List<string> ParseTopicTagsToList(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return list;
        }

        foreach (var segment in raw.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var s = segment.Trim();
            if (s.Length > 0)
            {
                list.Add(s);
            }
        }

        return list;
    }

    /// <summary>
    /// 新建题目（清空编辑器与选中项）。
    /// </summary>
    [RelayCommand]
    private void NewQuestion()
    {
        SelectedBankQuestion = null;
        EditorStem = string.Empty;
        EditorStandardAnswer = string.Empty;
        EditorOptionsJson = string.Empty;
        EditorKnowledgeTags = string.Empty;
        EditorPrimaryKnowledgePoint = string.Empty;
        EditorTopicTags = string.Empty;
        EditorTopicKeywords = string.Empty;
        EditorType = "单选";
        EditorDifficulty = "简单";
        EditorDomain = "未分类";
        StatusMessage = "已进入新建题目模式。";
    }

    /// <summary>
    /// 删除选中题目（硬删除演示版）。
    /// </summary>
    [RelayCommand]
    private async Task DeleteQuestionAsync()
    {
        if (SelectedBankQuestion is null)
        {
            StatusMessage = "请先选中题目。";
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var entity = await db.Questions.FirstOrDefaultAsync(x => x.Id == SelectedBankQuestion.Id).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = "题目不存在。";
            return;
        }

        db.Questions.Remove(entity);
        await db.SaveChangesAsync().ConfigureAwait(false);
        SelectedBankQuestion = null;
        StatusMessage = "题目已删除。";
        await RefreshBankAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 随机组卷并开始考试（限时）。
    /// </summary>
    [RelayCommand]
    private async Task StartExamAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var q = db.Questions.AsNoTracking().Where(x => x.IsEnabled);

        var type = MapUiToTypeOrNull(SelectedTypeFilter);
        if (type is not null)
        {
            q = q.Where(x => x.Type == type);
        }

        var diff = MapUiToDifficultyOrNull(SelectedDifficultyFilter);
        if (diff is not null)
        {
            q = q.Where(x => x.Difficulty == diff);
        }

        var examDomains = ExamDomainPicks.Where(x => x.IsSelected).Select(x => x.Domain).ToList();
        if (examDomains.Count > 0)
        {
            q = q.Where(x => examDomains.Contains(x.Domain));
        }
        else
        {
            var domain = MapUiToDomainOrNull(SelectedDomainFilter);
            if (domain is not null)
            {
                q = q.Where(x => x.Domain == domain);
            }
        }

        var pool = await q.ToListAsync().ConfigureAwait(false);
        if (pool.Count == 0)
        {
            StatusMessage = "题库筛选结果为空，无法组卷。";
            return;
        }

        var requested = Math.Clamp(ExamQuestionCount, 1, 50);
        var paper = BuildRandomPaperWithoutDuplicateIds(pool, requested);
        if (paper.Count == 0)
        {
            StatusMessage = "可用题目不足，无法组卷。";
            return;
        }

        _examFromWrongBookRedoSession = false;
        _examQuestions.Clear();
        _examQuestions.AddRange(paper);
        _answersByDisplayIndex.Clear();
        _examSessionId = Guid.NewGuid();

        ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
        IsExamRunning = true;
        ExamDisplayIndex = 1;
        ExamUserAnswer = string.Empty;
        _examTimer.Start();

        RenderCurrentExamQuestion();
        var domainNote = examDomains.Count > 0
            ? $"领域：{string.Join("、", ExamDomainPicks.Where(x => x.IsSelected).Select(x => x.DisplayName))}。"
            : string.Empty;
        var shortfallNote = paper.Count < requested
            ? $" 设定 {requested} 题，筛选后仅 {paper.Count} 题可用，已按最大可用题量开考。"
            : string.Empty;
        StatusMessage = $"考试已开始：{paper.Count} 题（随机抽样、题号不重复），倒计时 {ExamRemainingSeconds} 秒。{domainNote}{shortfallNote}";
        ExamStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 从错题本抽取题目开始专项练习（同样走倒计时与交卷判分）。
    /// </summary>
    [RelayCommand]
    private async Task StartWrongBookPracticeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var userId = DatabaseInitializer.DemoUserId;

        var joined = db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (w, q) => new { w, q });

        var domain = MapUiToDomainOrNull(WrongBookDomainFilter);
        if (domain is not null)
        {
            joined = joined.Where(x => x.q.Domain == domain);
        }

        var wrongIds = await joined
            .Select(x => x.q.Id)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        if (wrongIds.Count == 0)
        {
            StatusMessage = domain is null ? "错题本为空。" : "当前领域筛选下没有可练的错题。";
            return;
        }

        var qs = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled && wrongIds.Contains(q.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        if (qs.Count == 0)
        {
            StatusMessage = "错题本中的题目在题库中已不存在或已禁用。";
            return;
        }

        var requested = Math.Clamp(ExamQuestionCount, 1, 50);
        var paper = BuildRandomPaperWithoutDuplicateIds(qs, requested);
        if (paper.Count == 0)
        {
            StatusMessage = "无法组卷。";
            return;
        }

        _examFromWrongBookRedoSession = true;
        _examQuestions.Clear();
        _examQuestions.AddRange(paper);
        _answersByDisplayIndex.Clear();
        _examSessionId = Guid.NewGuid();

        ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
        IsExamRunning = true;
        ExamDisplayIndex = 1;
        ExamUserAnswer = string.Empty;
        _examTimer.Start();

        RenderCurrentExamQuestion();
        var wbShortfall = paper.Count < requested
            ? $" 设定 {requested} 题，错题可用仅 {paper.Count} 题，已按最大可用题量开考。"
            : string.Empty;
        StatusMessage = $"错题练习已开始：{paper.Count} 题（随机抽样、题号不重复）。{wbShortfall}";
        ExamStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 上一题（保留作答进度到内存字典）。
    /// </summary>
    [RelayCommand]
    private void ExamPrev()
    {
        if (!IsExamRunning || _examQuestions.Count == 0)
        {
            return;
        }

        StoreCurrentAnswerSnapshot();
        if (ExamDisplayIndex <= 1)
        {
            StatusMessage = "已是第一题。";
            return;
        }

        ExamDisplayIndex--;
        RenderCurrentExamQuestion();
    }

    /// <summary>
    /// 下一题。
    /// </summary>
    [RelayCommand]
    private void ExamNext()
    {
        if (!IsExamRunning || _examQuestions.Count == 0)
        {
            return;
        }

        StoreCurrentAnswerSnapshot();
        if (ExamDisplayIndex >= _examQuestions.Count)
        {
            StatusMessage = "已是最后一题。";
            return;
        }

        ExamDisplayIndex++;
        RenderCurrentExamQuestion();
    }

    /// <summary>
    /// 单选题：点击选项按钮，写入答案并高亮当前项。
    /// </summary>
    [RelayCommand]
    private void ExamPickSingleOption(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        ExamUserAnswer = key.Trim();
        foreach (var opt in ExamMcqOptions)
        {
            opt.IsSelected = string.Equals(opt.Key, ExamUserAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 多选题：点选切换某个选项的选中状态，并同步为「A,C」形式答案串。
    /// </summary>
    [RelayCommand]
    private void ExamToggleMultiOption(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var item = ExamMcqOptions.FirstOrDefault(x => string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
        var keys = ExamMcqOptions.Where(x => x.IsSelected).Select(x => x.Key).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        ExamUserAnswer = string.Join(",", keys);
    }

    /// <summary>
    /// 判断题：选「对」（无参数命令，避免带 CommandParameter 时部分环境下无法触发）。
    /// </summary>
    [RelayCommand]
    private void ExamPickTrue()
    {
        ExamUserAnswer = "对";
    }

    /// <summary>
    /// 判断题：选「错」。
    /// </summary>
    [RelayCommand]
    private void ExamPickFalse()
    {
        ExamUserAnswer = "错";
    }

    /// <summary>
    /// 取消做题：不保存答题记录，直接结束考试。
    /// </summary>
    [RelayCommand]
    private void CancelExam()
    {
        if (!IsExamRunning)
        {
            return;
        }

        var dialogResult = MessageBox.Show("确定要取消当前考试吗？答题记录将不会保存。", "取消考试", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult != MessageBoxResult.Yes)
        {
            return;
        }

        _examTimer.Stop();
        IsExamRunning = false;
        _examFromWrongBookRedoSession = false;
        _examQuestions.Clear();
        _answersByDisplayIndex.Clear();
        ResetExamAnswerPanels();
        ExamStem = string.Empty;
        ExamTypeText = string.Empty;
        ExamDifficultyText = string.Empty;
        ExamOptionsDisplay = string.Empty;
        ExamProgressLabel = string.Empty;
        ExamUserAnswer = string.Empty;
        ExamDisplayIndex = 0;
        ExamAiExplanationText = string.Empty;
        ExamAiConclusionText = string.Empty;
        StatusMessage = "已取消考试。";
    }

    /// <summary>
    /// 交卷：自动判分、写入答题记录、更新错题本；不自动调用 AI（请使用「AI 错题解析」按钮单独请求）。
    /// </summary>
    [RelayCommand]
    private async Task SubmitExamAsync()
    {
        if (Interlocked.Exchange(ref _submitGate, 1) == 1)
        {
            return;
        }

        try
        {
        if (!IsExamRunning || _examQuestions.Count == 0)
        {
            StatusMessage = "当前没有进行中的考试。";
            return;
        }

        _examTimer.Stop();
        IsExamRunning = false;
        StoreCurrentAnswerSnapshot();

        var markWrongBookRedoSession = _examFromWrongBookRedoSession;
        _examFromWrongBookRedoSession = false;

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var userId = DatabaseInitializer.DemoUserId;

        var correct = 0;
        var wrongInsights = new List<WrongQuestionInsightDto>();

        for (var i = 0; i < _examQuestions.Count; i++)
        {
            var q = _examQuestions[i];
            var answer = _answersByDisplayIndex.TryGetValue(i + 1, out var a) ? a : string.Empty;
            var ok = AnswerGrader.IsCorrect(q, answer);
            if (ok)
            {
                correct++;
            }

            // 演示版：耗时字段用于统计趋势，这里使用稳定占位值避免计时器与切题逻辑耦合。
            const int durationMs = 1000;
            db.AnswerRecords.Add(new AnswerRecord
            {
                UserId = userId,
                QuestionId = q.Id,
                SessionId = _examSessionId,
                UserAnswer = answer,
                IsCorrect = ok,
                Score = ok ? 1m : 0m,
                DurationMs = durationMs,
                CreatedAtUtc = DateTime.UtcNow
            });

            if (!ok)
            {
                await UpsertWrongBookAsync(db, userId, q.Id, answer, PersistWrongBookDetails).ConfigureAwait(false);
                _wrongBookAnalysisSelectionIds.Add(q.Id);

                wrongInsights.Add(new WrongQuestionInsightDto
                {
                    QuestionId = q.Id,
                    Type = q.Type,
                    StemSummary = q.Stem.Length > 48 ? q.Stem[..48] + "…" : q.Stem,
                    StemFull = q.Stem,
                    OptionsJson = q.OptionsJson,
                    KnowledgeTags = q.KnowledgeTags,
                    UserAnswer = answer,
                    StandardAnswer = q.StandardAnswer,
                    RootCause = string.Empty,
                    SolutionHints = string.Empty,
                    OptionAnalysis = string.Empty
                });
            }
        }

        if (markWrongBookRedoSession && _examQuestions.Count > 0)
        {
            var paperIds = _examQuestions.Select(q => q.Id).ToList();
            var redoEntries = await db.WrongBookEntries
                .Where(w => w.UserId == userId && paperIds.Contains(w.QuestionId))
                .ToListAsync()
                .ConfigureAwait(false);
            var nowRedo = DateTime.UtcNow;
            foreach (var wb in redoEntries)
            {
                wb.LastRedoCompletedAtUtc = nowRedo;
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        LastScoreText = $"{correct}/{_examQuestions.Count}";
        var redoNote = markWrongBookRedoSession ? " 错题再练已标记「已重做」。" : string.Empty;
        StatusMessage = $"交卷完成：{LastScoreText}。错题数：{wrongInsights.Count}。{redoNote}";

        _pendingWrongInsightsForAi = wrongInsights;
        if (wrongInsights.Count > 0)
        {
            AiOutputText =
                $"交卷已保存，共 {wrongInsights.Count} 道错题已记入错题本并已勾选。\n" +
                "请在「错题本 / AI」中确认勾选后点击「AI 错题解析（已勾选）」发送给模型。";
        }
        else
        {
            _pendingWrongInsightsForAi.Clear();
            AiOutputText = "本次没有错题。如需学习计划，请在学习计划页点击「生成 AI 学习计划」。";
        }

        _examQuestions.Clear();
        _answersByDisplayIndex.Clear();
        ResetExamAnswerPanels();
        ExamStem = string.Empty;
        ExamTypeText = string.Empty;
        ExamDifficultyText = string.Empty;
        ExamOptionsDisplay = string.Empty;
        ExamProgressLabel = string.Empty;
        ExamUserAnswer = string.Empty;
        ExamDisplayIndex = 0;
        ExamAiExplanationText = string.Empty;
        ExamAiConclusionText = string.Empty;
        }
        finally
        {
            Interlocked.Exchange(ref _submitGate, 0);
        }
    }

    /// <summary>
    /// 组卷：按 <see cref="Question.Id"/> 去重后，用部分 Fisher–Yates 算法配合
    /// <see cref="RandomNumberGenerator.GetInt32(int, int)"/> 做无放回均匀抽样，使顺序与组合随机性尽可能高；同一套卷内题号不重复。
    /// 若可用题量少于 <paramref name="requestedCount"/>，则只抽取全部可用题（不超过 50）。
    /// </summary>
    private static List<Question> BuildRandomPaperWithoutDuplicateIds(IReadOnlyList<Question> pool, int requestedCount)
    {
        var distinct = pool.DistinctBy(q => q.Id).ToList();
        if (distinct.Count == 0)
        {
            return new List<Question>();
        }

        var take = Math.Min(Math.Clamp(requestedCount, 1, 50), distinct.Count);
        // 部分 Fisher–Yates：将 [0..take) 位置变为从 n 元集合中均匀随机选出的 take 道题（顺序亦随机）。
        for (var i = 0; i < take; i++)
        {
            var j = RandomNumberGenerator.GetInt32(i, distinct.Count);
            (distinct[i], distinct[j]) = (distinct[j], distinct[i]);
        }

        return distinct.Take(take).ToList();
    }

    /// <summary>
    /// 错题行勾选变更：写入短时记忆集合，切换领域筛选后仍会恢复勾选。
    /// </summary>
    private void OnWrongBookRowSelectionChanged(long questionId, bool selected)
    {
        if (selected)
        {
            _wrongBookAnalysisSelectionIds.Add(questionId);
        }
        else
        {
            _wrongBookAnalysisSelectionIds.Remove(questionId);
        }

        RefreshWrongBookSelectAllCaption();
    }

    /// <summary>
    /// 根据当前筛选下列表是否已全选，更新领域旁按钮的「本领域全选 / 全部不选」文案。
    /// </summary>
    private void RefreshWrongBookSelectAllCaption()
    {
        if (WrongBookRows.Count == 0)
        {
            WrongBookSelectAllInFilterCaption = "本领域全选";
            return;
        }

        var allSelected = WrongBookRows.All(r => _wrongBookAnalysisSelectionIds.Contains(r.QuestionId));
        WrongBookSelectAllInFilterCaption = allSelected ? "全部不选" : "本领域全选";
    }

    /// <summary>
    /// 切换当前筛选下列表的 AI 解析勾选：未全选则一次全选；已全选则取消本列表内全部勾选（其他领域在短时记忆中的勾选仍保留）。
    /// </summary>
    [RelayCommand]
    private void WrongBookSelectAllInFilter()
    {
        if (WrongBookRows.Count == 0)
        {
            StatusMessage = "当前领域下暂无错题，请先刷新或更换筛选。";
            RefreshWrongBookSelectAllCaption();
            return;
        }

        var allSelected = WrongBookRows.All(r => _wrongBookAnalysisSelectionIds.Contains(r.QuestionId));
        if (allSelected)
        {
            foreach (var row in WrongBookRows)
            {
                _wrongBookAnalysisSelectionIds.Remove(row.QuestionId);
                row.SyncSelectionFromMemory(false);
            }

            StatusMessage = $"已取消当前列表全部勾选（领域：{WrongBookDomainFilter}）。仍勾选 {_wrongBookAnalysisSelectionIds.Count} 题（含其他领域）。";
        }
        else
        {
            foreach (var row in WrongBookRows)
            {
                _wrongBookAnalysisSelectionIds.Add(row.QuestionId);
                row.SyncSelectionFromMemory(true);
            }

            StatusMessage = $"已全选当前列表 {WrongBookRows.Count} 题（领域：{WrongBookDomainFilter}），累计勾选 {_wrongBookAnalysisSelectionIds.Count} 题。";
        }

        RefreshWrongBookSelectAllCaption();
    }

    /// <summary>
    /// 从错题本删除一条聚合记录（不影响题库题目与历史答题记录）。
    /// </summary>
    [RelayCommand]
    private async Task DeleteWrongBookEntryAsync(WrongBookRowVm? row)
    {
        if (row is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"确定从错题本移除该记录？\n题号：{row.QuestionId}",
            "删除错题本条目",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var entity = await db.WrongBookEntries.FirstOrDefaultAsync(e => e.Id == row.WrongBookEntryId).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = "记录已不存在。";
            await RefreshWrongBookAsync().ConfigureAwait(false);
            return;
        }

        db.WrongBookEntries.Remove(entity);
        await db.SaveChangesAsync().ConfigureAwait(false);
        _wrongBookAnalysisSelectionIds.Remove(row.QuestionId);
        StatusMessage = $"已删除错题本条目（题号 {row.QuestionId}）。";
        await RefreshWrongBookAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 刷新错题本表格：支持领域筛选，展示错答与标答（优先库内快照，否则尝试最近一次错误作答记录）。
    /// </summary>
    [RelayCommand]
    private async Task RefreshWrongBookAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var userId = DatabaseInitializer.DemoUserId;

        var joined = db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (w, q) => new { w, q });

        var domain = MapUiToDomainOrNull(WrongBookDomainFilter);
        if (domain is not null)
        {
            joined = joined.Where(x => x.q.Domain == domain);
        }

        var rows = await joined
            .OrderByDescending(x => x.w.LastWrongAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        var idsFallback = rows.Where(x => string.IsNullOrWhiteSpace(x.w.LastWrongUserAnswer)).Select(x => x.q.Id).ToList();
        Dictionary<long, string> fallbackAnswers = new();
        if (idsFallback.Count > 0)
        {
            var records = await db.AnswerRecords.AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsCorrect && idsFallback.Contains(a.QuestionId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var g in records.GroupBy(a => a.QuestionId))
            {
                fallbackAnswers[g.Key] = g.First().UserAnswer;
            }
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            WrongBookRows.Clear();
            foreach (var x in rows)
            {
                var wrongText = !string.IsNullOrWhiteSpace(x.w.LastWrongUserAnswer)
                    ? x.w.LastWrongUserAnswer!.Trim()
                    : (fallbackAnswers.TryGetValue(x.q.Id, out var fa) ? fa : "（无错答快照：可开启「长期保存错题详情」后重新交卷）");

                var row = new WrongBookRowVm(
                    x.w.Id,
                    x.q.Id,
                    MapDomainToUi(x.q.Domain),
                    MapTypeToUi(x.q.Type),
                    x.q.Stem,
                    x.q.OptionsJson,
                    wrongText,
                    x.q.StandardAnswer,
                    x.w.WrongCount,
                    x.w.LastWrongAtUtc,
                    x.w.LastRedoCompletedAtUtc,
                    OnWrongBookRowSelectionChanged);
                row.SyncSelectionFromMemory(_wrongBookAnalysisSelectionIds.Contains(x.q.Id));
                WrongBookRows.Add(row);
            }

            RefreshWrongBookSelectAllCaption();
        });

        StatusMessage = $"错题本已刷新：{rows.Count} 条（领域：{WrongBookDomainFilter}）。已勾选 {_wrongBookAnalysisSelectionIds.Count} 题用于 AI。";
    }

    /// <summary>
    /// 仅调用 AI 错题解析（单次 Ark 请求）：仅发送错题本中已勾选的题目（需含领域筛选与勾选状态）。
    /// </summary>
    [RelayCommand]
    private async Task AiTutorAnalyzeWrongAsync()
    {
        var ids = _wrongBookAnalysisSelectionIds.OrderBy(x => x).ToList();
        if (ids.Count == 0)
        {
            MessageBox.Show(
                "请先在错题列表中勾选要解析的题目（可切换领域筛选，勾选会短时记忆保留）。",
                "AI 错题解析",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            StatusMessage = "未勾选任何错题。";
            return;
        }

        var confirm = MessageBox.Show(
            $"将发送 {ids.Count} 道已勾选题到 AI 进行「错题解析」（不自动推荐题目或生成计划）。是否继续？",
            "AI 错题解析",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消 AI 错题解析。";
            return;
        }

        SetAiMainOutputLoading(true, "正在准备错题数据并发送 AI 解析请求…");
        var aiCts = new CancellationTokenSource();
        RegisterAiCancellation(aiCts);
        var token = aiCts.Token;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var userId = DatabaseInitializer.DemoUserId;
            var questions = await db.Questions.AsNoTracking()
                .Where(q => ids.Contains(q.Id))
                .ToListAsync(token)
                .ConfigureAwait(false);
            var entries = await db.WrongBookEntries.AsNoTracking()
                .Where(w => w.UserId == userId && ids.Contains(w.QuestionId))
                .ToListAsync(token)
                .ConfigureAwait(false);
            var wrongRecords = await db.AnswerRecords.AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsCorrect && ids.Contains(a.QuestionId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync(token)
                .ConfigureAwait(false);
            var lastAnswerByQ = wrongRecords
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.First().UserAnswer);

            var batch = new List<WrongQuestionInsightDto>();
            foreach (var id in ids)
            {
                var q = questions.FirstOrDefault(x => x.Id == id);
                if (q is null)
                {
                    continue;
                }

                var entry = entries.FirstOrDefault(e => e.QuestionId == id);
                string? ua = entry?.LastWrongUserAnswer;
                if (string.IsNullOrWhiteSpace(ua) && lastAnswerByQ.TryGetValue(id, out var fromRec))
                {
                    ua = fromRec;
                }

                ua ??= string.Empty;
                batch.Add(new WrongQuestionInsightDto
                {
                    QuestionId = q.Id,
                    Type = q.Type,
                    StemSummary = q.Stem.Length > 48 ? q.Stem[..48] + "…" : q.Stem,
                    StemFull = q.Stem,
                    OptionsJson = q.OptionsJson,
                    KnowledgeTags = q.KnowledgeTags,
                    UserAnswer = ua,
                    StandardAnswer = q.StandardAnswer,
                    RootCause = string.Empty,
                    SolutionHints = string.Empty,
                    OptionAnalysis = string.Empty
                });
            }

            if (batch.Count == 0)
            {
                MessageBox.Show("勾选的题目在题库中已不存在或已禁用。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Ui(() => AiMainOutputBusyMessage = "正在发送错题解析请求并等待 AI 返回…");
            AppendAiLog($"AI 错题解析：请求 Ark，共 {batch.Count} 道");
            SetAiPipeline("正在发送", "错题解析");
            var analyzed = await _aiTutor.AnalyzeWrongQuestionsAsync(batch, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            AiOutputText = FormatAiInsights(analyzed);
            ApplyAiTraceToUi("错题解析");
            StatusMessage = "AI 错题解析已完成。";
            _pendingWrongInsightsForAi.Clear();
        }
        catch (OperationCanceledException)
        {
            AppendAiLog("错题解析：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            StatusMessage = "已取消本次 AI 错题解析。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 错题解析失败");
            AiOutputText = "错题解析失败：" + FormatExceptionMessage(ex);
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog("错题解析：失败 — " + FormatExceptionMessage(ex));
            StatusMessage = "AI 错题解析失败。";
        }
        finally
        {
            UnregisterAiCancellation(aiCts);
            SetAiMainOutputLoading(false);
        }
    }

    /// <summary>
    /// 触发 AI 题目推荐并展示结果。
    /// </summary>
    [RelayCommand]
    private async Task RecommendAsync()
    {
        CancellationTokenSource? aiCts = null;
        try
        {
            var domainScope = MapUiToDomainOrNull(WrongBookDomainFilter);
            var selectedWrong = _wrongBookAnalysisSelectionIds.OrderBy(x => x).ToList();
            var dialogResult = MessageBox.Show(
                "是否发送请求给 AI 进行题目推荐？\n\n" +
                $"当前错题本「领域」筛选：{WrongBookDomainFilter}（非「全部」时，候选与错题统计均限定该领域）。\n" +
                $"已勾选错题：{selectedWrong.Count} 道（其 TopicTags/TopicKeywords 与知识点将写入请求；推荐结果会排除错题本中的题目）。",
                "AI 推荐",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (dialogResult != MessageBoxResult.Yes)
            {
                StatusMessage = "已取消 AI 题目推荐。";
                return;
            }

            SetAiMainOutputLoading(true, "正在发送题目推荐请求并等待 AI 返回…");
            aiCts = new CancellationTokenSource();
            RegisterAiCancellation(aiCts);
            var token = aiCts.Token;
            AppendAiLog("题目推荐：请求 Ark chat/completions");
            SetAiPipeline("正在发送", "题目推荐");
            var request = new QuestionRecommendationRequest
            {
                SelectedWrongQuestionIds = selectedWrong,
                DomainScope = domainScope
            };
            var dto = await _recommendation.RecommendAsync(DatabaseInitializer.DemoUserId, request, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _recommendedQuestionIds = dto.RecommendedQuestionIds.ToList();

            AiOutputText = await BuildRecommendationDisplayTextAsync(dto, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            ApplyAiTraceToUi("题目推荐");
            StatusMessage =
                $"AI 推荐已生成（{_recommendedQuestionIds.Count} 题）。如需刷题请点击「用推荐题开始考试」；学习计划请在学习计划页单独生成。";
        }
        catch (OperationCanceledException)
        {
            AppendAiLog("题目推荐：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            StatusMessage = "已取消本次 AI 题目推荐。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 推荐失败");
            AiOutputText = "题目推荐失败：" + FormatExceptionMessage(ex);
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog("题目推荐：失败 — " + FormatExceptionMessage(ex));
            StatusMessage = "AI 推荐失败。";
        }
        finally
        {
            if (aiCts is not null)
            {
                UnregisterAiCancellation(aiCts);
            }

            SetAiMainOutputLoading(false);
        }
    }

    /// <summary>
    /// 在已有推荐 Id 的前提下，确认后使用推荐题开始考试（不调用 AI）。
    /// </summary>
    [RelayCommand]
    private async Task StartExamFromRecommendationAsync()
    {
        if (_recommendedQuestionIds.Count == 0)
        {
            MessageBox.Show("请先在上方点击「AI 题目推荐」获取推荐题目列表。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusMessage = "暂无推荐题目，请先执行 AI 题目推荐。";
            return;
        }

        await ShowRecommendationDialogAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 显示推荐题目提示对话框，询问用户是否开始刷题。
    /// </summary>
    private async Task ShowRecommendationDialogAsync()
    {
        if (_recommendedQuestionIds.Count == 0)
        {
            MessageBox.Show("推荐题目数量为0，无法开始刷题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialogResult = MessageBox.Show($"AI 已推荐 {_recommendedQuestionIds.Count} 道题目，是否开始刷题？", "AI 推荐", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult == MessageBoxResult.Yes)
        {
            await StartExamWithRecommendedQuestionsAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 使用推荐题目开始考试。
    /// </summary>
    private async Task StartExamWithRecommendedQuestionsAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            
            var idOrder = _recommendedQuestionIds.Distinct().ToList();
            var recommendedQuestions = await db.Questions.AsNoTracking()
                .Where(q => q.IsEnabled && idOrder.Contains(q.Id))
                .ToListAsync()
                .ConfigureAwait(false);

            if (recommendedQuestions.Count == 0)
            {
                MessageBox.Show("无法获取推荐题目，可能题目已被删除。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var byId = recommendedQuestions.ToDictionary(q => q.Id);
            var orderedPaper = new List<Question>(idOrder.Count);
            foreach (var id in idOrder)
            {
                if (byId.TryGetValue(id, out var qq))
                {
                    orderedPaper.Add(qq);
                }
            }

            if (orderedPaper.Count == 0)
            {
                MessageBox.Show("无法获取推荐题目，可能题目已被删除。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var requested = Math.Clamp(ExamQuestionCount, 1, 50);
            var paper = BuildRandomPaperWithoutDuplicateIds(orderedPaper, requested);
            if (paper.Count == 0)
            {
                MessageBox.Show("无法组卷。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _examFromWrongBookRedoSession = false;
            _examQuestions.Clear();
            _examQuestions.AddRange(paper);
            _answersByDisplayIndex.Clear();
            _examSessionId = Guid.NewGuid();

            ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
            IsExamRunning = true;
            ExamDisplayIndex = 1;
            ExamUserAnswer = string.Empty;
            _examTimer.Start();
            
            RenderCurrentExamQuestion();
            var recShortfall = paper.Count < requested
                ? $" 设定 {requested} 题，推荐池仅 {paper.Count} 题，已按最大可用题量开考。"
                : string.Empty;
            StatusMessage = $"考试已开始：{paper.Count} 题（推荐范围内随机抽样、题号不重复），倒计时 {ExamRemainingSeconds} 秒。{recShortfall}";
            ExamStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用推荐题目开始考试失败");
            StatusMessage = "开始考试失败：" + ex.Message;
            MessageBox.Show("开始考试失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region CareerPath 技能包（CLI --import）

    /// <summary>
    /// 主窗口加载后调用：若启动参数含 <c>--import</c>，则加载 .skillpkg 并进入直接刷题或 AI 推荐流程。
    /// </summary>
    public async Task ProcessCareerPathStartupIfAnyAsync()
    {
        var path = CareerPathStartupState.ImportPath;
        var modeCli = CareerPathStartupState.ModeFromCli;
        var autoProceed = CareerPathStartupState.AutoProceedFromCli;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        CareerPathStartupState.ImportPath = null;
        CareerPathStartupState.ModeFromCli = null;
        CareerPathStartupState.AutoProceedFromCli = false;

        await ProcessCareerPathImportAsync(path, modeCli, autoProceed).ConfigureAwait(true);
    }

    /// <summary>
    /// 处理技能包（冷启动参数或来自已运行实例的 IPC）。<paramref name="importPath"/> 为空时仅将主窗口置前。
    /// </summary>
    public async Task ProcessCareerPathImportAsync(string? importPath, string? modeCli, bool autoProceed)
    {
        await _careerPathImportGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (string.IsNullOrWhiteSpace(importPath))
            {
                ActivateMainWindowForCareerPath();
                StatusMessage = "已切换到刷题软件，可继续刷题或 AI 推荐。";
                return;
            }

            ActivateMainWindowForCareerPath();

            if (!CareerPathSkillPackageJson.TryLoad(importPath, out var pkg, out var err))
            {
                MessageBox.Show(
                    err ?? "无法加载技能包。",
                    "CareerPath 技能包",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (pkg!.Source is { } src &&
                !src.Equals("careerpath_ai", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("技能包 source 非 careerpath_ai：{Source}", src);
            }

            var mode = CareerPathModeResolver.ResolveEffective(pkg, modeCli, _logger);
            var skills = (pkg.Skills ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (skills.Count == 0)
            {
                MessageBox.Show(
                    "技能包中 skills 为空，无法开始。",
                    "CareerPath 技能包",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var jc = pkg.JobContext;
            var jobSummary = jc?.JobSummary ?? string.Empty;
            var diffHint = jc?.Difficulty ?? string.Empty;
            var examOpts = pkg.ExamOptions;
            var examCountDefault = examOpts?.QuestionCount is >= 1 and <= 50
                ? examOpts.QuestionCount.Value
                : ExamQuestionCount;
            var mergedDiffHint = !string.IsNullOrWhiteSpace(examOpts?.Difficulty)
                ? examOpts!.Difficulty
                : diffHint;

            if (mode == CareerPathPracticeModeKind.Direct)
            {
                QuestionDomain? directDomain = null;
                DifficultyLevel? directDiff = null;
                var directCount = examCountDefault;

                if (!autoProceed)
                {
                    var perSkill = await CareerPathQuestionInventory.GetPerSkillCountsAsync(_dbFactory, skills)
                        .ConfigureAwait(true);
                    var byDom = await CareerPathQuestionInventory.GetMatchingCountsByDomainAsync(_dbFactory, skills)
                        .ConfigureAwait(true);
                    var infer = CareerPathDomainInference.InferDomain(skills, jobSummary);
                    var inferLine =
                        $"规则推断领域：{CareerPathDomainInference.MapDomainDisplay(infer)}。" +
                        "已检索题库：若某技术领域已有题目且与 JD/技能描述相近，补题时会优先归入该领域而非重复建类。";
                    var defaultDomainUi = ResolveCareerPathDefaultDomainUi(examOpts?.DomainHint, skills, jobSummary);
                    var defaultDiffUi = MapCareerPathDifficultyHint(mergedDiffHint) is { } dl
                        ? MapDifficultyToUi(dl)
                        : "全部";

                    var dlg = new CareerPathPracticePrepareDialog(
                        perSkill,
                        byDom,
                        inferLine,
                        examCountDefault,
                        defaultDomainUi,
                        defaultDiffUi)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    if (dlg.ShowDialog() != true)
                    {
                        StatusMessage = "已取消技能包刷题。";
                        return;
                    }

                    directDomain = MapUiToDomainOrNull(dlg.SelectedDomainUi);
                    directDiff = dlg.SelectedDifficultyUi == "全部"
                        ? null
                        : MapUiToDifficultyOrNull(dlg.SelectedDifficultyUi);
                    directCount = dlg.QuestionCount;
                }
                else
                {
                    directDomain = !string.IsNullOrWhiteSpace(examOpts?.DomainHint)
                        ? MapUiToDomainOrNull(examOpts!.DomainHint!.Trim())
                        : null;
                    directDiff = MapCareerPathDifficultyHint(mergedDiffHint);
                }

                await StartExamFromCareerPathSkillsAsync(
                        skills,
                        jobSummary,
                        directDiff,
                        directDomain,
                        directCount,
                        true)
                    .ConfigureAwait(true);
                ActivateMainWindowForCareerPath();
                return;
            }

            if (!autoProceed)
            {
                var r2 = MessageBox.Show(
                    "已加载 CareerPath 技能包。\n\n是否使用 AI 推荐题目并开始刷题？（将把 skills、岗位简述与难度传入推荐模块）",
                    "AI 推荐",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (r2 != MessageBoxResult.Yes)
                {
                    StatusMessage = "已取消 CareerPath AI 推荐。";
                    return;
                }
            }

            var recommendDomainScope = !string.IsNullOrWhiteSpace(examOpts?.DomainHint)
                ? MapUiToDomainOrNull(examOpts!.DomainHint!.Trim())
                : null;

            SetAiMainOutputLoading(true, "CareerPath：正在请求 AI 题目推荐…");
            CancellationTokenSource? cts = null;
            try
            {
                cts = new CancellationTokenSource();
                RegisterAiCancellation(cts);
                var token = cts.Token;
                var request = new QuestionRecommendationRequest
                {
                    ExternalSkillHints = skills,
                    ExternalJobSummary = string.IsNullOrWhiteSpace(jobSummary) ? null : jobSummary,
                    ExternalDifficultyHint = string.IsNullOrWhiteSpace(mergedDiffHint) ? null : mergedDiffHint,
                    DomainScope = recommendDomainScope
                };
                var dto = await _recommendation
                    .RecommendAsync(DatabaseInitializer.DemoUserId, request, token)
                    .ConfigureAwait(true);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                _recommendedQuestionIds = dto.RecommendedQuestionIds.ToList();
                AiOutputText = await BuildRecommendationDisplayTextAsync(dto, token).ConfigureAwait(true);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ApplyAiTraceToUi("题目推荐");
                StatusMessage = $"CareerPath：AI 推荐已生成（{_recommendedQuestionIds.Count} 题）。";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "已取消 CareerPath AI 推荐。";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CareerPath AI 推荐失败");
                AiOutputText = "题目推荐失败：" + FormatExceptionMessage(ex);
                StatusMessage = "CareerPath AI 推荐失败。";
                MessageBox.Show(
                    "AI 推荐失败：" + FormatExceptionMessage(ex),
                    "CareerPath",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            finally
            {
                if (cts is not null)
                {
                    UnregisterAiCancellation(cts);
                }

                SetAiMainOutputLoading(false);
            }

            if (_recommendedQuestionIds.Count == 0)
            {
                StatusMessage = "CareerPath：AI 推荐结果为空，未开考。";
                MessageBox.Show(
                    "未获得推荐题目，无法开考。请检查题库或稍后重试。",
                    "CareerPath",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (autoProceed)
            {
                await StartExamWithRecommendedQuestionsAsync().ConfigureAwait(true);
                ActivateMainWindowForCareerPath();
            }
            else
            {
                var r3 = MessageBox.Show(
                    $"AI 已推荐 {_recommendedQuestionIds.Count} 道题目，是否开始刷题？",
                    "CareerPath",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (r3 == MessageBoxResult.Yes)
                {
                    await StartExamWithRecommendedQuestionsAsync().ConfigureAwait(true);
                    ActivateMainWindowForCareerPath();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CareerPath 启动流程失败");
            MessageBox.Show(
                "处理技能包时出错：" + FormatExceptionMessage(ex),
                "CareerPath 技能包",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ActivateMainWindowForCareerPath();
            _careerPathImportGate.Release();
        }
    }

    private async Task StartExamFromCareerPathSkillsAsync(
        IReadOnlyList<string> skills,
        string? jobSummary,
        DifficultyLevel? difficultyFilter,
        QuestionDomain? domainScope,
        int questionCount,
        bool allowGenerateWhenEmpty = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = db.Questions.AsNoTracking().Where(x => x.IsEnabled);
        if (domainScope is { } dom)
        {
            query = query.Where(x => x.Domain == dom);
        }

        if (difficultyFilter is { } d)
        {
            query = query.Where(x => x.Difficulty == d);
        }

        var pool = await query.ToListAsync().ConfigureAwait(false);
        var matched = pool.Where(q => CareerPathQuestionFilter.MatchesAnySkill(q, skills)).ToList();
        if (matched.Count == 0)
        {
            if (allowGenerateWhenEmpty)
            {
                var generated = await ConfirmAndGenerateCareerPathQuestionsAsync(
                    skills,
                    jobSummary,
                    difficultyFilter,
                    domainScope).ConfigureAwait(true);
                if (generated)
                {
                    await StartExamFromCareerPathSkillsAsync(
                        skills,
                        jobSummary,
                        difficultyFilter,
                        domainScope,
                        questionCount,
                        allowGenerateWhenEmpty: false).ConfigureAwait(true);
                    return;
                }

                return;
            }

            MessageBox.Show(
                "题库中没有与技能包知识点匹配的题目。请为题库补充带相应 TopicTags、KnowledgeTags 或主知识点的题目。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            StatusMessage = "技能包知识点与题库无匹配，未开考。";
            return;
        }

        var requested = Math.Clamp(questionCount, 1, 50);
        var paper = BuildRandomPaperWithoutDuplicateIds(matched, requested);
        if (paper.Count == 0)
        {
            StatusMessage = "可用题目不足，无法组卷。";
            return;
        }

        _examFromWrongBookRedoSession = false;
        _examQuestions.Clear();
        _examQuestions.AddRange(paper);
        _answersByDisplayIndex.Clear();
        _examSessionId = Guid.NewGuid();

        ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
        IsExamRunning = true;
        ExamDisplayIndex = 1;
        ExamUserAnswer = string.Empty;
        _examTimer.Start();

        RenderCurrentExamQuestion();
        var diffNote = difficultyFilter is { } df ? $" 难度筛选：{MapDifficultyToUi(df)}。" : string.Empty;
        var shortfall = paper.Count < requested
            ? $" 设定 {requested} 题，匹配池仅 {paper.Count} 题，已按最大可用题量开考。"
            : string.Empty;
        StatusMessage =
            $"CareerPath 刷题已开始：{paper.Count} 题（按技能包知识点匹配、随机抽样）。{diffNote}{shortfall}";
        ExamStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 当 CareerPath 传入的知识点在题库中无匹配题时，先给用户一个明确的“是否生成 20 道题”选项，再进入详细参数面板。
    /// </summary>
    private async Task<bool> ConfirmAndGenerateCareerPathQuestionsAsync(
        IReadOnlyList<string> skills,
        string? jobSummary,
        DifficultyLevel? difficultyFilter,
        QuestionDomain? preferredDomain)
    {
        var confirm = await Application.Current.Dispatcher.InvokeAsync(() =>
            MessageBox.Show(
                $"题库中没有与当前知识点匹配的题目。{Environment.NewLine}{Environment.NewLine}" +
                $"是否生成 {CareerPathMissingQuestionDefaultCount} 道对应知识点的题并保存到题库？{Environment.NewLine}" +
                "点击“是”后仍可调整题目数量、题型和难度；系统会先检索最接近的领域，若该领域暂无题目，则写入该领域首批题目并按知识点归类。",
                "CareerPath 补题",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question));

        if (confirm != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消生成对应知识点题目，未开考。";
            return false;
        }

        return await PromptCareerPathQuestionGenerationAsync(
            skills,
            jobSummary,
            difficultyFilter,
            preferredDomain,
            CareerPathMissingQuestionDefaultCount).ConfigureAwait(true);
    }

    /// <summary>
    /// 准备对话框默认选中的领域：优先解析 CareerPath 传入的 <c>domain_hint</c>，否则按技能/JD 规则推断。
    /// </summary>
    private static string ResolveCareerPathDefaultDomainUi(
        string? domainHint,
        IReadOnlyList<string> skills,
        string? jobSummary)
    {
        if (!string.IsNullOrWhiteSpace(domainHint))
        {
            var t = domainHint.Trim();
            var mapped = MapUiToDomainOrNull(t);
            if (mapped is { } d)
            {
                return MapDomainToUi(d);
            }

            foreach (var opt in CareerPathPracticePrepareDialog.GetDomainUiOptionsForFilter())
            {
                if (string.Equals(opt, t, StringComparison.OrdinalIgnoreCase))
                {
                    return opt;
                }
            }
        }

        var inferred = CareerPathDomainInference.InferDomain(skills, jobSummary);
        return MapDomainToUi(inferred);
    }

    private static DifficultyLevel? MapCareerPathDifficultyHint(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return null;
        }

        var s = hint.Trim().ToLowerInvariant();
        if (s.Contains("难", StringComparison.Ordinal) || s == "hard")
        {
            return DifficultyLevel.Hard;
        }

        if (s.Contains("中", StringComparison.Ordinal) || s == "medium")
        {
            return DifficultyLevel.Medium;
        }

        if (s.Contains("简", StringComparison.Ordinal) || s == "easy")
        {
            return DifficultyLevel.Easy;
        }

        return null;
    }

    /// <summary>
    /// 由外部（如浏览器/实习通）拉起时，主窗口可能不在前台；在 CareerPath 流程中激活窗口以减少 MessageBox 被遮挡问题（与 <c>--auto</c> 配合更佳）。
    /// </summary>
private static void ActivateMainWindowForCareerPath()
    {
        var w = Application.Current?.MainWindow;
        if (w is null)
        {
            return;
        }

        try
        {
            if (!w.IsVisible)
            {
                w.Show();
            }

            if (w.WindowState == WindowState.Minimized)
            {
                w.WindowState = WindowState.Normal;
            }

            w.ShowInTaskbar = true;
            w.Topmost = true;
            w.Activate();
            w.BringIntoView();
            w.Focus();
            var handle = new WindowInteropHelper(w).Handle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, ShowWindowRestore);
                var foregroundHandle = GetForegroundWindow();
                var currentThreadId = GetCurrentThreadId();
                var foregroundThreadId = foregroundHandle == IntPtr.Zero
                    ? 0u
                    : GetWindowThreadProcessId(foregroundHandle, IntPtr.Zero);
                var attached = foregroundThreadId != 0 && foregroundThreadId != currentThreadId;
                try
                {
                    if (attached)
                    {
                        AttachThreadInput(currentThreadId, foregroundThreadId, true);
                    }

                    BringWindowToTop(handle);
                    SetWindowPos(handle, TopMostWindowHandle, 0, 0, 0, 0, SetWindowPosFlagsNoMoveOrSize | SetWindowPosFlagsShowWindow);
                    SetWindowPos(handle, NotTopMostWindowHandle, 0, 0, 0, 0, SetWindowPosFlagsNoMoveOrSize | SetWindowPosFlagsShowWindow);
                    SetForegroundWindow(handle);
                }
                finally
                {
                    if (attached)
                    {
                        AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                }
            }

            w.Topmost = false;
        }
        catch
        {
            // 忽略前台激活失败（最小化、权限等边界情况）
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    private const int ShowWindowRestore = 9;
    private const uint SetWindowPosFlagsNoMoveOrSize = 0x0001 | 0x0002;
    private const uint SetWindowPosFlagsShowWindow = 0x0040;
    private static readonly IntPtr TopMostWindowHandle = new(-1);
    private static readonly IntPtr NotTopMostWindowHandle = new(-2);

    #endregion

    /// <summary>
    /// 触发 AI 学习计划生成并展示结果。
    /// </summary>
    [RelayCommand]
    private async Task GenerateStudyPlanAsync()
    {
        var dialogResult = MessageBox.Show("是否发送请求给 AI 生成学习计划？", "AI 学习计划", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dialogResult != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消 AI 学习计划生成。";
            return;
        }

        await ExecuteGenerateStudyPlanCoreAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 执行学习计划生成（单次 Ark 请求），不含确认框；仅由 <see cref="GenerateStudyPlanAsync"/> 调用。
    /// </summary>
    private async Task ExecuteGenerateStudyPlanCoreAsync()
    {
        SetStudyPlanOutputLoading(true, "正在汇总答题数据并发送学习计划请求…");
        var aiCts = new CancellationTokenSource();
        RegisterAiCancellation(aiCts);
        var token = aiCts.Token;
        try
        {
            AppendAiLog("学习计划：请求 Ark chat/completions");
            SetAiPipeline("正在发送", "学习计划");
            await using var db = await _dbFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var userId = DatabaseInitializer.DemoUserId;

            var summary = await UserPerformanceSummaryFactory.CreateAsync(db, userId, token).ConfigureAwait(false);

            Ui(() => StudyPlanOutputBusyMessage = "正在调用 AI 生成学习计划，请稍候…");
            var plan = await _studyPlan.GeneratePlanAsync(summary, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            StudyPlanText =
                $"{plan.Title}\r\n阶段：{plan.PhaseDays} 天；每日：{plan.DailyQuestionQuota} 题。\r\n模块重点：{string.Join("、", plan.FocusKnowledgeTags)}\r\n知识点：{string.Join("、", plan.FocusKnowledgePoints)}\r\n说明：{plan.Notes}";
            ApplyAiTraceToUi("学习计划");
            StatusMessage = "学习计划已生成。";
        }
        catch (OperationCanceledException)
        {
            AppendAiLog("学习计划：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            StatusMessage = "已取消本次 AI 学习计划生成。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 学习计划生成失败");
            StudyPlanText = "学习计划生成失败：" + FormatExceptionMessage(ex);
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog("学习计划：失败 — " + FormatExceptionMessage(ex));
            StatusMessage = "AI 学习计划生成失败。";
        }
        finally
        {
            UnregisterAiCancellation(aiCts);
            SetStudyPlanOutputLoading(false);
        }
    }

    // 考试过程中保存每题作答（题号 -> 答案文本）。
    private readonly Dictionary<int, string> _answersByDisplayIndex = new();

    /// <summary>
    /// 将当前输入框内容写入当前题号对应的快照。
    /// </summary>
    private void StoreCurrentAnswerSnapshot()
    {
        if (_examQuestions.Count == 0)
        {
            return;
        }

        _answersByDisplayIndex[ExamDisplayIndex] = ExamUserAnswer ?? string.Empty;
    }

    /// <summary>
    /// 根据当前题号渲染题干与选项展示。
    /// </summary>
    private void RenderCurrentExamQuestion()
    {
        if (_examQuestions.Count == 0 || ExamDisplayIndex < 1 || ExamDisplayIndex > _examQuestions.Count)
        {
            ResetExamAnswerPanels();
            ExamStem = string.Empty;
            ExamTypeText = string.Empty;
            ExamDifficultyText = string.Empty;
            ExamOptionsDisplay = string.Empty;
            ExamProgressLabel = string.Empty;
            ExamUserAnswer = string.Empty;
            ExamAiExplanationText = string.Empty;
            ExamAiConclusionText = string.Empty;
            return;
        }

        ExamAiExplanationText = string.Empty;
        ExamAiConclusionText = string.Empty;
        var q = _examQuestions[ExamDisplayIndex - 1];
        ExamStem = q.Stem;
        ExamTypeText = MapTypeToUi(q.Type);
        ExamDifficultyText = MapDifficultyToUi(q.Difficulty);
        ExamOptionsDisplay = QuestionOptionsDisplayFormatter.FormatForDisplay(q.OptionsJson);
        ExamProgressLabel = $"第 {ExamDisplayIndex} / {_examQuestions.Count} 题 · 题库 Id={q.Id}";
        _questionStartedAtUtc = DateTime.UtcNow;

        if (_answersByDisplayIndex.TryGetValue(ExamDisplayIndex, out var cached))
        {
            ExamUserAnswer = cached;
        }
        else
        {
            ExamUserAnswer = string.Empty;
        }

        ResetExamAnswerPanels();
        ApplyExamAnswerModeForQuestion(q, ExamUserAnswer);
    }

    /// <summary>
    /// 关闭客观题按钮区与文本框区，避免题型切换时残留可见控件。
    /// </summary>
    private void ResetExamAnswerPanels()
    {
        ExamMcqOptions.Clear();
        ExamShowMcqSinglePanel = false;
        ExamShowMcqMultiPanel = false;
        ExamShowTrueFalsePanel = false;
        ExamShowTextAnswerPanel = false;
    }

    /// <summary>
    /// 按当前题目类型切换作答控件：客观题优先展示按钮；无选项 JSON 时退回文本输入。
    /// </summary>
    /// <param name="q">当前题目。</param>
    /// <param name="answerSnapshot">本题已缓存的答案文本。</param>
    private void ApplyExamAnswerModeForQuestion(Question q, string answerSnapshot)
    {
        switch (q.Type)
        {
            case QuestionType.SingleChoice:
            {
                var opts = ParseExamOptionRows(q.OptionsJson);
                if (opts.Count == 0)
                {
                    ExamShowTextAnswerPanel = true;
                    return;
                }

                foreach (var row in opts)
                {
                    var sel = string.Equals(answerSnapshot.Trim(), row.Key, StringComparison.OrdinalIgnoreCase);
                    ExamMcqOptions.Add(new ExamOptionItemVm { Key = row.Key, Caption = row.Caption, IsSelected = sel });
                }

                ExamShowMcqSinglePanel = true;
                return;
            }
            case QuestionType.MultipleChoice:
            {
                var opts = ParseExamOptionRows(q.OptionsJson);
                if (opts.Count == 0)
                {
                    ExamShowTextAnswerPanel = true;
                    return;
                }

                var picked = SplitMcqAnswerKeys(answerSnapshot);
                foreach (var row in opts)
                {
                    var sel = picked.Contains(row.Key, StringComparer.OrdinalIgnoreCase);
                    ExamMcqOptions.Add(new ExamOptionItemVm { Key = row.Key, Caption = row.Caption, IsSelected = sel });
                }

                ExamShowMcqMultiPanel = true;
                return;
            }
            case QuestionType.TrueFalse:
                ExamShowTrueFalsePanel = true;
                return;
            default:
                ExamShowTextAnswerPanel = true;
                return;
        }
    }

    /// <summary>
    /// 将选项 JSON 解析为 A/B/C… 键与展示行（与考试页原选项区文案一致）。
    /// </summary>
    private static List<(string Key, string Caption)> ParseExamOptionRows(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return new List<(string, string)>();
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(optionsJson);
            if (arr is null || arr.Count == 0)
            {
                return new List<(string, string)>();
            }

            var list = new List<(string, string)>();
            for (var i = 0; i < arr.Count; i++)
            {
                var label = ((char)('A' + i)).ToString();
                var body = QuestionOptionsDisplayFormatter.StripRedundantLeadingOptionPrefix(label, arr[i]);
                var caption = string.IsNullOrEmpty(body) ? $"{label}." : $"{label}. {body}";
                list.Add((label, caption));
            }

            return list;
        }
        catch
        {
            return new List<(string, string)>();
        }
    }

    /// <summary>
    /// 拆分多选题答案中的选项键（与判分器分隔符策略一致）。
    /// </summary>
    private static HashSet<string> SplitMcqAnswerKeys(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in text.Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (p.Length > 0)
            {
                set.Add(p);
            }
        }

        return set;
    }

    /// <summary>
    /// 计时器回调：每秒触发。
    /// </summary>
    private void OnExamTimerTick(object? sender, EventArgs e)
    {
        if (!IsExamRunning)
        {
            return;
        }

        ExamRemainingSeconds = Math.Max(0, ExamRemainingSeconds - 1);
        if (ExamRemainingSeconds != 0)
        {
            return;
        }

        _examTimer.Stop();
        _logger.LogWarning("倒计时结束，自动交卷。");

        // 回到 UI 调度器执行异步交卷，避免与计时器回调产生竞态。
        _ = Application.Current.Dispatcher.InvokeAsync(async () => await SubmitExamAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// 更新错题本聚合：答错时 WrongCount++ 或新增条目；若开启长期保存则写入最近一次错答快照。
    /// </summary>
    private static async Task UpsertWrongBookAsync(AppDbContext db, long userId, long questionId, string lastWrongAnswer, bool persistWrongDetails)
    {
        static string? TrimSnapshot(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            var t = s.Trim();
            return t.Length > 4000 ? t[..4000] : t;
        }

        var entry = await db.WrongBookEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.QuestionId == questionId)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var snapshot = persistWrongDetails ? TrimSnapshot(lastWrongAnswer) : null;

        if (entry is null)
        {
            db.WrongBookEntries.Add(new WrongBookEntry
            {
                UserId = userId,
                QuestionId = questionId,
                WrongCount = 1,
                LastWrongAtUtc = now,
                LastWrongUserAnswer = snapshot
            });
        }
        else
        {
            entry.WrongCount += 1;
            entry.LastWrongAtUtc = now;
            if (persistWrongDetails)
            {
                entry.LastWrongUserAnswer = snapshot;
            }
        }
    }

    /// <summary>
    /// 将 AI 解析结果格式化为可读文本。
    /// </summary>
    private static string FormatAiInsights(IReadOnlyList<WrongQuestionInsightDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AI 错题解析");
        foreach (var it in items)
        {
            sb.AppendLine("----");
            sb.AppendLine($"题号Id={it.QuestionId} [{MapTypeToUi(it.Type)}]");
            var stemBlock = !string.IsNullOrWhiteSpace(it.StemFull) ? it.StemFull : it.StemSummary;
            sb.AppendLine($"题干：{stemBlock}");
            if (!string.IsNullOrWhiteSpace(it.KnowledgeTags))
            {
                sb.AppendLine($"知识点标签：{it.KnowledgeTags}");
            }

            if (!string.IsNullOrWhiteSpace(it.OptionsJson))
            {
                sb.AppendLine("选项一览：");
                sb.AppendLine(QuestionOptionsDisplayFormatter.FormatForDisplay(it.OptionsJson).TrimEnd());
            }

            sb.AppendLine($"你的答案：{it.UserAnswer} / 标准：{it.StandardAnswer}");
            sb.AppendLine($"原因：{it.RootCause}");
            sb.AppendLine($"思路：{it.SolutionHints}");
            if (!string.IsNullOrWhiteSpace(it.OptionAnalysis))
            {
                sb.AppendLine("选项与要点辨析：");
                sb.AppendLine(it.OptionAnalysis);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 组装题目推荐展示：模型返回的理由与 Id 列表，并附题库中每道题的题型、难度、题干摘要与知识点标签。
    /// </summary>
    private async Task<string> BuildRecommendationDisplayTextAsync(
        QuestionRecommendationDto recommendation,
        CancellationToken cancellationToken = default)
    {
        var ids = recommendation.RecommendedQuestionIds;
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(recommendation.Rationale))
            sb.AppendLine(recommendation.Rationale.Trim());

        if (recommendation.FocusTags.Count > 0)
        {
            sb.AppendLine("AI 聚焦分类标签：" + string.Join("、", recommendation.FocusTags));
        }

        if (recommendation.FocusKeywords.Count > 0)
        {
            sb.AppendLine("AI 聚焦关键词：" + string.Join("、", recommendation.FocusKeywords));
        }

        sb.AppendLine("推荐题目 Id：");
        sb.AppendLine(string.Join(", ", ids));

        var detail = await FormatRecommendedQuestionsDetailAsync(ids, cancellationToken).ConfigureAwait(false);
        if (detail.Length > 0)
        {
            sb.AppendLine();
            sb.Append(detail);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 按推荐 Id 顺序查询题库，输出可读明细（解决界面仅显示数字 Id、无法对照具体题目的问题）。
    /// </summary>
    private async Task<string> FormatRecommendedQuestionsDetailAsync(
        IReadOnlyList<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return string.Empty;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var idList = ids.ToList();
        var found = await db.Questions.AsNoTracking()
            .Where(q => idList.Contains(q.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var byId = found.ToDictionary(q => q.Id);

        var sb = new StringBuilder();
        sb.AppendLine("推荐题目明细（来自题库）：");
        foreach (var id in idList)
        {
            if (!byId.TryGetValue(id, out var q))
            {
                sb.AppendLine($"  · Id={id} （题库中不存在或已删除）");
                continue;
            }

            var stem = q.Stem.Length > 72 ? q.Stem[..72] + "…" : q.Stem;
            var domainPart = $"领域:{MapDomainToUi(q.Domain)}";
            var tagPart = string.IsNullOrWhiteSpace(q.KnowledgeTags) ? string.Empty : $"  知识点:{q.KnowledgeTags}";
            var topicPart = string.IsNullOrWhiteSpace(q.TopicTags) ? string.Empty : $"  分类:{q.TopicTags}";
            var kwPart = string.IsNullOrWhiteSpace(q.TopicKeywords) ? string.Empty : $"  关键词:{q.TopicKeywords}";
            sb.AppendLine(
                $"  · Id={q.Id}  [{MapTypeToUi(q.Type)}·{MapDifficultyToUi(q.Difficulty)}] {domainPart} {stem}{tagPart}{topicPart}{kwPart}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string MapTypeToUi(QuestionType t) => t switch
    {
        QuestionType.MultipleChoice => "多选",
        QuestionType.TrueFalse => "判断",
        QuestionType.ShortAnswer => "简答",
        QuestionType.FillInBlank => "填空",
        _ => "单选"
    };

    private static string MapDifficultyToUi(DifficultyLevel d) => d switch
    {
        DifficultyLevel.Medium => "中等",
        DifficultyLevel.Hard => "困难",
        _ => "简单"
    };

    private static QuestionType MapUiToType(string ui) => ui switch
    {
        "多选" => QuestionType.MultipleChoice,
        "判断" => QuestionType.TrueFalse,
        "简答" => QuestionType.ShortAnswer,
        "填空" => QuestionType.FillInBlank,
        _ => QuestionType.SingleChoice
    };

    private static DifficultyLevel MapUiToDifficulty(string ui) => ui switch
    {
        "中等" => DifficultyLevel.Medium,
        "困难" => DifficultyLevel.Hard,
        _ => DifficultyLevel.Easy
    };

    private static QuestionType? MapUiToTypeOrNull(string ui) => ui switch
    {
        "全部" => null,
        "多选" => QuestionType.MultipleChoice,
        "判断" => QuestionType.TrueFalse,
        "简答" => QuestionType.ShortAnswer,
        "填空" => QuestionType.FillInBlank,
        "单选" => QuestionType.SingleChoice,
        _ => null
    };

    private static DifficultyLevel? MapUiToDifficultyOrNull(string ui) => ui switch
    {
        "全部" => null,
        "中等" => DifficultyLevel.Medium,
        "困难" => DifficultyLevel.Hard,
        "简单" => DifficultyLevel.Easy,
        _ => null
    };

    private static string MapDomainToUi(QuestionDomain d) => d switch
    {
        QuestionDomain.Python => "Python",
        QuestionDomain.C => "C",
        QuestionDomain.CPlusPlus => "C++",
        QuestionDomain.CSharp => "C#",
        QuestionDomain.Rust => "Rust",
        QuestionDomain.Java => "Java",
        QuestionDomain.JavaScript => "JavaScript",
        QuestionDomain.Go => "Go",
        QuestionDomain.DataStructure => "数据结构与算法",
        QuestionDomain.Database => "数据库",
        QuestionDomain.OperatingSystem => "操作系统",
        QuestionDomain.ComputerNetwork => "计算机网络",
        _ => "未分类"
    };

    private static QuestionDomain MapUiToDomain(string ui) => ui switch
    {
        "Python" => QuestionDomain.Python,
        "C" => QuestionDomain.C,
        "C++" => QuestionDomain.CPlusPlus,
        "C#" => QuestionDomain.CSharp,
        "Rust" => QuestionDomain.Rust,
        "Java" => QuestionDomain.Java,
        "JavaScript" => QuestionDomain.JavaScript,
        "Go" => QuestionDomain.Go,
        "数据结构与算法" => QuestionDomain.DataStructure,
        "数据库" => QuestionDomain.Database,
        "操作系统" => QuestionDomain.OperatingSystem,
        "计算机网络" => QuestionDomain.ComputerNetwork,
        _ => QuestionDomain.Uncategorized
    };

    private static QuestionDomain? MapUiToDomainOrNull(string ui) => ui switch
    {
        "全部" => null,
        "未分类" => QuestionDomain.Uncategorized,
        "Python" => QuestionDomain.Python,
        "C" => QuestionDomain.C,
        "C++" => QuestionDomain.CPlusPlus,
        "C#" => QuestionDomain.CSharp,
        "Rust" => QuestionDomain.Rust,
        "Java" => QuestionDomain.Java,
        "JavaScript" => QuestionDomain.JavaScript,
        "Go" => QuestionDomain.Go,
        "数据结构与算法" => QuestionDomain.DataStructure,
        "数据库" => QuestionDomain.Database,
        "操作系统" => QuestionDomain.OperatingSystem,
        "计算机网络" => QuestionDomain.ComputerNetwork,
        _ => null
    };

    /// <summary>
    /// 将题库管理页「AI 出题模板」显示名映射为领域题型枚举。
    /// </summary>
    /// <param name="templateDisplay">下拉选中项，例如「单选题模板」。</param>
    /// <returns>对应的 <see cref="QuestionType"/>。</returns>
    private static QuestionType MapBankAiTemplateToType(string templateDisplay) => templateDisplay switch
    {
        "多选题模板" => QuestionType.MultipleChoice,
        "判断题模板" => QuestionType.TrueFalse,
        "简答题模板" => QuestionType.ShortAnswer,
        "填空题模板" => QuestionType.FillInBlank,
        _ => QuestionType.SingleChoice
    };

    /// <summary>
    /// 仅在「新建题目」（未选中表格行）时可用：按编辑器中的领域、题型、难度与分类标签/关键词向 AI 请求一道题，校验通过后自动写入题库并同步表单。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    [RelayCommand(CanExecute = nameof(CanFillEditorQuestionFromAi))]
    private async Task FillEditorQuestionFromAiAsync()
    {
        if (SelectedBankQuestion is not null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"将按当前设置向 AI 请求 1 道题目：\n领域「{EditorDomain}」、题型「{EditorType}」、难度「{EditorDifficulty}」。\n" +
            "通过校验后将自动保存到题库并填入右侧编辑器。\n\n是否继续？",
            "AI 出题（新建）",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消 AI 出题。";
            return;
        }

        var sendHints = true;
        var tags = string.Empty;
        var kws = string.Empty;
        var editorDomainDisplay = string.Empty;
        var domain = QuestionDomain.Uncategorized;
        var templateType = QuestionType.SingleChoice;
        var diff = DifficultyLevel.Easy;
        Ui(() =>
        {
            IsEditorAiFillBusy = true;
            EditorAiProgressPhase = "准备发送…";
            sendHints = EditorAiSendTopicTagsToPrompt;
            tags = (EditorTopicTags ?? string.Empty).Trim();
            kws = (EditorTopicKeywords ?? string.Empty).Trim();
            editorDomainDisplay = EditorDomain;
            domain = MapUiToDomain(EditorDomain);
            templateType = MapUiToType(EditorType);
            diff = MapUiToDifficulty(EditorDifficulty);
        });
        var aiCts = new CancellationTokenSource();
        RegisterAiCancellation(aiCts);
        var token = aiCts.Token;
        try
        {
            AppendAiLog($"编辑器 AI 单题：领域={editorDomainDisplay}，题型={templateType}，难度={diff}");
            SetAiPipeline("正在发送", "编辑器 AI 单题");
            await Task.Delay(60, token).ConfigureAwait(false);
            Ui(() => EditorAiProgressPhase = "发送中，等待模型响应…");

            var hints = new QuestionBankGenerationHints
            {
                RequiredDifficulty = diff,
                TopicTagsHint = sendHints && !string.IsNullOrEmpty(tags) ? tags : null,
                TopicKeywordsHint = sendHints && !string.IsNullOrEmpty(kws) ? kws : null
            };

            var result = await _questionBankAi
                .GenerateQuestionsAsync(domain, editorDomainDisplay, templateType, 1, hints, token)
                .ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                return;
            }

            Ui(() => EditorAiProgressPhase = "解析与校验完成，正在填入表单…");
            await Task.Delay(80, token).ConfigureAwait(false);

            if (result.Questions.Count == 0)
            {
                var detail = result.Errors.Count > 0
                    ? string.Join(Environment.NewLine, result.Errors.Take(10))
                    : "模型未返回通过校验的题目。";
                Ui(() =>
                {
                    ApplyAiTraceToUi("编辑器 AI 单题");
                    StatusMessage = "AI 出题失败或无有效题目。";
                    AppendAiLog("编辑器 AI 单题失败详情：" + Environment.NewLine + detail);
                    MessageBox.Show(detail, "AI 出题", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            var q = result.Questions[0];
            ApplyAiTraceToUi("编辑器 AI 单题");

            if (result.Errors.Count > 0)
            {
                AppendAiLog("编辑器 AI 单题：另有未通过校验项 — " +
                             string.Join(" | ", result.Errors.Take(12)));
            }

            // 与「保存题目」新增分支一致：补齐标签与元数据后写入数据库，避免仅停留在表单导致关闭即丢。
            q.Domain = domain;
            q.KnowledgeTags = string.IsNullOrWhiteSpace(q.KnowledgeTags)
                ? (!string.IsNullOrWhiteSpace(editorDomainDisplay) ? editorDomainDisplay.Trim() : "未分类")
                : q.KnowledgeTags.Trim();
            q.OptionsJson = string.IsNullOrWhiteSpace(q.OptionsJson) ? null : q.OptionsJson.Trim();
            q.TopicTags = sendHints && !string.IsNullOrEmpty(tags)
                ? tags
                : (string.IsNullOrWhiteSpace(q.TopicTags) ? string.Empty : q.TopicTags.Trim());
            q.TopicKeywords = sendHints && !string.IsNullOrEmpty(kws)
                ? kws
                : (string.IsNullOrWhiteSpace(q.TopicKeywords) ? string.Empty : q.TopicKeywords.Trim());
            q.Stem = q.Stem.Trim();
            q.StandardAnswer = q.StandardAnswer.Trim();
            q.PrimaryKnowledgePoint = NormalizePrimaryKnowledgePoint(q.PrimaryKnowledgePoint, q.KnowledgeTags);
            q.IsEnabled = true;
            q.CreatedAtUtc = DateTime.UtcNow;

            await using (var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false))
            {
                db.Questions.Add(q);
                await db.SaveChangesAsync().ConfigureAwait(false);
            }

            AppendAiLog($"编辑器 AI 单题：已自动入库，题库 Id={q.Id}。");

            Ui(() =>
            {
                EditorStem = q.Stem;
                EditorStandardAnswer = q.StandardAnswer;
                EditorOptionsJson = string.IsNullOrWhiteSpace(q.OptionsJson) ? string.Empty : q.OptionsJson;
                EditorKnowledgeTags = string.IsNullOrWhiteSpace(q.KnowledgeTags) ? editorDomainDisplay : q.KnowledgeTags;
                EditorPrimaryKnowledgePoint = q.PrimaryKnowledgePoint ?? string.Empty;
                if (sendHints && !string.IsNullOrEmpty(tags))
                {
                    EditorTopicTags = tags;
                }
                else
                {
                    EditorTopicTags = string.IsNullOrWhiteSpace(q.TopicTags) ? string.Empty : q.TopicTags;
                }

                if (sendHints && !string.IsNullOrEmpty(kws))
                {
                    EditorTopicKeywords = kws;
                }
                else
                {
                    EditorTopicKeywords = string.IsNullOrWhiteSpace(q.TopicKeywords) ? string.Empty : q.TopicKeywords;
                }

                EditorType = MapTypeToUi(q.Type);
                EditorDifficulty = MapDifficultyToUi(q.Difficulty);
                SetAiPipeline("成功", "编辑器 AI 单题");
                MessageBox.Show("已成功生成并保存到题库。", "AI 出题", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = "已从 AI 生成并自动保存到题库，可在编辑器中继续修改后再点「保存题目」更新。";
            });

            await RefreshBankAsync().ConfigureAwait(false);
            Ui(() =>
            {
                var match = BankQuestions.FirstOrDefault(x => x.Id == q.Id);
                if (match is not null)
                {
                    SelectedBankQuestion = match;
                }
            });
        }
        catch (OperationCanceledException)
        {
            AppendAiLog("编辑器 AI 单题：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            Ui(() => StatusMessage = "已取消本次 AI 出题。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑器 AI 单题失败");
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog("编辑器 AI 单题：失败 — " + FormatExceptionMessage(ex));
            Ui(() =>
            {
                StatusMessage = "AI 出题失败：" + ex.Message;
                MessageBox.Show("AI 出题失败：" + FormatExceptionMessage(ex), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        finally
        {
            UnregisterAiCancellation(aiCts);
            Ui(() =>
            {
                IsEditorAiFillBusy = false;
                EditorAiProgressPhase = string.Empty;
            });
        }
    }

    /// <summary>
    /// <see cref="FillEditorQuestionFromAiCommand"/> 的可用性：新建模式且未在请求中。
    /// </summary>
    private bool CanFillEditorQuestionFromAi() =>
        SelectedBankQuestion is null && !IsEditorAiFillBusy && !IsBankAiGenerating;

    /// <summary>
    /// 刷新批量 AI 出题进度区：已入库数、剩余目标、百分比与「单批请求中」动画开关。
    /// </summary>
    private void PushBankAiProgress(int target, int generated, bool batchRequestInFlight)
    {
        var rem = Math.Max(0, target - generated);
        var pct = target > 0 ? Math.Min(100.0, 100.0 * generated / target) : 0.0;
        Ui(() =>
        {
            BankAiProgressTarget = target;
            BankAiProgressGenerated = generated;
            BankAiProgressRemaining = rem;
            BankAiProgressPercent = pct;
            BankAiBatchRequestActive = batchRequestInFlight;
            BankAiProgressStatsLine = $"已生成并入库 {generated} 道 · 目标 {target} 道 · 未生成 {rem} 道";
        });
    }

    /// <summary>
    /// 当 CareerPath 技能点在题库中无匹配题时，提示用户选择生成参数并自动补充一批对应题目。
    /// </summary>
    private async Task<bool> PromptCareerPathQuestionGenerationAsync(
        IReadOnlyList<string> skills,
        string? jobSummary,
        DifficultyLevel? difficultyFilter,
        QuestionDomain? preferredDomain,
        int defaultGenerationCount)
    {
        var normalizedSkills = CareerPathDomainInference.NormalizeSkillHints(skills, 12);
        var resolvedDomain = await CareerPathDomainResolution
            .ResolveDomainForGenerationAsync(_dbFactory, normalizedSkills, jobSummary)
            .ConfigureAwait(false);

        var domainChoices = CareerPathPracticePrepareDialog.GetDomainUiOptionsForFilter()
            .Where(x => x != "全部")
            .ToList();

        var defaultDisplay = preferredDomain is { } pd
            ? MapDomainToUi(pd)
            : MapDomainToUi(resolvedDomain);
        if (!domainChoices.Contains(defaultDisplay))
        {
            defaultDisplay = domainChoices.FirstOrDefault() ?? "未分类";
        }

        var chosenEnum = MapUiToDomain(defaultDisplay);
        var hasDomainQuestions = false;
        await using (var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false))
        {
            hasDomainQuestions = await db.Questions
                .AsNoTracking()
                .AnyAsync(x => x.IsEnabled && x.Domain == chosenEnum)
                .ConfigureAwait(false);
        }

        var scopeNote = preferredDomain is { } pref && pref != resolvedDomain
            ? $" 您筛选的领域为「{MapDomainToUi(pref)}」，默认已选中；系统推断为「{MapDomainToUi(resolvedDomain)}」，可在下拉中并入相近领域。"
            : $" 系统结合题库已有领域推断为「{MapDomainToUi(resolvedDomain)}」，避免重复建类。";
        var domainStateText =
            scopeNote +
            (hasDomainQuestions
                ? " 当前所选领域在题库中已有题目，新题将并入并按知识点归类。"
                : " 当前所选领域尚无题目，将写入该领域首批题目，相当于创建该领域并按知识点归类。");

        var dialogResult = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new CareerPathGenerateQuestionsDialog(
                domainChoices,
                defaultDisplay,
                domainStateText,
                normalizedSkills,
                Math.Clamp(defaultGenerationCount, 1, 96))
            {
                Owner = Application.Current.MainWindow
            };
            return (Result: dialog.ShowDialog(), Dialog: dialog);
        });
        if (dialogResult.Result != true)
        {
            StatusMessage = "已取消 CareerPath 补题。";
            return false;
        }

        var dialog = dialogResult.Dialog;
        var selectedDifficulty = dialog.SelectedDifficulty == "全部"
            ? difficultyFilter
            : MapUiToDifficultyOrNull(dialog.SelectedDifficulty);
        var knowledgeTagsHint = CareerPathDomainInference.BuildKnowledgeTagsHint(normalizedSkills, 10);
        var domainForGeneration = MapUiToDomain(dialog.SelectedDomainDisplay);
        var topicTagsHint = CareerPathDomainInference.BuildTopicTagsHint(domainForGeneration, normalizedSkills);
        var keywordsHint = string.Join("；", CareerPathDomainInference.NormalizeSkillHints(normalizedSkills, 12));
        var hints = new QuestionBankGenerationHints
        {
            RequiredDifficulty = selectedDifficulty,
            RandomizeDifficultyInBatch = selectedDifficulty is null,
            KnowledgeTagsHint = knowledgeTagsHint,
            TopicTagsHint = topicTagsHint,
            TopicKeywordsHint = string.IsNullOrWhiteSpace(jobSummary)
                ? keywordsHint
                : $"{keywordsHint}；{jobSummary.Trim()}"
        };

        var generated = await ExecuteBankAiGenerationAsync(new BankAiGenerationSessionOptions
        {
            Domain = domainForGeneration,
            DomainDisplayName = dialog.SelectedDomainDisplay.Trim(),
            TemplateType = MapUiToType(dialog.SelectedQuestionType),
            RequestedCount = dialog.GenerationCount,
            Hints = hints,
            OperationTitle = "CareerPath 补题",
            CompletionDialogTitle = "CareerPath 补题",
            ShowCompletionDialog = false
        }).ConfigureAwait(false);

        if (generated.Count == 0)
        {
            Ui(() => MessageBox.Show(
                "没有成功生成可入库题目，请检查 AI 日志或稍后重试。",
                "CareerPath 补题",
                MessageBoxButton.OK,
                MessageBoxImage.Warning));
            StatusMessage = "CareerPath 补题未写入题目。";
            return false;
        }

        StatusMessage = $"CareerPath 已补充 {generated.Count} 道题目，正在重新匹配知识点。";
        return true;
    }

    /// <summary>
    /// 统一执行批量 AI 出题入库流程，支持题库管理页与 CareerPath 缺题补题共用同一批处理与校验逻辑。
    /// </summary>
    private async Task<IReadOnlyList<Question>> ExecuteBankAiGenerationAsync(BankAiGenerationSessionOptions options)
    {
        Ui(() =>
        {
            IsBankAiGenerating = true;
            BankAiProgressPhase = "准备发送…";
        });
        var aiCts = new CancellationTokenSource();
        RegisterAiCancellation(aiCts);
        var token = aiCts.Token;
        var savedQuestions = new List<Question>();
        try
        {
            var totalRequested = Math.Clamp(options.RequestedCount, 1, BankAiGenerationMaxTotal);
            PushBankAiProgress(totalRequested, 0, false);

            AppendAiLog(
                $"{options.OperationTitle}：领域={options.DomainDisplayName}，题型={MapTypeToUi(options.TemplateType)}，" +
                $"数量={totalRequested}，随机难度={options.Hints.RandomizeDifficultyInBatch}");
            SetAiPipeline("正在发送", options.OperationTitle);
            await Task.Delay(60, token).ConfigureAwait(false);

            if (totalRequested != options.RequestedCount)
            {
                AppendAiLog($"{options.OperationTitle}：请求数量已从 {options.RequestedCount} 调整为上限 {BankAiGenerationMaxTotal}。");
            }

            var allErrors = new List<string>();
            string? lastSnippet = null;
            var minBatches = (int)Math.Ceiling(totalRequested / (double)BankAiGenerationChunkSize);
            var maxAttempts = minBatches + 10;

            for (var attempt = 0; attempt < maxAttempts && savedQuestions.Count < totalRequested; attempt++)
            {
                var chunk = Math.Min(BankAiGenerationChunkSize, totalRequested - savedQuestions.Count);
                if (chunk <= 0)
                {
                    break;
                }

                Ui(() => BankAiProgressPhase =
                    $"第 {attempt + 1} 批（每批≤{BankAiGenerationChunkSize} 题）：请求 {chunk} 题，已入库 {savedQuestions.Count}/{totalRequested}…");
                PushBankAiProgress(totalRequested, savedQuestions.Count, true);
                await Task.Delay(40, token).ConfigureAwait(false);

                var result = await _questionBankAi
                    .GenerateQuestionsAsync(
                        options.Domain,
                        options.DomainDisplayName,
                        options.TemplateType,
                        chunk,
                        options.Hints,
                        token)
                    .ConfigureAwait(false);

                allErrors.AddRange(result.Errors);
                if (!string.IsNullOrWhiteSpace(result.RawSnippet))
                {
                    lastSnippet = result.RawSnippet;
                }

                if (result.Questions.Count > 0)
                {
                    foreach (var item in result.Questions)
                    {
                        item.PrimaryKnowledgePoint = NormalizePrimaryKnowledgePoint(item.PrimaryKnowledgePoint, item.KnowledgeTags);
                    }

                    savedQuestions.AddRange(result.Questions);
                    await using (var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false))
                    {
                        db.Questions.AddRange(result.Questions);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }

                    AppendAiLog(
                        $"{options.OperationTitle}第 {attempt + 1} 批：本批入库 {result.Questions.Count} 题，累计 {savedQuestions.Count}/{totalRequested}。");
                }
                else
                {
                    AppendAiLog(
                        $"{options.OperationTitle}第 {attempt + 1} 批：未得到有效题目（本批校验失败 {result.Errors.Count} 条）。");
                }

                Ui(() => BankAiProgressPhase = $"写入完成一批，累计 {savedQuestions.Count}/{totalRequested}…");
                PushBankAiProgress(totalRequested, savedQuestions.Count, false);
                await Task.Delay(50, token).ConfigureAwait(false);
            }

            ApplyAiTraceToUi(options.OperationTitle);

            var sb = new StringBuilder();
            sb.AppendLine($"目标 {totalRequested} 题，实际入库 {savedQuestions.Count} 题（分多批请求，每批最多 {BankAiGenerationChunkSize} 题）。");
            if (savedQuestions.Count < totalRequested)
            {
                sb.AppendLine("提示：未完全达到目标题量，可能因部分批次校验未通过或达到重试上限。");
            }

            if (allErrors.Count > 0)
            {
                sb.AppendLine($"各批校验失败合计 {allErrors.Count} 条（摘录前 12 条）：");
                foreach (var error in allErrors.Take(12))
                {
                    sb.AppendLine(" - " + error);
                }

                if (allErrors.Count > 12)
                {
                    sb.AppendLine($" - …（其余 {allErrors.Count - 12} 条略）");
                }
            }

            if (!string.IsNullOrWhiteSpace(lastSnippet))
            {
                sb.AppendLine();
                sb.AppendLine("最后一批模型输出片段（截断）：");
                sb.AppendLine(lastSnippet);
            }

            var summary = sb.ToString().TrimEnd();
            AppendAiLog($"{options.OperationTitle}详情：" + Environment.NewLine + summary);
            StatusMessage = $"{options.OperationTitle}完成：已入库 {savedQuestions.Count} 题（目标 {totalRequested}）。";
            Ui(() =>
            {
                SetAiPipeline("成功", options.OperationTitle);
                if (!options.ShowCompletionDialog)
                {
                    return;
                }

                string msg;
                if (savedQuestions.Count == 0)
                {
                    msg = "未写入任何题目，详情请查看下方 AI 日志。";
                }
                else if (savedQuestions.Count < totalRequested)
                {
                    msg = $"已入库 {savedQuestions.Count} 题（目标 {totalRequested}）。未完全达标时请查看 AI 日志中的校验说明。";
                }
                else
                {
                    msg = $"已成功生成并入库 {savedQuestions.Count} 题。";
                }

                MessageBox.Show(
                    msg,
                    options.CompletionDialogTitle,
                    MessageBoxButton.OK,
                    savedQuestions.Count > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            });

            if (savedQuestions.Count > 0)
            {
                await RefreshBankAsync().ConfigureAwait(false);
            }

            return savedQuestions;
        }
        catch (OperationCanceledException)
        {
            AppendAiLog($"{options.OperationTitle}：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            StatusMessage = $"已取消 {options.OperationTitle}（已入库 {savedQuestions.Count} 题将保留）。";
            return savedQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationTitle}失败", options.OperationTitle);
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog($"{options.OperationTitle}：失败 — " + FormatExceptionMessage(ex));
            StatusMessage = $"{options.OperationTitle}失败：" + ex.Message;
            var partial = savedQuestions.Count > 0
                ? $"\n\n此前批次已成功入库 {savedQuestions.Count} 题，数据已保留。"
                : string.Empty;
            MessageBox.Show(
                $"{options.OperationTitle}失败：" + FormatExceptionMessage(ex) + partial,
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            if (savedQuestions.Count > 0)
            {
                await RefreshBankAsync().ConfigureAwait(false);
            }

            return savedQuestions;
        }
        finally
        {
            UnregisterAiCancellation(aiCts);
            Ui(() =>
            {
                IsBankAiGenerating = false;
                BankAiProgressPhase = string.Empty;
                BankAiBatchRequestActive = false;
                BankAiProgressTarget = 0;
                BankAiProgressGenerated = 0;
                BankAiProgressRemaining = 0;
                BankAiProgressPercent = 0;
                BankAiProgressStatsLine = string.Empty;
            });
        }
    }

    /// <summary>
    /// 调用 AI：按「刷新题库」旁筛选的领域、难度、题型批量生成并入库（题型为「全部」时用 AI 模板决定题型）；难度为「全部」时各批内随机简单/中等/困难。
    /// 多题时按 <see cref="BankAiGenerationChunkSize"/> 分多批调用模型，每批成功后立即入库，降低单次超时风险。
    /// </summary>
    [RelayCommand]
    private async Task GenerateBankQuestionsFromAiAsync()
    {
        var typeFromFilter = MapUiToTypeOrNull(SelectedTypeFilter);
        var templateType = typeFromFilter ?? MapBankAiTemplateToType(SelectedBankAiTemplate);
        var domain = MapUiToDomainOrNull(SelectedDomainFilter) ?? QuestionDomain.Uncategorized;
        var domainDisplay = SelectedDomainFilter;
        var diffOrNull = MapUiToDifficultyOrNull(SelectedDifficultyFilter);
        var bankAiHints = diffOrNull is null
            ? new QuestionBankGenerationHints { RandomizeDifficultyInBatch = true }
            : new QuestionBankGenerationHints { RequiredDifficulty = diffOrNull.Value };

        var typeExplain = typeFromFilter is not null
            ? $"题型筛选「{SelectedTypeFilter}」"
            : $"题型筛选为「全部」，题型按 AI 模板「{SelectedBankAiTemplate}」";
        var diffExplain = diffOrNull is null
            ? "难度筛选「全部」（本批各题难度在简单/中等/困难间随机并尽量均衡）"
            : $"难度筛选「{SelectedDifficultyFilter}」（本批均为该难度）";

        var confirm = MessageBox.Show(
            "将按上方「刷新题库」左侧筛选条件生成题目（与右侧题目编辑器中的领域/难度无关）：\n" +
            $"· 领域：「{SelectedDomainFilter}」\n" +
            $"· {typeExplain}\n" +
            $"· {diffExplain}\n" +
            $"· 目标生成数量：{BankAiGenCount}（实际不超过 {BankAiGenerationMaxTotal}；将分多批请求，每批最多 {BankAiGenerationChunkSize} 题，以降低超时风险）\n\n" +
            "模型须返回 JSON 数组；仅通过校验的题目会写入当前题库。\n\n是否继续？",
            "AI 按模板出题",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消 AI 出题。";
            return;
        }

        await ExecuteBankAiGenerationAsync(new BankAiGenerationSessionOptions
        {
            Domain = domain,
            DomainDisplayName = domainDisplay,
            TemplateType = templateType,
            RequestedCount = BankAiGenCount,
            Hints = bankAiHints,
            OperationTitle = "题库 AI 生成",
            CompletionDialogTitle = "AI 出题",
            ShowCompletionDialog = true
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 考试中：将当前题（含用户已输入）发送给 AI 获取解析，并在成功后弹出调侃提示。
    /// </summary>
    [RelayCommand]
    private async Task AskAiAboutCurrentExamQuestionAsync()
    {
        if (!IsExamRunning || _examQuestions.Count == 0 || ExamDisplayIndex < 1 || ExamDisplayIndex > _examQuestions.Count)
        {
            MessageBox.Show("请先开始考试并停留在某一题上。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StoreCurrentAnswerSnapshot();
        var q = _examQuestions[ExamDisplayIndex - 1];

        Ui(() => IsExamAiExplainBusy = true);
        var aiCts = new CancellationTokenSource();
        RegisterAiCancellation(aiCts);
        var token = aiCts.Token;
        try
        {
            AppendAiLog($"考试问 AI：题库 Id={q.Id}");
            SetAiPipeline("正在发送", "单题讲解");
            var explain = await _examQuestionAi.ExplainQuestionAsync(q, ExamUserAnswer, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            Ui(() =>
            {
                ExamAiConclusionText = explain.Conclusion;
                ExamAiExplanationText = explain.Detail;
            });
            ApplyAiTraceToUi("单题讲解");
            StatusMessage = "已获取当前题的 AI 解析。";
        }
        catch (OperationCanceledException)
        {
            AppendAiLog("单题讲解：用户已取消");
            SetAiPipeline("已取消", string.Empty);
            StatusMessage = "已取消本次 AI 单题讲解。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "考试单题 AI 讲解失败");
            SetAiPipeline("失败", FormatExceptionMessage(ex));
            AppendAiLog("单题讲解：失败 — " + FormatExceptionMessage(ex));
            StatusMessage = "单题讲解失败：" + ex.Message;
            MessageBox.Show("获取 AI 解析失败：" + FormatExceptionMessage(ex), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UnregisterAiCancellation(aiCts);
            Ui(() => IsExamAiExplainBusy = false);
        }
    }

    /// <summary>
    /// 导入题库：打开文件选择对话框，选择 JSON 文件并导入。
    /// </summary>
    [RelayCommand]
    private async Task ImportQuestionsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            Title = "选择题库导入文件",
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        var filePath = dialog.FileName;
        StatusMessage = $"正在导入：{Path.GetFileName(filePath)}...";

        try
        {
            var importResult = await _importService.ImportFromJsonFileAsync(filePath).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine($"导入完成：成功 {importResult.SuccessCount} 题，失败 {importResult.FailCount} 题。");

            if (importResult.Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("错误详情：");
                foreach (var error in importResult.Errors.Take(20))
                {
                    sb.AppendLine($"- {error}");
                }
                if (importResult.Errors.Count > 20)
                {
                    sb.AppendLine($"- ...（还有 {importResult.Errors.Count - 20} 条错误）");
                }
            }

            if (importResult.SuccessCount > 0)
            {
                await RefreshBankAsync().ConfigureAwait(false);
            }

            StatusMessage = sb.ToString().Trim();
            MessageBox.Show(sb.ToString(), "导入结果", MessageBoxButton.OK, importResult.SuccessCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入题库失败");
            var errorMsg = $"导入失败：{ex.Message}";
            StatusMessage = errorMsg;
            MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
