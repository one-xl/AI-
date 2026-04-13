using AiSmartDrill.App.Drill.Ai.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于Ark API的学习计划生成服务：调用火山引擎Ark API生成个性化刷题计划。
/// </summary>
public sealed class ApiStudyPlanService : IStudyPlanService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<ApiStudyPlanService> _logger;

    /// <summary>
    /// 初始化 <see cref="ApiStudyPlanService"/> 的新实例。
    /// </summary>
    /// <param name="chatCompletionService">聊天完成服务。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiStudyPlanService(
        IChatCompletionService chatCompletionService,
        ILogger<ApiStudyPlanService> logger)
    {
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto> GeneratePlanAsync(UserPerformanceSummary summary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 学习计划生成（API）：UserId={UserId}", summary.UserId);

        try
        {
            return await CallAiApiAsync(summary, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI API 调用失败，回退到本地计划生成");
            // 回退到本地计划生成
            return FallbackToLocalPlan(summary);
        }
    }

    private async Task<StudyPlanDto> CallAiApiAsync(UserPerformanceSummary summary, CancellationToken cancellationToken)
    {
        var weakTagsString = string.Join(", ", summary.WeakTags);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content = "教育AI助手：根据学生学习数据生成个性化刷题计划。"
            },
            new ChatMessage
            {
                Role = "user",
                Content = string.Format("生成计划：用户ID={0},总答题={1},正确={2},错题={3},弱项={4}\n\n严格要求：仅返回JSON格式，包含Title、DailyQuestionQuota、FocusKnowledgeTags、PhaseDays、Notes五个字段，不要包含其他任何文本。\n示例：{{\"Title\":\"计划标题\",\"DailyQuestionQuota\":5,\"FocusKnowledgeTags\":[\"知识点1\",\"知识点2\"],\"PhaseDays\":7,\"Notes\":\"计划说明\"}}" , summary.UserId, summary.TotalAttempts, summary.CorrectAttempts, summary.WrongBookCount, weakTagsString)
            }
        };

        var response = await _chatCompletionService.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);

        // 尝试解析AI返回的JSON
        var aiContent = response.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(aiContent))
            {
                throw new InvalidOperationException("AI API 返回内容为空");
            }
            var planResult = JsonSerializer.Deserialize<StudyPlanDto>(aiContent);
            return planResult ?? FallbackToLocalPlan(summary);
        }
        catch
        {
            // 如果AI返回的不是JSON格式，使用默认计划
            return FallbackToLocalPlan(summary);
        }
    }

    private StudyPlanDto FallbackToLocalPlan(UserPerformanceSummary summary)
    {
        // 基于用户数据生成默认计划
        var dailyQuota = summary.TotalAttempts > 0 ? Math.Max(5, summary.TotalAttempts / 10) : 5;
        var phaseDays = summary.WrongBookCount > 10 ? 14 : 7;

        return new StudyPlanDto
        {
            Title = "个性化学习计划",
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = summary.WeakTags.Count > 0 ? summary.WeakTags : new List<string> { "基础知识" },
            PhaseDays = phaseDays,
            Notes = "基于您的学习数据生成的默认计划。建议每天坚持练习，重点关注薄弱知识点。"
        };
    }


}