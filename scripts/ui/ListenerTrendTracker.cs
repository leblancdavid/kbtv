#nullable enable

using System.Collections.Generic;

namespace KBTV.UI
{
    /// <summary>
    /// Tracks listener counts over a rolling window and reports the current trend direction.
    /// Compares the average of the recent half of the window against the earlier half, using a
    /// deadzone to ignore the jitter from ListenerManager's periodic updates.
    /// Dependency-free so it can be unit tested without a scene tree.
    /// </summary>
    public class ListenerTrendTracker
    {
        public const float WindowSeconds = 6f;
        public const float Deadzone = 10f;

        public const int Up = 1;
        public const int Down = -1;
        public const int Flat = 0;

        private readonly List<(float Time, int Count)> _samples = new();

        /// <summary>
        /// Feed a new listener count sample and get the current trend direction.
        /// Call at a steady cadence (the overlay samples every 0.5s).
        /// </summary>
        public int Update(float now, int count)
        {
            _samples.Add((now, count));
            Trim(now);

            float windowStart = now - WindowSeconds;
            float mid = now - WindowSeconds * 0.5f;

            var recent = WindowAverage(mid, windowStart);
            var older = WindowAverage(now, mid);

            if (recent == null || older == null)
            {
                return Flat;
            }

            float delta = recent.Value - older.Value;
            if (delta > Deadzone) return Up;
            if (delta < -Deadzone) return Down;
            return Flat;
        }

        public void Clear() => _samples.Clear();

        /// <summary>
        /// Drop samples that have fallen outside the window.
        /// </summary>
        private void Trim(float now)
        {
            _samples.RemoveAll(s => now - s.Time > WindowSeconds);
        }

        /// <summary>
        /// Average of samples with (newest, oldest] bounds, or null if no samples fall in range.
        /// </summary>
        private float? WindowAverage(float newest, float oldest)
        {
            float sum = 0f;
            int count = 0;
            foreach (var (t, c) in _samples)
            {
                if (t <= newest && t > oldest)
                {
                    sum += c;
                    count++;
                }
            }

            return count > 0 ? sum / count : null;
        }
    }
}