namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 按场景限制 <c>max_tokens</c>，缩短生成时间与 payload；长 JSON 场景按批量大小上浮上限。
/// </summary>
internal static class AiCompletionTokenBudgets
{
    public static int StudyPlanJson => 480;

    public static int RecommendationJson => 720;

    public static int ExamExplain => 640;

    public static int BankGeneration(int questionCount) =>
        Math.Clamp(420 + 240 * questionCount, 960, 3000);

    public static int TutorWrongBatch(int wrongCount) =>
        Math.Clamp(640 + 620 * wrongCount, 1800, 4096);
}
