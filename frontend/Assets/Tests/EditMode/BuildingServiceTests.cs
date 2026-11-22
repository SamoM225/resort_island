using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class BuildingServiceTests
    {
        private const double Tolerance = 1e-6;

        private GameConfigs configs;
        private ServiceRules rules;
        private List<double> waits;
        private int served;

        [SetUp]
        public void SetUp()
        {
            configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            rules = ServiceRules.From(configs.WaveSystem);
            waits = new List<double>();
            served = 0;
        }

        private BuildingService Building(bool staffed, int level = 1)
        {
            BuildingService service = new BuildingService("drinks", BuildingSeats.For(configs.Buildings, level), rules) { Staffed = staffed };
            service.UnitSeated += waits.Add;
            service.UnitServed += () => served++;
            return service;
        }

        private static void Arrive(BuildingService service, int units)
        {
            for (int unit = 0; unit < units; unit++)
                service.Arrive();
        }

        private static void ArriveAt(BuildingService service, params double[] times)
        {
            double now = 0;
            foreach (double time in times)
            {
                service.Tick(time - now);
                now = time;
                service.Arrive();
            }
        }

        [Test]
        public void Seats_ComeFromTheBuildingLevelInConfig()
        {
            Assert.AreEqual(3, BuildingSeats.For(configs.Buildings, 1));
            Assert.AreEqual(5, BuildingSeats.For(configs.Buildings, 2));
            Assert.AreEqual(7, BuildingSeats.For(configs.Buildings, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildingSeats.For(configs.Buildings, 4));
        }

        [Test]
        public void Rules_ComeFromTheWaveSystemConfig()
        {
            Assert.AreEqual(12.0, rules.ServiceTime);
            Assert.AreEqual(1.0, rules.UtilizationStaffed);
            Assert.AreEqual(0.33, rules.UtilizationEmpty);
        }

        [Test]
        public void Rules_WithoutUtilization_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ServiceRules(12, 1, 0));
        }

        [Test]
        public void FreeSeat_IsTakenAtOnceWithoutWaiting()
        {
            BuildingService service = Building(staffed: true);

            service.Arrive();

            Assert.AreEqual(1, service.Seated);
            Assert.AreEqual(0, service.Waiting);
            CollectionAssert.AreEqual(new[] { 0.0 }, waits);
        }

        [Test]
        public void GuestsBeyondTheSeats_WaitInTheQueue()
        {
            BuildingService service = Building(staffed: true);

            Arrive(service, 5);

            Assert.AreEqual(3, service.Seated);
            Assert.AreEqual(2, service.Waiting);
            Assert.AreEqual(3, waits.Count);
        }

        [Test]
        public void StaffedBuilding_ServesAGuestInServiceTime()
        {
            BuildingService service = Building(staffed: true);
            service.Arrive();

            service.Tick(11.9);
            Assert.AreEqual(0, served);

            service.Tick(0.1);
            Assert.AreEqual(1, served);
            Assert.IsTrue(service.IsClear);
        }

        [Test]
        public void BuildingWithoutWorker_ServesSlower()
        {
            BuildingService service = Building(staffed: false);
            service.Arrive();

            service.Tick(36.0);
            Assert.AreEqual(0, served);

            service.Tick(0.4);
            Assert.AreEqual(1, served);
        }

        [Test]
        public void FreedSeat_GoesToTheFirstGuestInTheQueue()
        {
            BuildingService service = Building(staffed: true);
            Arrive(service, 4);

            service.Tick(12.0);

            Assert.AreEqual(3, served);
            Assert.AreEqual(1, service.Seated);
            Assert.AreEqual(0, service.Waiting);
            Assert.AreEqual(12.0, waits[3], Tolerance);
        }

        [Test]
        public void HigherLevel_SeatsMoreGuests()
        {
            BuildingService service = Building(staffed: true, level: 3);

            Arrive(service, 8);

            Assert.AreEqual(7, service.Seated);
            Assert.AreEqual(1, service.Waiting);
        }

        [Test]
        public void WorkerArriving_SpeedsUpGuestsAlreadySeated()
        {
            BuildingService service = Building(staffed: false);
            service.Arrive();
            service.Tick(18.0);

            service.Staffed = true;

            service.Tick(6.05);
            Assert.AreEqual(0, served);
            service.Tick(0.02);
            Assert.AreEqual(1, served);
        }

        [Test]
        public void WorkerLeaving_SlowsDownGuestsAlreadySeated()
        {
            BuildingService service = Building(staffed: true);
            service.Arrive();
            service.Tick(6.0);

            service.Staffed = false;

            service.Tick(18.0);
            Assert.AreEqual(0, served);
            service.Tick(0.2);
            Assert.AreEqual(1, served);
        }

        [Test]
        public void Waits_DoNotDependOnFrameLength()
        {
            BuildingService oneFrame = Building(staffed: false);
            Arrive(oneFrame, 10);
            oneFrame.Tick(200.0);
            List<double> oneFrameWaits = new List<double>(waits);

            waits.Clear();
            BuildingService manyFrames = Building(staffed: false);
            Arrive(manyFrames, 10);
            for (int frame = 0; frame < 12000; frame++)
                manyFrames.Tick(1.0 / 60.0);

            Assert.AreEqual(oneFrameWaits.Count, waits.Count);
            for (int unit = 0; unit < waits.Count; unit++)
                Assert.AreEqual(oneFrameWaits[unit], waits[unit], Tolerance);
        }

        [Test]
        public void QueueIsClear_OnlyWhenNobodySitsOrWaits()
        {
            BuildingService service = Building(staffed: true);
            Assert.IsTrue(service.IsClear);

            Arrive(service, 4);
            service.Tick(12.0);
            Assert.IsFalse(service.IsClear);

            service.Tick(12.0);
            Assert.IsTrue(service.IsClear);
        }

        [Test]
        public void Tick_CannotGoBackInTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Building(staffed: true).Tick(-1.0));
        }

        [Test]
        public void BurstWithoutWorker_WaitsAsInWaveSim()
        {
            BuildingService service = Building(staffed: false);

            Arrive(service, 10);
            service.Tick(200.0);

            double[] expected = { 0, 0, 0, 36.363636, 36.363636, 36.363636, 72.727273, 72.727273, 72.727273, 109.090909 };
            Assert.AreEqual(expected.Length, waits.Count);
            for (int unit = 0; unit < expected.Length; unit++)
                Assert.AreEqual(expected[unit], waits[unit], Tolerance);
        }

        [Test]
        public void EvenArrivalsWithoutWorker_WaitAsInWaveSim()
        {
            BuildingService service = Building(staffed: false);

            ArriveAt(service, 9, 18, 27, 36, 45, 54, 63, 72, 81, 90);
            service.Tick(200.0);

            double[] expected = { 0, 0, 0, 9.363636, 9.363636, 9.363636, 18.727273, 18.727273, 18.727273, 28.090909 };
            Assert.AreEqual(expected.Length, waits.Count);
            for (int unit = 0; unit < expected.Length; unit++)
                Assert.AreEqual(expected[unit], waits[unit], Tolerance);
        }
    }
}
