using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.Data
{
	/// <summary>
	/// Vern Tell's stat system v2.
	/// Three core stats (Physical, Emotional, Mental) + two dependencies (Caffeine, Nicotine).
	/// See docs/systems/VERN_STATS.md for full documentation.
	/// </summary>
	public partial class VernStats : Resource
	{
		[Signal] public delegate void StatsChangedEventHandler();
		[Signal] public delegate void VibeChangedEventHandler(float newVibe);
		[Signal] public delegate void MoodTypeChangedEventHandler(VernMoodType newMood);

		// ═══════════════════════════════════════════════════════════════════════════════
		// DEPENDENCIES - Decay over time, protect core stats from decay
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialCaffeine = 100f;
		[Export] private float _initialNicotine = 100f;

		[Export] private float _caffeineDecayRate = 30f;  // per minute (was 5f - increased for active dependency management)
		[Export] private float _nicotineDecayRate = 35f;  // per minute (was 4f - increased for active dependency management)

		// ═══════════════════════════════════════════════════════════════════════════════
		// CORE STATS - Range -100 to +100, start at 0
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialPhysical = 0f;
		[Export] private float _initialEmotional = 0f;
		[Export] private float _initialMental = 0f;

		// Core stat decay rates when dependencies are depleted (per minute)
		[Export] private float _physicalDecayRate = 6f;
		[Export] private float _emotionalDecayRate = 6f;
		[Export] private float _mentalDecayRate = 3f;  // Both dependencies affect mental

		// ═══════════════════════════════════════════════════════════════════════════════
		// STAT INTERACTION MULTIPLIERS
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _lowStatDecayMultiplier = 1.5f;  // +50% decay when stat < -25
		[Export] private float _lowMentalDependencyMultiplier = 1.25f;  // +25% dependency decay when Mental < -25

		// Runtime stat instances - Dependencies (0 to 100)
		private Stat _caffeine = null!;
		private Stat _nicotine = null!;

		// Runtime stat instances - Core Stats (-100 to +100)
		private Stat _physical = null!;
		private Stat _emotional = null!;
		private Stat _mental = null!;

		// Public accessors - Dependencies
		public Stat Caffeine => _caffeine;
		public Stat Nicotine => _nicotine;

		// Public accessors - Core Stats
		public Stat Physical => _physical;
		public Stat Emotional => _emotional;
		public Stat Mental => _mental;

		// Public decay rate accessors for monitors
		public float CaffeineDecayRate => _caffeineDecayRate;
		public float NicotineDecayRate => _nicotineDecayRate;
		public float PhysicalDecayRate => _physicalDecayRate;
		public float EmotionalDecayRate => _emotionalDecayRate;
		public float MentalDecayRate => _mentalDecayRate;

		// Interaction multipliers for monitors
		public float LowStatDecayMultiplier => _lowStatDecayMultiplier;
		public float LowMentalDependencyMultiplier => _lowMentalDependencyMultiplier;

		private VernMoodType _currentMoodType = VernMoodType.Neutral;
		private float _lastVibe = 0f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// INITIALIZATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Initialize runtime stats. Call this when starting a new game/night.
		/// </summary>
		public void Initialize()
		{
			// Dependencies (0 to 100, start full)
			_caffeine = new Stat("Caffeine", _initialCaffeine, 0f, 100f);
			_nicotine = new Stat("Nicotine", _initialNicotine, 0f, 100f);

			// Core Stats (-100 to +100, start at 0)
			_physical = new Stat("Physical", _initialPhysical, -100f, 100f);
			_emotional = new Stat("Emotional", _initialEmotional, -100f, 100f);
			_mental = new Stat("Mental", _initialMental, -100f, 100f);

			// Subscribe to individual stat changes
			SubscribeToStatChanges();

			_currentMoodType = VernMoodType.Neutral;
			_lastVibe = CalculateVIBE();

			EmitSignal("StatsChanged");
		}

		private void SubscribeToStatChanges()
		{
			_caffeine.OnValueChanged += NotifyStatsChanged;
			_nicotine.OnValueChanged += NotifyStatsChanged;
			_physical.OnValueChanged += NotifyStatsChanged;
			_emotional.OnValueChanged += NotifyStatsChanged;
			_mental.OnValueChanged += NotifyStatsChanged;
		}

		private void NotifyStatsChanged(float oldVal, float newVal)
		{
			EmitSignal("StatsChanged");

			// Check for VIBE change
			float newVibe = CalculateVIBE();
			if (Mathf.Abs(newVibe - _lastVibe) > 0.01f)
			{
				_lastVibe = newVibe;
				EmitSignal("VibeChanged", _lastVibe);
			}

			// Check for mood type change
			VernMoodType newMoodType = CalculateMoodType();
			if (newMoodType != _currentMoodType)
			{
				_currentMoodType = newMoodType;
				EmitSignal("MoodTypeChanged", (int)_currentMoodType);
			}
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// VIBE CALCULATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Calculate VIBE (Vibrancy, Interest, Broadcast Entertainment) from -100 to +100.
		/// VIBE = (Physical × 0.25) + (Emotional × 0.40) + (Mental × 0.35)
		/// </summary>
		public float CalculateVIBE()
		{
			float vibe = (_physical.Value * 0.25f) + (_emotional.Value * 0.40f) + (_mental.Value * 0.35f);
			return Mathf.Clamp(vibe, -100f, 100f);
		}

		/// <summary>
		/// Get current VIBE value without recalculating.
		/// </summary>
		public float CurrentVIBE => _lastVibe;

		// ═══════════════════════════════════════════════════════════════════════════════
		// MOOD TYPE CALCULATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Calculate Vern's current mood type based on stats.
		/// Priority order: Tired → Irritated → Energized → Amused → Focused → Gruff → Neutral
		/// </summary>
		public VernMoodType CalculateMoodType()
		{
			// Tired: Physical < -25
			if (_physical.Value < -25f)
				return VernMoodType.Tired;

			// Irritated: Emotional < -25
			if (_emotional.Value < -25f)
				return VernMoodType.Irritated;

			// Energized: Physical > +50
			if (_physical.Value > 50f)
				return VernMoodType.Energized;

			// Amused: Emotional > +50
			if (_emotional.Value > 50f)
				return VernMoodType.Amused;

			// Focused: Mental > +50
			if (_mental.Value > 50f)
				return VernMoodType.Focused;

			// Gruff: Emotional < 0 AND Mental > 0
			if (_emotional.Value < 0f && _mental.Value > 0f)
				return VernMoodType.Gruff;

			// Default: Neutral
			return VernMoodType.Neutral;
		}

		public VernMoodType CurrentMoodType => _currentMoodType;

		// ═══════════════════════════════════════════════════════════════════════════════
		// DEPENDENCY DECAY MODIFIERS
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Get caffeine decay rate modifier based on Mental stat.
		/// Higher Mental → slower caffeine decay.
		/// Formula: BaseRate × (1 - (Mental / 100))
		/// </summary>
		public float GetCaffeineDecayModifier()
		{
			// Mental at +100 → 0.5x decay (50%, minimum cap)
			// Mental at 0 → 1.0x decay (100%)
			// Mental at -100 → 2.0x decay (200%, double speed)
			float modifier = 1f - (_mental.Value / 100f);
			return Mathf.Max(0.5f, modifier);  // Cap at minimum 50% decay
		}

		/// <summary>
		/// Get nicotine decay rate modifier based on Emotional stat.
		/// Higher Emotional → slower nicotine decay.
		/// Formula: BaseRate × (1 - (Emotional / 100))
		/// </summary>
		public float GetNicotineDecayModifier()
		{
			// Emotional at +100 → 0.5x decay (50%, minimum cap)
			// Emotional at 0 → 1.0x decay (100%)
			// Emotional at -100 → 2.0x decay (200%, double speed)
			float modifier = 1f - (_emotional.Value / 100f);
			return Mathf.Max(0.5f, modifier);  // Cap at minimum 50% decay
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// STAT INTERACTION CHECKS
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Check if Physical is in critical state (< -25), which accelerates Mental decay.
		/// </summary>
		public bool IsPhysicalCritical => _physical.Value < -25f;

		/// <summary>
		/// Check if Emotional is in critical state (< -25), which accelerates Physical decay.
		/// </summary>
		public bool IsEmotionalCritical => _emotional.Value < -25f;

		/// <summary>
		/// Check if Mental is in critical state (< -25), which accelerates dependency decay.
		/// </summary>
		public bool IsMentalCritical => _mental.Value < -25f;

		/// <summary>
		/// Check if caffeine is depleted, causing Physical and Mental decay.
		/// </summary>
		public bool IsCaffeineDepleted => _caffeine.IsEmpty;

		/// <summary>
		/// Check if nicotine is depleted, causing Emotional and Mental decay.
		/// </summary>
		public bool IsNicotineDepleted => _nicotine.IsEmpty;

		// ═══════════════════════════════════════════════════════════════════════════════
		// CALLER EFFECTS
		// ═══════════════════════════════════════════════════════════════════════════════

 		/// <summary>
 		/// Apply effects from a good caller.
 		/// </summary>
 		[Obsolete("Use ApplyCallerEffects() for per-property effects")]
 		public void ApplyGoodCallerEffects()
 		{
 			_physical.Modify(5f);
 			_emotional.Modify(15f);
 			_mental.Modify(5f);
 		}

 		/// <summary>
 		/// Apply effects from a bad caller.
 		/// </summary>
 		[Obsolete("Use ApplyCallerEffects() for per-property effects")]
 		public void ApplyBadCallerEffects()
 		{
 			_physical.Modify(-3f);
 			_emotional.Modify(-15f);
 			_mental.Modify(-10f);
 		}

		/// <summary>
		/// Apply effects from catching a hoaxer during screening.
		/// </summary>
		public void ApplyHoaxerCaughtEffects()
		{
			_emotional.Modify(5f);
			_mental.Modify(10f);
		}

 		/// <summary>
 		/// Apply effects from being fooled by a hoaxer on-air.
 		/// </summary>
 		public void ApplyHoaxerFooledPenalty()
 		{
 			_emotional.Modify(-10f);
 			_mental.Modify(-15f);
 		}

 		/// <summary>
 		/// Apply per-property caller effects from aggregated stat dictionary.
 		/// </summary>
 		public void ApplyCallerEffects(Dictionary<StatType, float> effects)
 		{
 			foreach (var (statType, amount) in effects)
 			{
 				switch (statType)
 				{
 					case StatType.Physical: _physical.Modify(amount); break;
 					case StatType.Emotional: _emotional.Modify(amount); break;
 					case StatType.Mental: _mental.Modify(amount); break;
 					case StatType.Caffeine: _caffeine.Modify(amount); break;
 					case StatType.Nicotine: _nicotine.Modify(amount); break;
 				}
 			}
 		}

 		/// <summary>
 		/// Apply penalty for off-topic callers (hurts VIBE/listener engagement).
 		/// </summary>
 		public void ApplyOffTopicPenalty()
 		{
 			// VIBE penalty: -5 base + proportional to current Emotional/Mental
 			float vibePenalty = -5f;
 			_emotional.Modify(vibePenalty * 0.6f);  // -3 Emotional
 			_mental.Modify(vibePenalty * 0.4f);     // -2 Mental
 		}

 		/// <summary>
 		/// Apply dead air penalty with consecutive multiplier.
 		/// </summary>
 		public void ApplyDeadAirPenalty(int consecutiveCount)
 		{
 			// Base penalty: -5 VIBE, scales with consecutive dead air
 			float basePenalty = -5f;
 			float multiplier = 1f + (consecutiveCount - 1) * 0.5f; // Linear increase
 			float totalPenalty = basePenalty * multiplier;

 			_emotional.Modify(totalPenalty * 0.7f);  // 70% to Emotional
 			_mental.Modify(totalPenalty * 0.3f);     // 30% to Mental
 		}

 		// ═══════════════════════════════════════════════════════════════════════════════
 		// ITEM EFFECTS
 		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Use coffee: Caffeine → 100, Physical +10
		/// </summary>
		public void UseCoffee()
		{
			_caffeine.SetValue(100f);
			_physical.Modify(10f);
		}

		/// <summary>
		/// Use cigarette: Nicotine → 100, Emotional +5
		/// </summary>
		public void UseCigarette()
		{
			_nicotine.SetValue(100f);
			_emotional.Modify(5f);
		}
	}
}
