using System.Collections.ObjectModel;
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
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAiTutorService _aiTutor;
    private readonly IQuestionRecommendationService _recommendation;
    private readonly IStudyPlanService _studyPlan;
    private readonly QuestionImportService _importService;
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
        ILogger<MainWindowViewModel> logger)
    {
        _dbFactory = dbFactory;
        _aiTutor = aiTutor;
        _recommendation = recommendation;
        _studyPlan = studyPlan;
        _importService = importService;
        _logger = logger;

        _examTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _examTimer.Tick += OnExamTimerTick;

        TypeFilterOptions = new ObservableCollection<string> { "全部", "单选", "多选", "判断", "简答" };
        DifficultyFilterOptions = new ObservableCollection<string> { "全部", "简单", "中等", "困难" };
        EditorTypeOptions = new ObservableCollection<string> { "单选", "多选", "判断", "简答" };
        EditorDifficultyOptions = new ObservableCollection<string> { "简单", "中等", "困难" };

        SelectedTypeFilter = "全部";
        SelectedDifficultyFilter = "全部";

        ExamQuestionCount = 5;
        ExamMinutes = 10;
        ExamUserAnswer = string.Empty;
        StatusMessage = "就绪：请先刷新题库。";
    }

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
    /// 用户当前作答输入。
    /// </summary>
    [ObservableProperty]
    private string _examUserAnswer = string.Empty;

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
    /// 错题本列表（只读展示）。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _wrongBookLines = new();

    /// <summary>
    /// 学习计划输出。
    /// </summary>
    [ObservableProperty]
    private string _studyPlanText = string.Empty;

    /// <summary>
    /// 状态栏提示信息。
    /// </summary>
    [ObservableProperty]
    private string _statusMessage;

    /// <inheritdoc />
    public void Dispose()
    {
        _examTimer.Tick -= OnExamTimerTick;
        _examTimer.Stop();
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
    }

    /// <summary>
    /// 刷新题库列表（应用题型+难度 AND 筛选）。
    /// </summary>
    [RelayCommand]
    private async Task RefreshBankAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
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

        var list = await q.OrderByDescending(x => x.Id).ToListAsync().ConfigureAwait(true);
        BankQuestions = new ObservableCollection<Question>(list);
        StatusMessage = $"题库已刷新：{list.Count} 条（筛选：{SelectedTypeFilter} + {SelectedDifficultyFilter}）。";
    }

    /// <summary>
    /// 保存题目（新增或更新）。
    /// </summary>
    [RelayCommand]
    private async Task SaveQuestionAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorStem) || string.IsNullOrWhiteSpace(EditorStandardAnswer))
        {
            StatusMessage = "题干与标准答案不能为空。";
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
        var type = MapUiToType(EditorType);
        var diff = MapUiToDifficulty(EditorDifficulty);

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
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            var entity = await db.Questions.FirstOrDefaultAsync(x => x.Id == SelectedBankQuestion.Id).ConfigureAwait(true);
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
        }

        await db.SaveChangesAsync().ConfigureAwait(true);
        StatusMessage = "题目已保存。";
        await RefreshBankAsync().ConfigureAwait(true);
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
        EditorKnowledgeTags = "示例标签,基础";
        EditorType = "单选";
        EditorDifficulty = "简单";
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

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
        var entity = await db.Questions.FirstOrDefaultAsync(x => x.Id == SelectedBankQuestion.Id).ConfigureAwait(true);
        if (entity is null)
        {
            StatusMessage = "题目不存在。";
            return;
        }

        db.Questions.Remove(entity);
        await db.SaveChangesAsync().ConfigureAwait(true);
        SelectedBankQuestion = null;
        StatusMessage = "题目已删除。";
        await RefreshBankAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// 随机组卷并开始考试（限时）。
    /// </summary>
    [RelayCommand]
    private async Task StartExamAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
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

        var pool = await q.ToListAsync().ConfigureAwait(true);
        if (pool.Count == 0)
        {
            StatusMessage = "题库筛选结果为空，无法组卷。";
            return;
        }

        var take = Math.Clamp(ExamQuestionCount, 1, 50);
        var rng = new Random();
        var paper = pool.OrderBy(_ => rng.Next()).Take(take).ToList();

        _examQuestions.Clear();
        _examQuestions.AddRange(paper);
        _examSessionId = Guid.NewGuid();

        ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
        IsExamRunning = true;
        ExamDisplayIndex = 1;
        ExamUserAnswer = string.Empty;
        _examTimer.Start();

        RenderCurrentExamQuestion();
        StatusMessage = $"考试已开始：{paper.Count} 题，倒计时 {ExamRemainingSeconds} 秒。";
    }

    /// <summary>
    /// 从错题本抽取题目开始专项练习（同样走倒计时与交卷判分）。
    /// </summary>
    [RelayCommand]
    private async Task StartWrongBookPracticeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
        var wrongIds = await db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == DatabaseInitializer.DemoUserId)
            .Select(w => w.QuestionId)
            .ToListAsync()
            .ConfigureAwait(true);

        if (wrongIds.Count == 0)
        {
            StatusMessage = "错题本为空。";
            return;
        }

        var qs = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled && wrongIds.Contains(q.Id))
            .ToListAsync()
            .ConfigureAwait(true);

        var rng = new Random();
        var paper = qs.OrderBy(_ => rng.Next()).Take(Math.Clamp(ExamQuestionCount, 1, 50)).ToList();

        _examQuestions.Clear();
        _examQuestions.AddRange(paper);
        _examSessionId = Guid.NewGuid();

        ExamRemainingSeconds = Math.Clamp(ExamMinutes, 1, 180) * 60;
        IsExamRunning = true;
        ExamDisplayIndex = 1;
        ExamUserAnswer = string.Empty;
        _examTimer.Start();

        RenderCurrentExamQuestion();
        StatusMessage = $"错题练习已开始：{paper.Count} 题。";
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
        ExamDisplayIndex = Math.Max(1, ExamDisplayIndex - 1);
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
        ExamDisplayIndex = Math.Min(_examQuestions.Count, ExamDisplayIndex + 1);
        RenderCurrentExamQuestion();
    }

    /// <summary>
    /// 交卷：自动判分、写入答题记录、更新错题本，并触发 AI 错题解析调用链。
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

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
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
                await UpsertWrongBookAsync(db, userId, q.Id).ConfigureAwait(true);

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

        await db.SaveChangesAsync().ConfigureAwait(true);

        LastScoreText = $"{correct}/{_examQuestions.Count}";
        StatusMessage = $"交卷完成：{LastScoreText}。错题数：{wrongInsights.Count}。";

        if (wrongInsights.Count > 0)
        {
            var analyzed = await _aiTutor.AnalyzeWrongQuestionsAsync(wrongInsights).ConfigureAwait(true);
            AiOutputText = FormatAiInsights(analyzed);
        }
        else
        {
            AiOutputText = "本次没有错题，AI 解析跳过。";
        }

        _examQuestions.Clear();
        _answersByDisplayIndex.Clear();
        }
        finally
        {
            Interlocked.Exchange(ref _submitGate, 0);
        }
    }

    /// <summary>
    /// 刷新错题本列表展示。
    /// </summary>
    [RelayCommand]
    private async Task RefreshWrongBookAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
        var rows = await db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == DatabaseInitializer.DemoUserId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (w, q) => new { w.WrongCount, w.LastWrongAtUtc, q.Stem, q.Type, q.Difficulty })
            .OrderByDescending(x => x.LastWrongAtUtc)
            .ToListAsync()
            .ConfigureAwait(true);

        var lines = rows.Select(x =>
                $"[{x.LastWrongAtUtc:yyyy-MM-dd}] {MapTypeToUi(x.Type)} / {MapDifficultyToUi(x.Difficulty)} / 错{x.WrongCount}次 — {x.Stem}")
            .ToList();

        WrongBookLines = new ObservableCollection<string>(lines);
        StatusMessage = $"错题本已刷新：{lines.Count} 条。";
    }

    /// <summary>
    /// 触发 AI 题目推荐并展示结果。
    /// </summary>
    [RelayCommand]
    private async Task RecommendAsync()
    {
        var dto = await _recommendation.RecommendAsync(DatabaseInitializer.DemoUserId).ConfigureAwait(true);
        var sb = new StringBuilder();
        sb.AppendLine(dto.Rationale);
        sb.AppendLine("推荐题目 Id：");
        sb.AppendLine(string.Join(", ", dto.RecommendedQuestionIds));
        AiOutputText = sb.ToString();
        StatusMessage = "AI 推荐已生成（占位实现）。";
    }

    /// <summary>
    /// 触发 AI 学习计划生成并展示结果。
    /// </summary>
    [RelayCommand]
    private async Task GenerateStudyPlanAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(true);
        var userId = DatabaseInitializer.DemoUserId;

        var total = await db.AnswerRecords.CountAsync(x => x.UserId == userId).ConfigureAwait(true);
        var correct = await db.AnswerRecords.CountAsync(x => x.UserId == userId && x.IsCorrect).ConfigureAwait(true);
        var wrongCount = await db.WrongBookEntries.CountAsync(x => x.UserId == userId).ConfigureAwait(true);

        var weakTags = await db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q.KnowledgeTags)
            .ToListAsync()
            .ConfigureAwait(true);

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

        var plan = await _studyPlan.GeneratePlanAsync(summary).ConfigureAwait(true);
        StudyPlanText =
            $"{plan.Title}\r\n阶段：{plan.PhaseDays} 天；每日：{plan.DailyQuestionQuota} 题。\r\n重点：{string.Join("、", plan.FocusKnowledgeTags)}\r\n说明：{plan.Notes}";
        StatusMessage = "学习计划已生成。";
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
            ExamStem = string.Empty;
            ExamTypeText = string.Empty;
            ExamDifficultyText = string.Empty;
            ExamOptionsDisplay = string.Empty;
            ExamUserAnswer = string.Empty;
            return;
        }

        var q = _examQuestions[ExamDisplayIndex - 1];
        ExamStem = q.Stem;
        ExamTypeText = MapTypeToUi(q.Type);
        ExamDifficultyText = MapDifficultyToUi(q.Difficulty);
        ExamOptionsDisplay = FormatOptionsForDisplay(q.OptionsJson);
        _questionStartedAtUtc = DateTime.UtcNow;

        if (_answersByDisplayIndex.TryGetValue(ExamDisplayIndex, out var cached))
        {
            ExamUserAnswer = cached;
        }
        else
        {
            ExamUserAnswer = string.Empty;
        }
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
        _ = Application.Current.Dispatcher.InvokeAsync(async () => await SubmitExamAsync().ConfigureAwait(true));
    }

    /// <summary>
    /// 更新错题本聚合：答错时 WrongCount++ 或新增条目。
    /// </summary>
    private static async Task UpsertWrongBookAsync(AppDbContext db, long userId, long questionId)
    {
        var entry = await db.WrongBookEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.QuestionId == questionId)
            .ConfigureAwait(true);

        var now = DateTime.UtcNow;
        if (entry is null)
        {
            db.WrongBookEntries.Add(new WrongBookEntry
            {
                UserId = userId,
                QuestionId = questionId,
                WrongCount = 1,
                LastWrongAtUtc = now
            });
        }
        else
        {
            entry.WrongCount += 1;
            entry.LastWrongAtUtc = now;
        }
    }

    /// <summary>
    /// 将 AI 解析结果格式化为可读文本。
    /// </summary>
    private static string FormatAiInsights(IReadOnlyList<WrongQuestionInsightDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AI 错题解析（占位服务输出）");
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
                var label = (char)('A' + i);
                sb.AppendLine($"{label}. {arr[i]}");
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            return optionsJson;
        }
    }

    private static string MapTypeToUi(QuestionType t) => t switch
    {
        QuestionType.MultipleChoice => "多选",
        QuestionType.TrueFalse => "判断",
        QuestionType.ShortAnswer => "简答",
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
            var importResult = await _importService.ImportFromJsonFileAsync(filePath).ConfigureAwait(true);

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
                await RefreshBankAsync().ConfigureAwait(true);
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
