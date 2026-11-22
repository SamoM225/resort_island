using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    /// <summary>
    /// Rates the cases exported by tools/wave_sim/wave_sim.py (every wave plus edge cases) and expects
    /// the same satisfaction, rating and coins as the simulation. The backend runs the same cases.
    /// </summary>
    public class WaveRatingTests
    {
        private static readonly string CasesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "wave_sim", "fixtures", "wave_rating_cases.json"));

        private static readonly JsonSerializer SnakeCase = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        });

        private static JObject LoadCases() => JObject.Parse(File.ReadAllText(CasesPath));

        private static WaveRating RatingFrom(JObject cases)
        {
            JToken rules = cases["rules"];
            return new WaveRating(
                rules["wave_system"].ToObject<WaveSystemConfig>(SnakeCase),
                rules["economy"].ToObject<EconomyConfig>(SnakeCase),
                rules["needs"].ToObject<List<NeedDefinition>>(SnakeCase));
        }

        private static WaveScore Score(WaveRating rating, JToken testCase)
        {
            WaveScore score = new WaveScore(rating, testCase.Value<int?>("wave") ?? 0);
            foreach (JProperty need in testCase["waits"].Children<JProperty>())
            {
                foreach (double wait in need.Value.Values<double>())
                    score.RecordWait(need.Name, wait);
            }
            return score;
        }

        [Test]
        public void EveryCaseFromWaveSim_GetsTheSameSatisfactionRatingAndCoins()
        {
            JObject cases = LoadCases();
            WaveRating rating = RatingFrom(cases);

            Assert.That(cases["cases"].Count(), Is.GreaterThan(10));
            foreach (JToken testCase in cases["cases"])
            {
                WaveScore score = Score(rating, testCase);
                string name = testCase.Value<string>("name");

                Assert.AreEqual(testCase.Value<int>("units"), score.Units, name);
                Assert.AreEqual(testCase.Value<double>("satisfaction"), score.SatisfactionRatio, 1e-9, name);
                Assert.AreEqual(testCase.Value<int>("rating"), score.Rating, name);
                Assert.AreEqual(testCase.Value<int>("coins"), score.Coins, name);
            }
        }

        [Test]
        public void Contribution_IsFullWithinGraceThenFallsToUnitFloor()
        {
            WaveRating rating = RatingFrom(LoadCases());

            Assert.AreEqual(1.0, rating.Contribution(0));
            Assert.AreEqual(1.0, rating.Contribution(10));
            Assert.AreEqual(0.625, rating.Contribution(20), 1e-12);
            Assert.AreEqual(0.25, rating.Contribution(30));
            Assert.AreEqual(0.25, rating.Contribution(300));
        }

        [TestCase(0.40, 1)]
        [TestCase(0.5499, 1)]
        [TestCase(0.55, 2)]
        [TestCase(0.80, 4)]
        [TestCase(0.9999, 5)]
        [TestCase(1.0, WaveRating.Perfect)]
        public void Rating_IsTheHighestReachedThreshold(double satisfaction, int expected)
        {
            Assert.AreEqual(expected, RatingFrom(LoadCases()).RatingFor(satisfaction));
        }

        [Test]
        public void Score_IsAWaveResultInPercent()
        {
            WaveScore score = new WaveScore(RatingFrom(LoadCases()), 3);
            score.RecordWait("rooms", 0);
            score.RecordWait("rooms", 20);

            IWaveResult result = score;

            Assert.AreEqual(81.25f, result.Satisfaction, 1e-4);
            Assert.AreEqual(4, result.Rating);
        }

        [Test]
        public void UnknownNeed_IsRejected()
        {
            WaveScore score = new WaveScore(RatingFrom(LoadCases()), 1);

            Assert.Throws<System.ArgumentException>(() => score.RecordWait("spa", 3));
        }
    }
}
