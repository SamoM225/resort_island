using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveArrivalsTests
    {
        private GameConfigs configs;
        private ScriptedWavePlanner planner;
        private List<string> arrived;

        [SetUp]
        public void SetUp()
        {
            configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            planner = new ScriptedWavePlanner(configs.Waves, StartingNeeds.From(configs.Buildings));
            arrived = new List<string>();
        }

        private WaveArrivals ArrivalsOf(WavePlan plan)
        {
            WaveArrivals arrivals = new WaveArrivals(plan, configs.WaveSystem.WaveTimerS);
            arrivals.UnitArrived += arrived.Add;
            return arrivals;
        }

        private static WavePlan OneNeed(string needId, int units) => new WavePlan(1, units, new[] { new NeedDemand(needId, units) });

        [Test]
        public void Timer_ComesFromTheWaveSystemConfig()
        {
            Assert.AreEqual(90.0, ArrivalsOf(planner.Plan(1)).Duration);
        }

        [Test]
        public void EveryUnitOfTheWaveTable_ArrivesWithinTheTimer()
        {
            WaveArrivals arrivals = ArrivalsOf(planner.Plan(1));

            arrivals.Tick(90.0);

            Assert.AreEqual(22, arrivals.Total);
            Assert.IsTrue(arrivals.AllArrived);
            Assert.AreEqual(8, arrived.Count(need => need == "rooms"));
            Assert.AreEqual(8, arrived.Count(need => need == "drinks"));
            Assert.AreEqual(6, arrived.Count(need => need == "food"));
        }

        [Test]
        public void NobodyArrives_BeforeTheFirstSlot()
        {
            WaveArrivals arrivals = ArrivalsOf(OneNeed("drinks", 8));

            arrivals.Tick(11.2);

            CollectionAssert.IsEmpty(arrived);
            Assert.AreEqual(0.05, arrivals.TimeUntilNext, 1e-9);
        }

        [Test]
        public void Units_ArriveEvenlyOverTheTimer()
        {
            WaveArrivals arrivals = ArrivalsOf(OneNeed("drinks", 8));

            for (int slot = 1; slot <= 8; slot++)
            {
                arrivals.Tick(11.25);
                Assert.AreEqual(slot, arrivals.Arrived);
            }

            Assert.IsTrue(arrivals.AllArrived);
        }

        [Test]
        public void EachNeed_IsSpreadOverTheWholeTimer()
        {
            WaveArrivals arrivals = ArrivalsOf(OneNeed("food", 6));

            arrivals.Tick(14.99);
            Assert.AreEqual(0, arrivals.Arrived);
            arrivals.Tick(0.01);
            Assert.AreEqual(1, arrivals.Arrived);
            arrivals.Tick(74.99);
            Assert.AreEqual(5, arrivals.Arrived);
            arrivals.Tick(0.01);
            Assert.AreEqual(6, arrivals.Arrived);
        }

        [Test]
        public void Needs_ArriveInTimeOrder()
        {
            WaveArrivals arrivals = ArrivalsOf(planner.Plan(1));

            arrivals.Tick(15.0);

            CollectionAssert.AreEqual(new[] { "rooms", "drinks", "food" }, arrived);
            Assert.AreEqual(7.5, arrivals.TimeUntilNext, 1e-9);
        }

        [Test]
        public void LastUnits_ArriveWhenTheTimerEnds()
        {
            WaveArrivals arrivals = ArrivalsOf(planner.Plan(1));

            arrivals.Tick(89.99);
            Assert.AreEqual(19, arrivals.Arrived);

            arrivals.Tick(0.01);
            Assert.IsTrue(arrivals.AllArrived);
            Assert.AreEqual(double.PositiveInfinity, arrivals.TimeUntilNext);
        }

        [Test]
        public void Arrivals_DoNotDependOnFrameLength()
        {
            ArrivalsOf(planner.Plan(5)).Tick(90.0);
            List<string> oneFrame = new List<string>(arrived);

            arrived.Clear();
            WaveArrivals manyFrames = ArrivalsOf(planner.Plan(5));
            for (int frame = 0; frame < 5400; frame++)
                manyFrames.Tick(1.0 / 60.0);

            Assert.IsTrue(manyFrames.AllArrived);
            CollectionAssert.AreEqual(oneFrame, arrived);
        }

        [Test]
        public void NeedWithoutUnits_BringsNobody()
        {
            WaveArrivals arrivals = ArrivalsOf(OneNeed("relax", 0));

            arrivals.Tick(90.0);

            Assert.AreEqual(0, arrivals.Total);
            Assert.IsTrue(arrivals.AllArrived);
        }

        [Test]
        public void Tick_CannotGoBackInTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ArrivalsOf(planner.Plan(1)).Tick(-1.0));
        }

        [Test]
        public void Timer_MustBeAboveZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaveArrivals(planner.Plan(1), 0));
        }
    }
}
