using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using AiSmartDrill.App.Drill.Ai;
using AiSmartDrill.App.Drill.Grading;
using AiSmartDrill.App.Drill.Import;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
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
    private readonly AiCallTrace _aiTrace;
    private readonly ILogger<MainWindowViewModel> _logger;

    /// <summary>
    /// 考试计时器：每秒递减剩余秒数；到达 0 时自动交卷。
    /// </summary>
    private readonly DispatcherTimer _examTimer;

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
    /// 初始化 <see cref="MainWindowViewModel"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="aiTutor">AI 错题解析服务。</param>
    /// <param name="recommendation">AI 推荐服务。</param>
    /// <param name="studyPlan">AI 学习计划服务。</param>
    /// <param name="logger">日志记录器。</param>
    public MainWindowViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IAiTutorService aiTutor,
        IQuestionRecommendationService recommendation,
        IStudyPlanService studyPlan,
        QuestionImportService importService,
        AiCallTrace aiTrace,
        ILogger<MainWindowViewModel> logger)
    {
        _dbFactory = dbFactory;
        _aiTutor = aiTutor;
        _recommendation = recommendation;
        _studyPlan = studyPlan;
        _importService = importService;
        _aiTrace = aiTrace;
        _logger = logger;

        _examTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _examTimer.Tick += OnExamTimerTick;

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
    /// 编辑器：知识点标签。
    /// </summary>
    [ObservableProperty]
    private string _editorKnowledgeTags = string.Empty;

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

    /// <inheritdoc />
    public void Dispose()
    {
        _examTimer.Tick -= OnExamTimerTick;
        _examTimer.Stop();
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
        EditorStandardAnswer = value.StandardAnswer;
        EditorOptionsJson = value.OptionsJson ?? string.Empty;
        EditorKnowledgeTags = value.KnowledgeTags;
        EditorType = MapTypeToUi(value.Type);
        EditorDifficulty = MapDifficultyToUi(value.Difficulty);
        EditorDomain = MapDomainToUi(value.Domain);
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
                    UserAnswer = answer,
                    StandardAnswer = q.StandardAnswer,
                    RootCause = string.Empty,
                    SolutionHints = string.Empty
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
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var userId = DatabaseInitializer.DemoUserId;
            var questions = await db.Questions.AsNoTracking()
                .Where(q => ids.Contains(q.Id))
                .ToListAsync()
                .ConfigureAwait(false);
            var entries = await db.WrongBookEntries.AsNoTracking()
                .Where(w => w.UserId == userId && ids.Contains(w.QuestionId))
                .ToListAsync()
                .ConfigureAwait(false);
            var wrongRecords = await db.AnswerRecords.AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsCorrect && ids.Contains(a.QuestionId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync()
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
                    UserAnswer = ua,
                    StandardAnswer = q.StandardAnswer,
                    RootCause = string.Empty,
                    SolutionHints = string.Empty
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
            var analyzed = await _aiTutor.AnalyzeWrongQuestionsAsync(batch).ConfigureAwait(false);
            AiOutputText = FormatAiInsights(analyzed);
            ApplyAiTraceToUi("错题解析");
            StatusMessage = "AI 错题解析已完成。";
            _pendingWrongInsightsForAi.Clear();
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
            SetAiMainOutputLoading(false);
        }
    }

    /// <summary>
    /// 触发 AI 题目推荐并展示结果。
    /// </summary>
    [RelayCommand]
    private async Task RecommendAsync()
    {
        try
        {
            var dialogResult = MessageBox.Show("是否发送请求给 AI 进行题目推荐？", "AI 推荐", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult != MessageBoxResult.Yes)
            {
                StatusMessage = "已取消 AI 题目推荐。";
                return;
            }

            SetAiMainOutputLoading(true, "正在发送题目推荐请求并等待 AI 返回…");
            AppendAiLog("题目推荐：请求 Ark chat/completions");
            SetAiPipeline("正在发送", "题目推荐");
            var dto = await _recommendation.RecommendAsync(DatabaseInitializer.DemoUserId).ConfigureAwait(false);
            _recommendedQuestionIds = dto.RecommendedQuestionIds.ToList();

            AiOutputText = await BuildRecommendationDisplayTextAsync(dto).ConfigureAwait(false);
            ApplyAiTraceToUi("题目推荐");
            StatusMessage =
                $"AI 推荐已生成（{_recommendedQuestionIds.Count} 题）。如需刷题请点击「用推荐题开始考试」；学习计划请在学习计划页单独生成。";
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
        try
        {
            AppendAiLog("学习计划：请求 Ark chat/completions");
            SetAiPipeline("正在发送", "学习计划");
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var userId = DatabaseInitializer.DemoUserId;

            var total = await db.AnswerRecords.CountAsync(x => x.UserId == userId).ConfigureAwait(false);
            var correct = await db.AnswerRecords.CountAsync(x => x.UserId == userId && x.IsCorrect).ConfigureAwait(false);
            var wrongCount = await db.WrongBookEntries.CountAsync(x => x.UserId == userId).ConfigureAwait(false);

            var weakTags = await db.WrongBookEntries.AsNoTracking()
                .Where(w => w.UserId == userId)
                .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q.KnowledgeTags)
                .ToListAsync()
                .ConfigureAwait(false);

            var tagHistogram = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var tags in weakTags)
            {
                foreach (var t in tags.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    tagHistogram[t] = tagHistogram.TryGetValue(t, out var c) ? c + 1 : 1;
                }
            }

            var top = tagHistogram
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => kv.Key)
                .ToList();

            var summary = new UserPerformanceSummary
            {
                UserId = userId,
                TotalAttempts = total,
                CorrectAttempts = correct,
                WrongBookCount = wrongCount,
                WeakTags = top
            };

            Ui(() => StudyPlanOutputBusyMessage = "正在调用 AI 生成学习计划，请稍候…");
            var plan = await _studyPlan.GeneratePlanAsync(summary).ConfigureAwait(false);
            StudyPlanText =
                $"{plan.Title}\r\n阶段：{plan.PhaseDays} 天；每日：{plan.DailyQuestionQuota} 题。\r\n重点：{string.Join("、", plan.FocusKnowledgeTags)}\r\n说明：{plan.Notes}";
            ApplyAiTraceToUi("学习计划");
            StatusMessage = "学习计划已生成。";
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
            return;
        }

        var q = _examQuestions[ExamDisplayIndex - 1];
        ExamStem = q.Stem;
        ExamTypeText = MapTypeToUi(q.Type);
        ExamDifficultyText = MapDifficultyToUi(q.Difficulty);
        ExamOptionsDisplay = FormatOptionsForDisplay(q.OptionsJson);
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
    /// 去掉选项正文中与当前键重复的「字母 + 句点/顿号」前缀（支持嵌套如「A. A. 正文」），
    /// 以便 JSON 里已带「A. xxx」时不再与外层 <c>A. </c> 拼接成双前缀。
    /// </summary>
    /// <param name="optionKey">当前行键（单字符 A–Z）。</param>
    /// <param name="storedLine">JSON 数组中的原始字符串。</param>
    /// <returns>去重后的正文（不含行首键）。</returns>
    private static string StripRedundantLeadingOptionPrefix(string optionKey, string storedLine)
    {
        if (string.IsNullOrWhiteSpace(storedLine) || optionKey.Length != 1)
        {
            return storedLine.Trim();
        }

        var keyUpper = char.ToUpperInvariant(optionKey[0]);
        if (keyUpper is < 'A' or > 'Z')
        {
            return storedLine.Trim();
        }

        var t = storedLine.Trim();
        while (t.Length >= 2
               && char.ToUpperInvariant(t[0]) == keyUpper
               && (t[1] == '.' || t[1] == '．' || t[1] == '、'))
        {
            t = t[2..].TrimStart();
        }

        return t;
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
                var body = StripRedundantLeadingOptionPrefix(label, arr[i]);
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
            sb.AppendLine($"题干摘要：{it.StemSummary}");
            sb.AppendLine($"你的答案：{it.UserAnswer} / 标准：{it.StandardAnswer}");
            sb.AppendLine($"原因：{it.RootCause}");
            sb.AppendLine($"思路：{it.SolutionHints}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 将选项 JSON 解析为人类可读文本。
    /// </summary>
    private static string FormatOptionsForDisplay(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return "（本题无选项 JSON：若为客观题，请直接按标准格式作答，例如 A 或 A,C）";
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(optionsJson);
            if (arr is null || arr.Count == 0)
            {
                return optionsJson;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < arr.Count; i++)
            {
                var label = ((char)('A' + i)).ToString();
                var body = StripRedundantLeadingOptionPrefix(label, arr[i]);
                var line = string.IsNullOrEmpty(body) ? $"{label}." : $"{label}. {body}";
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            return optionsJson;
        }
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
            var tagPart = string.IsNullOrWhiteSpace(q.KnowledgeTags) ? string.Empty : $"  标签：{q.KnowledgeTags}";
            sb.AppendLine($"  · Id={q.Id}  [{MapTypeToUi(q.Type)}·{MapDifficultyToUi(q.Difficulty)}] {stem}{tagPart}");
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
