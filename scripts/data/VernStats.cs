using System;
using Godot;

namespace KBTV.Data
{
	/// <summary>
	/// Vern Tell's complete stat system.
	/// Tracks dependencies, physical state, spirit, cognitive performance, and long-term metrics.
	/// See docs/VERN_STATS.md for full documentation.
	/// </summary>
	public partial class VernStats : Resource
	{
		[Signal] public delegate void StatsChangedEventHandler();
		[Signal] public delegate void VibeChangedEventHandler(float newVibe);
		[Signal] public delegate void MoodTypeChangedEventHandler(VernMoodType newMood);

		// ═══════════════════════════════════════════════════════════════════════════════
		// DEPENDENCIES - Decay-only, cause withdrawal when low
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialCaffeine = 50f;
		[Export] private float _initialNicotine = 50f;

		[Export] private float _caffeineDecayRate = 5f;
		[Export] private float _nicotineDecayRate = 4f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// PHYSICAL - Capacity to perform
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialEnergy = 100f;
		[Export] private float _initialSatiety = 50f;

		[Export] private float _energyDecayRate = 2f;
		[Export] private float _satietyDecayRate = 3f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// EMOTIONAL - Spirit (-50 to +50)
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialSpirit = 0f;  // -50 to +50

		// ═══════════════════════════════════════════════════════════════════════════════
		// COGNITIVE - Performance quality
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialAlertness = 75f;
		[Export] private float _initialDiscernment = 50f;
		[Export] private float _initialFocus = 50f;
		[Export] private float _initialPatience = 50f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// LONG-TERM - Persistent across nights
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _initialBelief = 50f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// DECAY MULTIPLIERS
		// ═══════════════════════════════════════════════════════════════════════════════
		[Export] private float _lowCaffeineEnergyMultiplier = 2f;
		[Export] private float _lowSatietyEnergyMultiplier = 1.5f;
		[Export] private float _lowSpiritEnergyMultiplier = 1.3f;
		[Export] private float _talkingSatietyMultiplier = 1.5f;
		[Export] private float _highStressNicotineMultiplier = 2f;
		[Export] private float _highNeedsSpiritPenalty = 0.5f;  // per second

		// Runtime stat instances - Dependencies
		private Stat _caffeine;
		private Stat _nicotine;

		// Runtime stat instances - Physical
		private Stat _energy;
		private Stat _satiety;

		// Runtime stat instances - Emotional
		private Stat _spirit;  // -50 to +50 range

		// Runtime stat instances - Cognitive
		private Stat _alertness;
		private Stat _discernment;
		private Stat _focus;
		private Stat _patience;

		// Runtime stat instances - Long-Term
		private Stat _belief;

		// Public accessors - Dependencies
		public Stat Caffeine => _caffeine;
		public Stat Nicotine => _nicotine;

		// Public accessors - Physical
		public Stat Energy => _energy;
		public Stat Satiety => _satiety;

		// Public accessors - Emotional
		public Stat Spirit => _spirit;

		// Public accessors - Cognitive
		public Stat Alertness => _alertness;
		public Stat Discernment => _discernment;
		public Stat Focus => _focus;
		public Stat Patience => _patience;

		// Public accessors - Long-Term
		public Stat Belief => _belief;

		// Public decay rate accessors for monitors
		public float CaffeineDecayRate => _caffeineDecayRate;
		public float NicotineDecayRate => _nicotineDecayRate;
		public float EnergyDecayRate => _energyDecayRate;
		public float SatietyDecayRate => _satietyDecayRate;
		public float PatienceDecayRate => _patienceDecayRate;



		private VernMoodType _currentMoodType = VernMoodType.Neutral;
		private float _lastVibe = 0f;

		private float _patienceDecayRate = 3f;

		// ═══════════════════════════════════════════════════════════════════════════════
		// INITIALIZATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Initialize runtime stats. Call this when starting a new game/night.
		/// </summary>
		public void Initialize()
		{
			// Dependencies
			_caffeine = new Stat("Caffeine", _initialCaffeine);
			_nicotine = new Stat("Nicotine", _initialNicotine);

			// Physical
			_energy = new Stat("Energy", _initialEnergy);
			_satiety = new Stat("Satiety", _initialSatiety);

			// Emotional (Spirit ranges -50 to +50)
			_spirit = new Stat("Spirit", _initialSpirit, -50f, 50f);

			// Cognitive
			_alertness = new Stat("Alertness", _initialAlertness);
			_discernment = new Stat("Discernment", _initialDiscernment);
			_focus = new Stat("Focus", _initialFocus);
			_patience = new Stat("Patience", _initialPatience);

			// Long-Term
			_belief = new Stat("Belief", _initialBelief);

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
			_energy.OnValueChanged += NotifyStatsChanged;
			_satiety.OnValueChanged += NotifyStatsChanged;
			_spirit.OnValueChanged += NotifyStatsChanged;
			_alertness.OnValueChanged += NotifyStatsChanged;
			_discernment.OnValueChanged += NotifyStatsChanged;
			_focus.OnValueChanged += NotifyStatsChanged;
			_patience.OnValueChanged += NotifyStatsChanged;
			_belief.OnValueChanged += NotifyStatsChanged;
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
		/// VIBE determines listener growth and show quality.
		/// </summary>
		public float CalculateVIBE()
		{
			float entertainment = CalculateEntertainment();
			float credibility = CalculateCredibility();
			float engagement = CalculateEngagement();

			float vibe = (entertainment * 0.4f) + (credibility * 0.3f) + (engagement * 0.3f);

			return Mathf.Clamp(vibe, -100f, 100f);
		}

		/// <summary>
		/// Calculate Entertainment component (0-100).
		/// </summary>
		public float CalculateEntertainment()
		{
			float spiritFactor = CalculateSpiritModifier();
			float energyFactor = _energy.Normalized;
			float alertnessFactor = _alertness.Normalized;
			float topicBonus = 0.1f;  // TODO: Add topic affinity when implemented

			float entertainment = (spiritFactor * 40f) + (energyFactor * 30f) + (alertnessFactor * 20f) + (topicBonus * 100f);

			return Mathf.Clamp(entertainment, 0f, 100f);
		}

		/// <summary>
		/// Calculate Credibility component (0-100).
		/// </summary>
		public float CalculateCredibility()
		{
			float discernmentFactor = _discernment.Normalized;
			float beliefFactor = _belief.Normalized;
			float evidenceBonus = 0f;  // TODO: Add evidence bonuses when implemented

			float credibility = (discernmentFactor * 50f) + (beliefFactor * 30f) + (evidenceBonus * 20f);

			return Mathf.Clamp(credibility, 0f, 100f);
		}

		/// <summary>
		/// Calculate Engagement component (0-100).
		/// </summary>
		public float CalculateEngagement()
		{
			float focusFactor = _focus.Normalized;
			float patienceFactor = _patience.Normalized;
			float spiritFactor = CalculateSpiritModifier() * 0.5f + 0.5f;  // Convert to 0-1 range
			float callerBonus = 0f;  // TODO: Add caller quality when implemented

			float engagement = (focusFactor * 40f) + (patienceFactor * 30f) + (spiritFactor * 20f) + (callerBonus * 10f);

			return Mathf.Clamp(engagement, 0f, 100f);
		}

		/// <summary>
		/// Calculate Spirit modifier using sigmoid curve.
		/// Returns multiplier from ~0.58 to ~1.58 based on Spirit (-50 to +50).
		/// </summary>
		public float CalculateSpiritModifier()
		{
			float normalizedSpirit = _spirit.Value / 100f;  // -0.5 to +0.5
			float modifier = 1.0f + (normalizedSpirit * 0.8f) + (normalizedSpirit * normalizedSpirit * 0.4f);
			return modifier;
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// MOOD TYPE CALCULATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Calculate Vern's current mood type based on stats.
		/// Priority order: Tired → Energetized → Irritated → Amused → Gruff → Focused → Neutral
		/// </summary>
		public VernMoodType CalculateMoodType()
		{
			// Tired: Low energy
			if (_energy.Value < 30f)
				return VernMoodType.Tired;

			// Energetized: High caffeine AND high energy
			if (_caffeine.Value > 60f && _energy.Value > 60f)
				return VernMoodType.Energized;

			// Irritated: Low spirit OR low patience
			if (_spirit.Value < -10f || _patience.Value < 40f)
				return VernMoodType.Irritated;

			// Amused: High spirit AND recent positive interaction
			// TODO: Track recent positive interaction
			if (_spirit.Value > 20f)
				return VernMoodType.Amused;

			// Gruff: Recent bad caller OR negative spirit
			// TODO: Track recent bad caller
			if (_spirit.Value < 0f)
				return VernMoodType.Gruff;

			// Focused: High alertness AND high discernment
			if (_alertness.Value > 60f && _discernment.Value > 50f)
				return VernMoodType.Focused;

			// Default: Neutral
			return VernMoodType.Neutral;
		}

		public VernMoodType CurrentMoodType => _currentMoodType;

	}
}
