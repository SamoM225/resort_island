using System;
using System.Collections.Generic;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    /// <summary>
    /// How a wave is rated (build document 4.4 and 4.8), the same model as tools/wave_sim/wave_sim.py and the backend:
    /// every unit of a need contributes by how long it waited until it was seated, weighted by its need;
    /// the weighted average (at least wave_floor) is the satisfaction, the highest reached threshold the rating,
    /// and the coins are units x coins_per_unit x the rating's multiplier.
    /// </summary>
    public sealed class WaveRating
    {
        /// <summary>Ratings are 1-5 stars; 6 is Perfect.</summary>
        public const int Perfect = 6;

        private const double Epsilon = 1e-9;

        private readonly double patienceGrace;
        private readonly double patienceFloorAfter;
        private readonly double unitFloor;
        private readonly double waveFloor;
        private readonly double[] ratingThresholds;
        private readonly int coinsPerUnit;
        private readonly double[] coinsRatingMultipliers;
        private readonly Dictionary<string, double> weights = new Dictionary<string, double>();

        public WaveRating(WaveSystemConfig waveSystem, EconomyConfig economy, IEnumerable<NeedDefinition> needs)
        {
            if (waveSystem == null) throw new ArgumentNullException(nameof(waveSystem));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (needs == null) throw new ArgumentNullException(nameof(needs));

            patienceGrace = Exact(waveSystem.PatienceGraceS);
            patienceFloorAfter = Exact(waveSystem.PatienceFloorAfterS);
            unitFloor = Exact(waveSystem.UnitFloor);
            waveFloor = Exact(waveSystem.WaveFloor);
            ratingThresholds = Array.ConvertAll(waveSystem.RatingThresholds, Exact);
            coinsPerUnit = economy.CoinsPerUnit;
            coinsRatingMultipliers = Array.ConvertAll(economy.CoinsRatingMult, Exact);
            foreach (NeedDefinition need in needs)
                weights[need.Id] = Exact(need.SatisfactionWeight);

            if (ratingThresholds.Length != Perfect || coinsRatingMultipliers.Length != Perfect)
                throw new ArgumentException($"Configs need {Perfect} rating thresholds and coin multipliers (1-5 stars and Perfect).");
        }

        public static WaveRating From(GameConfigs configs) => new WaveRating(configs.WaveSystem, configs.Economy, configs.Needs.Needs);

        public bool HasNeed(string needId) => weights.ContainsKey(needId);

        /// <summary>100 % within patience_grace, then linearly down to unit_floor at patience_floor_after.</summary>
        public double Contribution(double waitSeconds)
        {
            if (waitSeconds <= patienceGrace)
                return 1.0;
            if (waitSeconds >= patienceFloorAfter)
                return unitFloor;

            double progress = (waitSeconds - patienceGrace) / (patienceFloorAfter - patienceGrace);
            return 1.0 - progress * (1.0 - unitFloor);
        }

        /// <summary>Satisfaction 0-1 from the wait of every unit, by need. A wave without units is 1.</summary>
        public double Satisfaction(IEnumerable<KeyValuePair<string, List<double>>> waitsByNeed)
        {
            double weighted = 0.0;
            double totalWeight = 0.0;
            foreach (KeyValuePair<string, List<double>> need in waitsByNeed)
            {
                double weight = WeightOf(need.Key);
                foreach (double wait in need.Value)
                {
                    weighted += Contribution(wait) * weight;
                    totalWeight += weight;
                }
            }

            return totalWeight == 0.0 ? 1.0 : Math.Max(waveFloor, weighted / totalWeight);
        }

        /// <summary>1-5 stars or Perfect (6): the highest threshold the satisfaction reached.</summary>
        public int RatingFor(double satisfaction)
        {
            int rating = 1;
            for (int index = 0; index < ratingThresholds.Length; index++)
            {
                if (satisfaction + Epsilon >= ratingThresholds[index])
                    rating = index + 1;
            }
            return rating;
        }

        public int CoinsFor(int units, int rating)
        {
            if (rating < 1 || rating > Perfect)
                throw new ArgumentOutOfRangeException(nameof(rating));

            return (int)Math.Floor(units * coinsPerUnit * coinsRatingMultipliers[rating - 1] + 0.5);
        }

        private double WeightOf(string needId)
        {
            if (!weights.TryGetValue(needId, out double weight))
                throw new ArgumentException($"Need \"{needId}\" is not in the needs config.", nameof(needId));
            return weight;
        }

        // Configs hold floats; 0.4f is 0.4000000059. Through decimal it is 0.4 again, like in the configs and in wave_sim.
        private static double Exact(float value) => (double)(decimal)value;
    }
}
