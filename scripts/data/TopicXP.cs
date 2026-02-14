using System;
using Godot;

namespace KBTV.Data
{
	/// <summary>
	/// Tier levels for Topic XP progression (0-6, with 6+ showing as Expert).
	/// </summary>
	public static class TopicLevel
	{
		public const int MinLevel = 0;
		public const int MaxLevel = 6;
		
		public const int Skeptic = 0;
		public const int Curious = 1;
		public const int Interested = 2;
		public const int Believer = 3;
		public const int TrueBeliever = 4;
		public const int Expert = 5;
		public const int Master = 6;

		public static string GetLevelName(int level)
		{
			if (level >= Master) return "Expert";
			return level switch
			{
				Skeptic => "Skeptic",
				Curious => "Curious",
				Interested => "Interested",
				Believer => "Believer",
				TrueBeliever => "True Believer",
				Expert => "Expert",
				Master => "Master",
				_ => $"Level {level}"
			};
		}
	}

	/// <summary>
	/// Tracks Vern's experience in a specific topic.
	/// XP is a tiered experience system where:
	/// - XP can go up and down based on caller quality
	/// - Once a level is reached, XP cannot drop below that level's floor
	/// - Higher levels provide Mental bonuses for that topic
	/// </summary>
	[Serializable]
	public class TopicXP
	{
		private string _topicId;
		private string _topicName;
		private float _xp;
		private int _highestLevelReached;

		public string TopicId => _topicId;
		public string TopicName => _topicName;
		public float XP => _xp;
		public int CurrentLevel => _highestLevelReached;
		public int HighestLevelReached => _highestLevelReached;

		public event Action<float, float>? OnXPChanged; // oldValue, newValue
		public event Action<int, int>? OnLevelChanged; // oldLevel, newLevel

		// ═══════════════════════════════════════════════════════════════════════════════
		// LEVEL THRESHOLDS
		// ═══════════════════════════════════════════════════════════════════════════════

		public static float GetLevelThreshold(int level) => level switch
		{
			0 => 0f,
			1 => 100f,
			2 => 250f,
			3 => 450f,
			4 => 700f,
			5 => 1000f,
			6 => 1400f,
			_ => 1400f + (level - 6) * 200f // Allow unlimited leveling
		};

		public static float GetLevelFloor(int level) => GetLevelThreshold(level);

		public static int GetLevelForXP(float xp)
		{
			for (int level = TopicLevel.MaxLevel; level >= TopicLevel.MinLevel; level--)
			{
				if (xp >= GetLevelThreshold(level))
					return level;
			}
			return TopicLevel.MinLevel;
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// LEVEL BONUSES
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Get Mental bonus percentage for current level.
		/// </summary>
		public float MentalBonus => GetMentalBonusForLevel(CurrentLevel);

		public static float GetMentalBonusForLevel(int level) => level switch
		{
			0 => 0f,
			1 => 0.05f,      // +5%
			2 => 0.10f,      // +10%
			3 => 0.15f,      // +15%
			4 => 0.20f,      // +20%
			5 => 0.25f,      // +25%
			6 => 0.30f,      // +30%
			_ => 0.30f + (level - 6) * 0.02f // Small bonus increase for each level beyond
		};

		/// <summary>
		/// Returns true if screening hints are available (Level 2+).
		/// </summary>
		public bool HasScreeningHints => CurrentLevel >= TopicLevel.Interested;

		/// <summary>
		/// Returns true if better caller pool is available (Level 3+).
		/// </summary>
		public bool HasBetterCallerPool => CurrentLevel >= TopicLevel.Believer;

		/// <summary>
		/// Returns true if expert guests are available (Level 4+).
		/// </summary>
		public bool HasExpertGuests => CurrentLevel >= TopicLevel.TrueBeliever;

		// ═══════════════════════════════════════════════════════════════════════════════
		// CONSTRUCTION
		// ═══════════════════════════════════════════════════════════════════════════════

		public TopicXP(string topicId, string topicName, float initialXP = 0f)
		{
			_topicId = topicId;
			_topicName = topicName;
			_xp = Mathf.Max(0f, initialXP);
			_highestLevelReached = GetLevelForXP(_xp); // Initialize based on starting XP
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// XP MODIFICATION
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Add or remove XP. Cannot drop below the floor of the highest level reached.
		/// Does not automatically advance levels - leveling is manual.
		/// </summary>
		public void ModifyXP(float delta)
		{
			float oldXP = _xp;
			int oldLevel = CurrentLevel;

			_xp += delta;

			// Enforce floor: cannot drop below highest level reached
			float floor = GetLevelFloor(_highestLevelReached);
			_xp = Mathf.Max(floor, _xp);

			// Fire events (but no automatic level advancement)
			if (!Mathf.IsEqualApprox(oldXP, _xp))
			{
				OnXPChanged?.Invoke(oldXP, _xp);
			}

			// No OnLevelChanged here - levels only change manually
		}

		/// <summary>
		/// Set XP to a specific value. Respects level floor.
		/// Does not automatically advance levels - leveling is manual.
		/// </summary>
		public void SetXP(float value)
		{
			float oldXP = _xp;
			int oldLevel = CurrentLevel;

			// Enforce floor
			float floor = GetLevelFloor(_highestLevelReached);
			_xp = Mathf.Max(floor, value);

			// Fire events (but no automatic level advancement)
			if (!Mathf.IsEqualApprox(oldXP, _xp))
			{
				OnXPChanged?.Invoke(oldXP, _xp);
			}

			// No OnLevelChanged here - levels only change manually
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// MANUAL LEVELING
		// ═══════════════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Manually level up to the next level, preserving overflow XP.
		/// Example: 150 XP at Level 1 (threshold 100) → Level up → 50 XP at Level 2
		/// </summary>
		public void LevelUp()
		{
			int newLevel = CurrentLevel + 1;

			int oldLevel = CurrentLevel;
			
			// Preserve overflow XP: subtract current level threshold, keep remainder
			float oldThreshold = GetLevelThreshold(CurrentLevel);
			_xp = Mathf.Max(0, _xp - oldThreshold);
			
			// Advance to new level
			_highestLevelReached = newLevel;

			// Fire level changed event
			OnLevelChanged?.Invoke(oldLevel, newLevel);
			
			// Also fire XP changed event since XP value changed
			OnXPChanged?.Invoke(oldThreshold, _xp);
		}

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
		/// Get progress toward the next level (0 to 1).
		/// Returns 1.0 if at max level.
		/// </summary>
		public float ProgressToNextLevel
		{
			get
			{
				int nextLevel = CurrentLevel + 1;
				float currentFloor = GetLevelFloor(CurrentLevel);
				float nextFloor = GetLevelFloor(nextLevel);
				float range = nextFloor - currentFloor;

				if (range <= 0) return 1f; // At or beyond max level

				return (_xp - currentFloor) / range;
			}
		}

		/// <summary>
		/// Get XP required to reach the next level.
		/// Returns 0 if at or beyond max level.
		/// </summary>
		public float XPToNextLevel
		{
			get
			{
				int nextLevel = CurrentLevel + 1;
				float nextFloor = GetLevelFloor(nextLevel);
				if (_xp >= nextFloor) return 0f;

				return nextFloor - _xp;
			}
		}

		// ═══════════════════════════════════════════════════════════════════════════════
		// DISPLAY
		// ═══════════════════════════════════════════════════════════════════════════════

		public string CurrentLevelName => TopicLevel.GetLevelName(CurrentLevel);

		public override string ToString()
		{
			return $"{_topicName}: Level {CurrentLevel} ({CurrentLevelName}) - {_xp:F0} XP";
		}
	}
}
