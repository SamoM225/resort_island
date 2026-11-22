using System;
using System.Collections.Generic;

namespace ResortIsland.Waves
{
    /// <summary>
    /// The score of one wave. During the wave every unit's wait (arrival until seated, service not counted)
    /// is recorded silently; the player never sees it. At the end it gives the satisfaction, rating and coins.
    /// </summary>
    public sealed class WaveScore : IWaveResult
    {
        private readonly WaveRating rating;
        private readonly Dictionary<string, List<double>> waits = new Dictionary<string, List<double>>();

        public int Wave { get; }

        public WaveScore(WaveRating rating, int wave)
        {
            this.rating = rating ?? throw new ArgumentNullException(nameof(rating));
            Wave = wave;
        }

        public IReadOnlyDictionary<string, List<double>> Waits => waits;

        public int Units { get; private set; }

        /// <summary>Satisfaction 0-1.</summary>
        public double SatisfactionRatio => rating.Satisfaction(waits);

        /// <summary>Satisfaction in percent, as shown on Wave Complete and logged to analytics.</summary>
        public float Satisfaction => (float)(SatisfactionRatio * 100.0);

        /// <summary>1-5 stars, 6 = Perfect.</summary>
        public int Rating => rating.RatingFor(SatisfactionRatio);

        public bool IsPerfect => Rating == WaveRating.Perfect;

        public int Coins => rating.CoinsFor(Units, Rating);

        public void RecordWait(string needId, double waitSeconds)
        {
            if (!rating.HasNeed(needId))
                throw new ArgumentException($"Need \"{needId}\" is not in the needs config.", nameof(needId));
            if (waitSeconds < 0 || double.IsNaN(waitSeconds))
                throw new ArgumentOutOfRangeException(nameof(waitSeconds), "A wait cannot be negative.");

            if (!waits.TryGetValue(needId, out List<double> needWaits))
            {
                needWaits = new List<double>();
                waits.Add(needId, needWaits);
            }
            needWaits.Add(waitSeconds);
            Units++;
        }
    }
}
