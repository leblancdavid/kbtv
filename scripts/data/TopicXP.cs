using System;
using Godot;

namespace KBTV.Data
{
	/// <summary>
	/// Tier levels for Topic XP progression.
	/// </summary>
	public enum XPTier
	{
		Skeptic = 1,      // 0 xp
		Curious = 2,      // 100 xp
		Interested = 3,   // 300 xp
		Believer = 4,     // 600 xp
		TrueBeliever = 5  // 1000 xp
	}

	/// <summary>
	/// Tracks Vern's experience in a specific topic.
	/// XP is a tiered experience system where:
	/// - XP can go up and down based on caller quality
	/// - Once a tier is reached, XP cannot drop below that tier's floor
	/// - Higher tiers provide Mental bonuses for that topic
	/// </summary>
	[Serializable]
	public class TopicXP
	{
		private string _topicId;
		private string _topicName;
		private float _xp;
		private XPTier _highestTierReached;

		public string TopicId => _topicId;
		public string TopicName => _topicName;
		public float XP => _xp;
		public XPTier CurrentTier => GetTierForXP(_xp);
		public XPTier HighestTierReached => _highestTierReached;

		public event Action<float, float>? OnXPChanged; // oldValue, newValue
		public event Action<XPTier, XPTier>? OnTierChanged; // oldTier, newTier

		// ═══════════════════════════════════════════════════════════════════════════════
		// TIER THRESHOLDS
		// ═══════════════════════════════════════════════════════════════════════════════

		public static float GetTierThreshold(XPTier tier) => tier switch
		{
			XPTier.Skeptic => 0f,
			XPTier.Curious => 100f,
			XPTier.Interested => 300f,
			XPTier.Believer => 600f,
			XPTier.TrueBeliever => 1000f,
			_ => 0f
		};

		public static float GetTierFloor(XPTier tier) => GetTierThreshold(tier);

		public static XPTier GetTierForXP(float xp)
		{
			if (xp >= 1000f) return XPTier.TrueBeliever;
			if (xp >= 600f) return XPTier.Believer;
			if (xp >= 300f) return XPTier.Interested;
			if (xp >= 100f) return XPTier.Curious;
			return XPTier.Skeptic;
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// TIER BONUSES
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Get Mental bonus percentage for current tier.
		/// </summary>
		public float MentalBonus => GetMentalBonusForTier(CurrentTier);

		public static float GetMentalBonusForTier(XPTier tier) => tier switch
		{
			XPTier.Skeptic => 0f,
			XPTier.Curious => 0.05f,      // +5%
			XPTier.Interested => 0.10f,   // +10%
			XPTier.Believer => 0.15f,     // +15%
			XPTier.TrueBeliever => 0.20f, // +20%
			_ => 0f
		};

		/// <summary>
		/// Returns true if screening hints are available (Tier 3+).
		/// </summary>
		public bool HasScreeningHints => CurrentTier >= XPTier.Interested;

		/// <summary>
		/// Returns true if better caller pool is available (Tier 4+).
		/// </summary>
		public bool HasBetterCallerPool => CurrentTier >= XPTier.Believer;

		/// <summary>
		/// Returns true if expert guests are available (Tier 5).
		/// </summary>
		public bool HasExpertGuests => CurrentTier >= XPTier.TrueBeliever;

		// ═══════════════════════════════════════════════════════════════════════════════
		// CONSTRUCTION
		// ═══════════════════════════════════════════════════════════════════════════════

		public TopicXP(string topicId, string topicName, float initialXP = 0f)
		{
			_topicId = topicId;
			_topicName = topicName;
			_xp = Mathf.Max(0f, initialXP);
			_highestTierReached = GetTierForXP(_xp);
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// XP MODIFICATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Add or remove XP. Cannot drop below the floor of the highest tier reached.
		/// </summary>
		public void ModifyXP(float delta)
		{
			float oldXP = _xp;
			XPTier oldTier = CurrentTier;

			_xp += delta;

			// Enforce floor: cannot drop below highest tier reached
			float floor = GetTierFloor(_highestTierReached);
			_xp = Mathf.Max(floor, _xp);

			// Check if we reached a new highest tier
			XPTier newTier = CurrentTier;
			if (newTier > _highestTierReached)
			{
				_highestTierReached = newTier;
			}

			// Fire events
			if (!Mathf.IsEqualApprox(oldXP, _xp))
			{
				OnXPChanged?.Invoke(oldXP, _xp);
			}

			if (oldTier != newTier)
			{
				OnTierChanged?.Invoke(oldTier, newTier);
			}
		}

		/// <summary>
		/// Set XP to a specific value. Respects tier floor.
		/// </summary>
		public void SetXP(float value)
		{
			float oldXP = _xp;
			XPTier oldTier = CurrentTier;

			// Enforce floor
			float floor = GetTierFloor(_highestTierReached);
			_xp = Mathf.Max(floor, value);

			// Check if we reached a new highest tier
			XPTier newTier = CurrentTier;
			if (newTier > _highestTierReached)
			{
				_highestTierReached = newTier;
			}

			// Fire events
			if (!Mathf.IsEqualApprox(oldXP, _xp))
			{
				OnXPChanged?.Invoke(oldXP, _xp);
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
		/// Apply XP change from a good on-topic caller.
		/// </summary>
		public void ApplyGoodCaller(float xpGain = 15f)
		{
			ModifyXP(xpGain);
		}

		/// <summary>
		/// Apply XP change from a bad/hoax on-topic caller.
		/// </summary>
		public void ApplyBadCaller(float xpLoss = -10f)
		{
			ModifyXP(xpLoss);
		}

		/// <summary>
		/// Apply XP bonus for completing a show on this topic.
		/// </summary>
		public void ApplyShowCompleted(float xpGain = 25f)
		{
			ModifyXP(xpGain);
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
				if (CurrentTier == XPTier.TrueBeliever)
					return 1f;

				float currentFloor = GetTierFloor(CurrentTier);
				float nextFloor = GetTierFloor(CurrentTier + 1);
				float range = nextFloor - currentFloor;

				return (_xp - currentFloor) / range;
			}
		}

		/// <summary>
		/// Get XP required to reach the next tier.
		/// Returns 0 if at max tier.
		/// </summary>
		public float XPToNextTier
		{
			get
			{
				if (CurrentTier == XPTier.TrueBeliever)
					return 0f;

				float nextFloor = GetTierFloor(CurrentTier + 1);
				return nextFloor - _xp;
			}
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// DISPLAY
		// ═══════════════════════════════════════════════════════════════════════════════

		public static string GetTierName(XPTier tier) => tier switch
		{
			XPTier.Skeptic => "Skeptic",
			XPTier.Curious => "Curious",
			XPTier.Interested => "Interested",
			XPTier.Believer => "Believer",
			XPTier.TrueBeliever => "True Believer",
			_ => "Unknown"
		};

		public string CurrentTierName => GetTierName(CurrentTier);

		public override string ToString()
		{
			return $"{_topicName}: Tier {(int)CurrentTier} ({CurrentTierName}) - {_xp:F0} XP";
		}
	}
}
