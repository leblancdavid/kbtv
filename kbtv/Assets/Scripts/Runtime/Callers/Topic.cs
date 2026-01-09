using System;
using UnityEngine;

namespace KBTV.Callers
{
    /// <summary>
    /// A screening rule that callers must pass to be accepted.
    /// </summary>
    [Serializable]
    public class ScreeningRule
    {
        [SerializeField] private string _description;
        [SerializeField] private ScreeningRuleType _ruleType;
        [SerializeField] private string _requiredValue;
        [SerializeField] private bool _isRequired;

        public string Description => _description;
        public ScreeningRuleType RuleType => _ruleType;
        public string RequiredValue => _requiredValue;
        public bool IsRequired => _isRequired;

        public ScreeningRule(ScreeningRuleType type, string value, string description, bool required = true)
        {
            _ruleType = type;
            _requiredValue = value;
            _description = description;
            _isRequired = required;
        }
    }

    /// <summary>
    /// Types of screening rules that can be applied.
    /// </summary>
    public enum ScreeningRuleType
    {
        /// <summary>Caller must claim to be calling about this topic</summary>
        TopicMustMatch,
        /// <summary>Caller must be from a specific location/area code</summary>
        LocationRequired,
        /// <summary>Caller must NOT be from a specific location</summary>
        LocationBanned,
        /// <summary>Caller's area code must match pattern</summary>
        AreaCodeRequired,
        /// <summary>Minimum legitimacy level required</summary>
        MinimumLegitimacy
    }

    /// <summary>
    /// Defines a show topic with associated screening rules.
    /// Topics determine what callers are valid for tonight's show.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTopic", menuName = "KBTV/Topic")]
    public class Topic : ScriptableObject
    {
        [Header("Topic Info")]
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private string _topicId;

        [Header("Screening Rules")]
        [SerializeField] private ScreeningRule[] _rules;

        [Header("Caller Generation")]
        [Tooltip("Keywords associated with this topic for caller generation")]
        [SerializeField] private string[] _keywords;
        
        [Tooltip("How likely callers are to lie about being on-topic (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _deceptionRate = 0.2f;

        [Tooltip("Base quality multiplier for on-topic callers")]
        [SerializeField] private float _qualityMultiplier = 1f;

        public string DisplayName => _displayName;
        public string Description => _description;
        public string TopicId => _topicId;
        public ScreeningRule[] Rules => _rules;
        public string[] Keywords => _keywords;
        public float DeceptionRate => _deceptionRate;
        public float QualityMultiplier => _qualityMultiplier;

        /// <summary>
        /// Check if a caller passes all screening rules for this topic.
        /// </summary>
        public ScreeningResult ScreenCaller(Caller caller)
        {
            if (_rules == null || _rules.Length == 0)
            {
                return new ScreeningResult(true, "No rules to check");
            }

            foreach (var rule in _rules)
            {
                if (!rule.IsRequired) continue;

                bool passed = EvaluateRule(caller, rule);
                if (!passed)
                {
                    return new ScreeningResult(false, $"Failed: {rule.Description}");
                }
            }

            return new ScreeningResult(true, "All rules passed");
        }

        private bool EvaluateRule(Caller caller, ScreeningRule rule)
        {
            return rule.RuleType switch
            {
                ScreeningRuleType.TopicMustMatch => 
                    caller.ClaimedTopic.Equals(rule.RequiredValue, StringComparison.OrdinalIgnoreCase) ||
                    caller.ClaimedTopic.Equals(_topicId, StringComparison.OrdinalIgnoreCase),

                ScreeningRuleType.LocationRequired => 
                    caller.Location.Contains(rule.RequiredValue, StringComparison.OrdinalIgnoreCase),

                ScreeningRuleType.LocationBanned => 
                    !caller.Location.Contains(rule.RequiredValue, StringComparison.OrdinalIgnoreCase),

                ScreeningRuleType.AreaCodeRequired => 
                    caller.PhoneNumber.StartsWith(rule.RequiredValue),

                ScreeningRuleType.MinimumLegitimacy => 
                    (int)caller.Legitimacy >= int.Parse(rule.RequiredValue),

                _ => true
            };
        }
    }

    /// <summary>
    /// Result of screening a caller against topic rules.
    /// </summary>
    public struct ScreeningResult
    {
        public bool Passed;
        public string Reason;

        public ScreeningResult(bool passed, string reason)
        {
            Passed = passed;
            Reason = reason;
        }
    }
}
