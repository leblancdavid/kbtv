using System;
using Godot;

namespace KBTV.Data
{
	/// <summary>
	/// Tier levels for Topic Belief progression.
	/// </summary>
	public enum BeliefTier
	{
		Skeptic = 1,      // 0 belief
		Curious = 2,      // 100 belief
		Interested = 3,   // 300 belief
		Believer = 4,     // 600 belief
		TrueBeliever = 5  // 1000 belief
	}

	/// <summary>
	/// Tracks Vern's belief in a specific topic.
	/// Belief is a tiered XP system where:
	/// - Belief can go up and down based on caller quality
	/// - Once a tier is reached, belief cannot drop below that tier's floor
	/// - Higher tiers provide Mental bonuses for that topic
	/// </summary>
	[Serializable]
	public class TopicBelief
	{
		private string _topicId;
		private string _topicName;
		private float _belief;
		private BeliefTier _highestTierReached;

		public string TopicId => _topicId;
		public string TopicName => _topicName;
		public float Belief => _belief;
		public BeliefTier CurrentTier => GetTierForBelief(_belief);
		public BeliefTier HighestTierReached => _highestTierReached;

		public event Action<float, float>? OnBeliefChanged; // oldValue, newValue
		public event Action<BeliefTier, BeliefTier>? OnTierChanged; // oldTier, newTier

		// ═══════════════════════════════════════════════════════════════════════════════
		// TIER THRESHOLDS
		// ═══════════════════════════════════════════════════════════════════════════════

		public static float GetTierThreshold(BeliefTier tier) => tier switch
		{
			BeliefTier.Skeptic => 0f,
			BeliefTier.Curious => 100f,
			BeliefTier.Interested => 300f,
			BeliefTier.Believer => 600f,
			BeliefTier.TrueBeliever => 1000f,
			_ => 0f
		};

		public static float GetTierFloor(BeliefTier tier) => GetTierThreshold(tier);

		public static BeliefTier GetTierForBelief(float belief)
		{
			if (belief >= 1000f) return BeliefTier.TrueBeliever;
			if (belief >= 600f) return BeliefTier.Believer;
			if (belief >= 300f) return BeliefTier.Interested;
			if (belief >= 100f) return BeliefTier.Curious;
			return BeliefTier.Skeptic;
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// TIER BONUSES
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Get Mental bonus percentage for current tier.
		/// </summary>
		public float MentalBonus => GetMentalBonusForTier(CurrentTier);

		public static float GetMentalBonusForTier(BeliefTier tier) => tier switch
		{
			BeliefTier.Skeptic => 0f,
			BeliefTier.Curious => 0.05f,      // +5%
			BeliefTier.Interested => 0.10f,   // +10%
			BeliefTier.Believer => 0.15f,     // +15%
			BeliefTier.TrueBeliever => 0.20f, // +20%
			_ => 0f
		};

		/// <summary>
		/// Returns true if screening hints are available (Tier 3+).
		/// </summary>
		public bool HasScreeningHints => CurrentTier >= BeliefTier.Interested;

		/// <summary>
		/// Returns true if better caller pool is available (Tier 4+).
		/// </summary>
		public bool HasBetterCallerPool => CurrentTier >= BeliefTier.Believer;

		/// <summary>
		/// Returns true if expert guests are available (Tier 5).
		/// </summary>
		public bool HasExpertGuests => CurrentTier >= BeliefTier.TrueBeliever;

		// ═══════════════════════════════════════════════════════════════════════════════
		// CONSTRUCTION
		// ═══════════════════════════════════════════════════════════════════════════════

		public TopicBelief(string topicId, string topicName, float initialBelief = 0f)
		{
			_topicId = topicId;
			_topicName = topicName;
			_belief = Mathf.Max(0f, initialBelief);
			_highestTierReached = GetTierForBelief(_belief);
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// BELIEF MODIFICATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Add or remove belief. Cannot drop below the floor of the highest tier reached.
		/// </summary>
		public void ModifyBelief(float delta)
		{
			float oldBelief = _belief;
			BeliefTier oldTier = CurrentTier;

			_belief += delta;

			// Enforce floor: cannot drop below highest tier reached
			float floor = GetTierFloor(_highestTierReached);
			_belief = Mathf.Max(floor, _belief);

			// Check if we reached a new highest tier
			BeliefTier newTier = CurrentTier;
			if (newTier > _highestTierReached)
			{
				_highestTierReached = newTier;
			}

			// Fire events
			if (!Mathf.IsEqualApprox(oldBelief, _belief))
			{
				OnBeliefChanged?.Invoke(oldBelief, _belief);
			}

			if (oldTier != newTier)
			{
				OnTierChanged?.Invoke(oldTier, newTier);
			}
		}

		/// <summary>
		/// Set belief to a specific value. Respects tier floor.
		/// </summary>
		public void SetBelief(float value)
		{
			float oldBelief = _belief;
			BeliefTier oldTier = CurrentTier;

			// Enforce floor
			float floor = GetTierFloor(_highestTierReached);
			_belief = Mathf.Max(floor, value);

			// Check if we reached a new highest tier
			BeliefTier newTier = CurrentTier;
			if (newTier > _highestTierReached)
			{
				_highestTierReached = newTier;
			}

			// Fire events
			if (!Mathf.IsEqualApprox(oldBelief, _belief))
			{
				OnBeliefChanged?.Invoke(oldBelief, _belief);
			}

			if (oldTier != newTier)
			{
				OnTierChanged?.Invoke(oldTier, newTier);
			}
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// CALLER EFFECTS
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Apply belief change from a good on-topic caller.
		/// </summary>
		public void ApplyGoodCaller(float beliefGain = 15f)
		{
			ModifyBelief(beliefGain);
		}

		/// <summary>
		/// Apply belief change from a bad/hoax on-topic caller.
		/// </summary>
		public void ApplyBadCaller(float beliefLoss = -10f)
		{
			ModifyBelief(beliefLoss);
		}

		/// <summary>
		/// Apply belief bonus for completing a show on this topic.
		/// </summary>
		public void ApplyShowCompleted(float beliefGain = 25f)
		{
			ModifyBelief(beliefGain);
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// PROGRESS
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Get progress toward the next tier (0 to 1).
		/// Returns 1.0 if at max tier.
		/// </summary>
		public float ProgressToNextTier
		{
			get
			{
				if (CurrentTier == BeliefTier.TrueBeliever)
					return 1f;

				float currentFloor = GetTierFloor(CurrentTier);
				float nextFloor = GetTierFloor(CurrentTier + 1);
				float range = nextFloor - currentFloor;

				return (_belief - currentFloor) / range;
			}
		}

		/// <summary>
		/// Get belief required to reach the next tier.
		/// Returns 0 if at max tier.
		/// </summary>
		public float BeliefToNextTier
		{
			get
			{
				if (CurrentTier == BeliefTier.TrueBeliever)
					return 0f;

				float nextFloor = GetTierFloor(CurrentTier + 1);
				return nextFloor - _belief;
			}
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// DISPLAY
		// ═══════════════════════════════════════════════════════════════════════════════

		public static string GetTierName(BeliefTier tier) => tier switch
		{
			BeliefTier.Skeptic => "Skeptic",
			BeliefTier.Curious => "Curious",
			BeliefTier.Interested => "Interested",
			BeliefTier.Believer => "Believer",
			BeliefTier.TrueBeliever => "True Believer",
			_ => "Unknown"
		};

		public string CurrentTierName => GetTierName(CurrentTier);

		public override string ToString()
		{
			return $"{_topicName}: Tier {(int)CurrentTier} ({CurrentTierName}) - {_belief:F0} belief";
		}
	}
}
