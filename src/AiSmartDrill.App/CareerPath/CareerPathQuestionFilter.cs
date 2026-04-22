using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 将技能包 <c>skills</c> 与题库题目做宽松匹配（分类标签、关键词、主知识点等）。
/// </summary>
public static class CareerPathQuestionFilter
{
    /// <summary>
    /// 若任一 skill 在题目的 TopicTags/KnowledgeTags、关键词字段、主知识点或题干中与题目命中，则视为入选。
    /// </summary>
    public static bool MatchesAnySkill(Question q, IReadOnlyList<string> skills)
    {
        if (skills.Count == 0)
        {
            return false;
        }

        foreach (var raw in skills)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var s = raw.Trim();
            if (RecommendationMatcher.TagFieldsContain(q, s))
            {
                return true;
            }

            if (RecommendationMatcher.KeywordHit(q, s))
            {
                return true;
            }

            if (RecommendationMatcher.KnowledgePointHit(q, s))
            {
                return true;
            }
        }

        return false;
    }
}
