using System;
using System.Collections.Generic;
using ResortIsland.Waves;

namespace ResortIsland.Analytics
{
    public class WaveRatingMetric : IWaveMetric
    {
        public const string RatingKey = "rating";
        public const string SatisfactionKey = "satisfaction";

        private readonly IWaveResult result;

        public WaveRatingMetric(IWaveResult result)
        {
            this.result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public void AddTo(Dictionary<string, object> payload)
        {
            payload[RatingKey] = result.Rating;
            payload[SatisfactionKey] = Math.Round(result.Satisfaction, 1);
        }
    }
}
