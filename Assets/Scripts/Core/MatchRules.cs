using UnityEngine;

namespace ArenaCraft
{
    public enum MatchRuleSet
    {
        GddClassic,
        QuickMatch
    }

    public static class MatchRules
    {
        public const string RuleSetPreferenceKey = "ArenaCraft.RuleSet";

        public static MatchRuleSet Current =>
            (MatchRuleSet)PlayerPrefs.GetInt(RuleSetPreferenceKey, (int)MatchRuleSet.GddClassic);

        public static float ResourcePhaseDuration => Current == MatchRuleSet.GddClassic ? 180f : 75f;
        public static float ShoppingPhaseDuration => Current == MatchRuleSet.GddClassic ? 60f : 30f;
        public static float RespawnMultiplier => Current == MatchRuleSet.GddClassic ? 1f : 0.45f;

        public static void Select(MatchRuleSet ruleSet)
        {
            PlayerPrefs.SetInt(RuleSetPreferenceKey, (int)ruleSet);
        }
    }
}
