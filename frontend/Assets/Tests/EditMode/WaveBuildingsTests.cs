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
    public class WaveBuildingsTests
    {
        private GameConfigs configs;
        private List<(string NeedId, double Wait)> seated;

        [SetUp]
        public void SetUp()
        {
            configs = GameConfigs.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/configs_response.json")));
            seated = new List<(string, double)>();
        }

        private WaveBuildings Watch(WaveBuildings buildings)
        {
            buildings.UnitSeated += (needId, wait) => seated.Add((needId, wait));
            return buildings;
        }

        private static BuildingService OneSeat(string needId) => new BuildingService(needId, 1, new ServiceRules(12, 1, 0.33)) { Staffed = true };

        [Test]
        public void Configs_GiveOneBuildingPerNeed()
        {
            WaveBuildings buildings = WaveBuildings.From(configs);

            CollectionAssert.AreEquivalent(new[] { "rooms", "food", "drinks", "relax", "ocean_view" }, buildings.Queues.Select(queue => queue.NeedId));
            Assert.IsTrue(buildings.Queues.All(queue => queue.Seats == 3));
        }

        [Test]
        public void Unit_GoesToTheBuildingOfItsNeed()
        {
            WaveBuildings buildings = WaveBuildings.From(configs);

            buildings.Arrive("drinks");

            Assert.AreEqual(1, buildings.Queues.Single(queue => queue.NeedId == "drinks").Seated);
            Assert.AreEqual(0, buildings.Queues.Where(queue => queue.NeedId != "drinks").Sum(queue => queue.Seated));
        }

        [Test]
        public void SeatedUnit_TellsItsNeedAndWait()
        {
            WaveBuildings buildings = Watch(new WaveBuildings(new[] { OneSeat("drinks") }));

            buildings.Arrive("drinks");
            buildings.Arrive("drinks");
            buildings.Tick(12.0);

            Assert.AreEqual(2, seated.Count);
            Assert.AreEqual(("drinks", 0.0), seated[0]);
            Assert.AreEqual("drinks", seated[1].NeedId);
            Assert.AreEqual(12.0, seated[1].Wait, 1e-6);
        }

        [Test]
        public void Tick_MovesEveryBuilding()
        {
            WaveBuildings buildings = new WaveBuildings(new[] { OneSeat("drinks"), OneSeat("food") });
            buildings.Arrive("drinks");
            buildings.Arrive("food");

            buildings.Tick(12.0);

            Assert.IsTrue(buildings.Queues.All(queue => queue.IsClear));
        }

        [Test]
        public void NeedWithoutBuilding_CannotArrive()
        {
            WaveBuildings buildings = new WaveBuildings(new[] { OneSeat("drinks") });

            Assert.Throws<ArgumentException>(() => buildings.Arrive("food"));
        }

        [Test]
        public void Need_HasOneBuilding()
        {
            Assert.Throws<ArgumentException>(() => new WaveBuildings(new[] { OneSeat("drinks"), OneSeat("drinks") }));
        }
    }
}
