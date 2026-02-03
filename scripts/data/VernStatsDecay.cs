using System;
using Godot;
using KBTV.Data;

namespace KBTV.Data
{
	/// <summary>
	/// Vern Stat's decay and modification methods.
	/// Split from main file to reduce size.
	/// </summary>
	public partial class VernStats : Resource
	{
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
				case StatType.Belief:
					_belief.Modify(amount);
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
			// Directly modify belief stat
			_belief.Modify(amount);
		}
	}
}