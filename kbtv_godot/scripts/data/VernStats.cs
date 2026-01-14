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
		[Export] private float _initialSkepticism = 50f;

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
		private Stat _skepticism;

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
		public Stat Skepticism => _skepticism;

		/// <summary>
		/// Event fired when any stat changes. Useful for UI updates.
		/// </summary>
		public event Action OnStatsChanged;

		/// <summary>
		/// Event fired when VIBE changes significantly.
		/// </summary>
		public event Action<float> OnVibeChanged;

		/// <summary>
		/// Event fired when mood type changes.
		/// </summary>
		public event Action<VernMoodType> OnMoodTypeChanged;

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
			_skepticism = new Stat("Skepticism", _initialSkepticism);

			// Subscribe to individual stat changes
			SubscribeToStatChanges();

			_currentMoodType = VernMoodType.Neutral;
			_lastVibe = CalculateVIBE();

			OnStatsChanged?.Invoke();
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
			_skepticism.OnValueChanged += NotifyStatsChanged;
		}

		private void NotifyStatsChanged(float oldVal, float newVal)
		{
			OnStatsChanged?.Invoke();

			// Check for VIBE change
			float newVibe = CalculateVIBE();
			if (Mathf.Abs(newVibe - _lastVibe) > 0.01f)
			{
				_lastVibe = newVibe;
				OnVibeChanged?.Invoke(_lastVibe);
			}

			// Check for mood type change
			VernMoodType newMoodType = CalculateMoodType();
			if (newMoodType != _currentMoodType)
			{
				_currentMoodType = newMoodType;
				OnMoodTypeChanged?.Invoke(_currentMoodType);
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
			float skepticismFactor = _skepticism.Normalized;
			float evidenceBonus = 0f;  // TODO: Add evidence bonuses when implemented

			float credibility = (discernmentFactor * 50f) + (skepticismFactor * 30f) + (evidenceBonus * 20f);

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

		// ═══════════════════════════════════════════════════════════════════════════════
		// DECAY AND UPDATE
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Apply time-based decay to stats during live show.
		/// </summary>
		/// <param name="deltaTime">Time elapsed in seconds</param>
		/// <param name="decayMultiplier">Global multiplier for faster/slower decay</param>
		/// <param name="stressLevel">0-1, affects decay rates</param>
		public void ApplyDecay(float deltaTime, float decayMultiplier = 1f, float stressLevel = 0f)
		{
			// Calculate decay multipliers
			float energyMultiplier = CalculateEnergyDecayMultiplier();
			float satietyMultiplier = _talkingSatietyMultiplier;

			// Caffeine decay (affected by stress)
			float stressNicotineMultiplier = 1f + (stressLevel * (_highStressNicotineMultiplier - 1f));
			_caffeine.Modify(-_caffeineDecayRate * deltaTime * decayMultiplier * stressNicotineMultiplier);

			// Nicotine decay (affected by stress)
			_nicotine.Modify(-_nicotineDecayRate * deltaTime * decayMultiplier * stressNicotineMultiplier);

			// Energy decay (affected by caffeine, satiety, spirit)
			_energy.Modify(-_energyDecayRate * deltaTime * decayMultiplier * energyMultiplier);

			// Satiety decay
			_satiety.Modify(-_satietyDecayRate * deltaTime * decayMultiplier * satietyMultiplier);

			// Spirit decay based on needs
			if (_satiety.Value < 30f || _energy.Value < 30f)
			{
				_spirit.Modify(-_highNeedsSpiritPenalty * deltaTime * decayMultiplier);
			}

			// Natural drift toward neutral spirit
			if (_spirit.Value > 0f)
				_spirit.Modify(-0.5f * deltaTime * decayMultiplier);
			else if (_spirit.Value < 0f)
				_spirit.Modify(0.5f * deltaTime * decayMultiplier);

			// Patience decay
			_patience.Modify(-_patienceDecayRate * deltaTime * decayMultiplier);

			// Update derived cognitive stats
			UpdateCognitiveStats();
		}

		private float CalculateEnergyDecayMultiplier()
		{
			float multiplier = 1f;

			// Low caffeine accelerates energy decay
			if (_caffeine.Value < 30f)
				multiplier *= _lowCaffeineEnergyMultiplier;

			// Low satiety accelerates energy decay
			if (_satiety.Value < 40f)
				multiplier *= _lowSatietyEnergyMultiplier;

			// Low spirit accelerates energy decay
			if (_spirit.Value < -20f)
				multiplier *= _lowSpiritEnergyMultiplier;

			return multiplier;
		}

		private void UpdateCognitiveStats()
		{
			// Alertness is derived from energy and caffeine
			float alertnessFromEnergy = _energy.Normalized * 0.7f;
			float alertnessFromCaffeine = _caffeine.Normalized * 0.3f;
			float targetAlertness = (alertnessFromEnergy + alertnessFromCaffeine) * 100f;
			_alertness.SetValue(targetAlertness);

			// Discernment is derived from alertness and spirit
			float discernmentFromAlertness = _alertness.Normalized * 0.6f;
			float discernmentFromSpirit = (_spirit.Normalized + 1f) * 0.4f;  // Convert -50:+50 to 0:1
			float targetDiscernment = (discernmentFromAlertness + discernmentFromSpirit) * 100f;
			_discernment.SetValue(targetDiscernment);

			// Focus is derived from energy and patience
			float focusFromEnergy = _energy.Normalized * 0.5f;
			float focusFromPatience = _patience.Normalized * 0.5f;
			float targetFocus = (focusFromEnergy + focusFromPatience) * 100f;
			_focus.SetValue(targetFocus);
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// STAT MODIFICATION HELPERS
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Apply a stat modification from an item or event.
		/// </summary>
		public void ApplyModification(StatType type, float amount)
		{
			switch (type)
			{
				case StatType.Caffeine:
					_caffeine.Modify(amount);
					break;
				case StatType.Nicotine:
					_nicotine.Modify(amount);
					break;
				case StatType.Energy:
					_energy.Modify(amount);
					break;
				case StatType.Satiety:
					_satiety.Modify(amount);
					break;
				case StatType.Spirit:
					_spirit.Modify(amount);
					break;
				case StatType.Alertness:
					_alertness.Modify(amount);
					break;
				case StatType.Discernment:
					_discernment.Modify(amount);
					break;
				case StatType.Focus:
					_focus.Modify(amount);
					break;
				case StatType.Patience:
					_patience.Modify(amount);
					break;
				case StatType.Skepticism:
					_skepticism.Modify(amount);
					break;
			}
		}

		/// <summary>
		/// Apply a good caller effect (boosts spirit and energy slightly).
		/// </summary>
		public void ApplyGoodCallerEffect()
		{
			_spirit.Modify(8f);
			_energy.Modify(5f);
			_patience.Modify(5f);
		}

		/// <summary>
		/// Apply a great caller effect (larger boost).
		/// </summary>
		public void ApplyGreatCallerEffect()
		{
			_spirit.Modify(15f);
			_energy.Modify(10f);
			_patience.Modify(10f);
		}

		/// <summary>
		/// Apply a bad caller effect (reduces spirit and patience).
		/// </summary>
		public void ApplyBadCallerEffect()
		{
			_spirit.Modify(-8f);
			_patience.Modify(-10f);
		}

		/// <summary>
		/// Apply belief change (from caller outcome).
		/// </summary>
		public void ApplyBeliefChange(float amount)
		{
			// Belief change affects skepticism over time
			_skepticism.Modify(amount);
		}
	}
}
