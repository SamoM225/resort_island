using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Analytics;

namespace ResortIsland.Tests
{
    public class WaveRatingMetricTests
    {
        [Test]
        public void AddTo_WritesRatingAndSatisfaction()
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();

            new WaveRatingMetric(new FakeWaveResult { Rating = 5, Satisfaction = 92.37f }).AddTo(payload);

            Assert.AreEqual(5, payload[WaveRatingMetric.RatingKey]);
            Assert.AreEqual(92.4, (double)payload[WaveRatingMetric.SatisfactionKey], 0.0001);
        }

        [Test]
        public void AddTo_ReadsTheResultOfTheWaveJustFinished()
        {
            FakeWaveResult result = new FakeWaveResult { Rating = 2, Satisfaction = 55f };
            WaveRatingMetric metric = new WaveRatingMetric(result);
            result.Rating = 1;
            result.Satisfaction = 40f;
            Dictionary<string, object> payload = new Dictionary<string, object>();

            metric.AddTo(payload);

            Assert.AreEqual(1, payload[WaveRatingMetric.RatingKey]);
            Assert.AreEqual(40.0, (double)payload[WaveRatingMetric.SatisfactionKey], 0.0001);
        }
    }
}
