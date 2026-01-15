using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Callers
{
    /// <summary>
    /// A screening rule that callers must pass to be accepted.
    /// </summary>
    public partial class ScreeningRule : Resource
    {
        [Export] private string _description;
        [Export] private ScreeningRuleType _ruleType;
        [Export] private string _requiredValue;
        [Export] private bool _isRequired = true;

        public string Description => _description;
        public ScreeningRuleType RuleType => _ruleType;
        public string RequiredValue => _requiredValue;
        public bool IsRequired => _isRequired;

        /// <summary>
        /// Default constructor for Godot.
        /// </summary>
        public ScreeningRule() {}

        /// <summary>
        /// Constructor for programmatic rule creation.
        /// </summary>
        public ScreeningRule(string description, ScreeningRuleType ruleType, string requiredValue, bool isRequired = true)
        {
            _description = description;
            _ruleType = ruleType;
            _requiredValue = requiredValue;
            _isRequired = isRequired;
        }

        /// <summary>
        /// Set rule properties programmatically.
        /// </summary>
        public void SetProperties(string description, ScreeningRuleType ruleType, string requiredValue, bool isRequired = true)
        {
            _description = description;
            _ruleType = ruleType;
            _requiredValue = requiredValue;
            _isRequired = isRequired;
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
    public partial class Topic : Resource
    {
        [Export] private string _displayName;
        [Export] private string _description;
        [Export] private string _topicId;

        [Export] private Godot.Collections.Array<ScreeningRule> _rules = new Godot.Collections.Array<ScreeningRule>();

        [Export] private Godot.Collections.Array<string> _keywords = new Godot.Collections.Array<string>();

        [Export(PropertyHint.Range, "0,1,0.01")] private float _offTopicRate = 0.1f;
        [Export(PropertyHint.Range, "0,1,0.01")] private float _deceptionRate = 0.2f;
        [Export] private float _qualityMultiplier = 1f;

        /// <summary>
        /// Default constructor for Godot.
        /// </summary>
        public Topic() {}

        /// <summary>
        /// Constructor for programmatic topic creation.
        /// </summary>
        public Topic(string displayName, string topicId, string description = "")
        {
            _displayName = displayName;
            _topicId = topicId;
            _description = string.IsNullOrEmpty(description) ? $"Discuss {displayName.ToLower()}" : description;
        }

        public string DisplayName => _displayName;
        public string Description => _description;
        public string TopicId => _topicId;
        public Godot.Collections.Array<ScreeningRule> Rules => _rules;
        public Godot.Collections.Array<string> Keywords => _keywords;
        public float OffTopicRate => _offTopicRate;
        public float DeceptionRate => _deceptionRate;
        public float QualityMultiplier => _qualityMultiplier;

        /// <summary>
        /// Set topic properties programmatically (for runtime creation).
        /// </summary>
        public void SetProperties(float offTopicRate, float deceptionRate, float qualityMultiplier)
        {
            _offTopicRate = offTopicRate;
            _deceptionRate = deceptionRate;
            _qualityMultiplier = qualityMultiplier;
        }

        /// <summary>
        /// Set keywords programmatically.
        /// </summary>
        public void SetKeywords(Godot.Collections.Array<string> keywords)
        {
            _keywords = keywords;
        }

        /// <summary>
        /// Set rules programmatically.
        /// </summary>
        public void SetRules(Godot.Collections.Array<ScreeningRule> rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Check if a caller passes all screening rules for this topic.
        /// </summary>
        public ScreeningResult ScreenCaller(Caller caller)
        {
            if (_rules == null || _rules.Count == 0)
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