using NUnit.Framework;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Tests
{
    public class BuildingLayoutSaveTests
    {
        [Test]
        public void JsonRoundTrip_PreservesBuildingLayoutFields()
        {
            var layout = new BuildingLayoutSave
            {
                saveVersion = 1,
                savedAtUtc = "2026-11-22T19:15:46Z"
            };
            layout.buildings.Add(new BuildingSave
            {
                instanceId = "building_000",
                buildingId = "restaurant",
                cellX = 4,
                cellY = -3,
                rotation = 2
            });

            string json = JsonUtility.ToJson(layout);
            BuildingLayoutSave loaded = JsonUtility.FromJson<BuildingLayoutSave>(json);

            Assert.AreEqual(1, loaded.saveVersion);
            Assert.AreEqual("2026-11-22T19:15:46Z", loaded.savedAtUtc);
            Assert.AreEqual(1, loaded.buildings.Count);
            Assert.AreEqual("building_000", loaded.buildings[0].instanceId);
            Assert.AreEqual("restaurant", loaded.buildings[0].buildingId);
            Assert.AreEqual(4, loaded.buildings[0].cellX);
            Assert.AreEqual(-3, loaded.buildings[0].cellY);
            Assert.AreEqual(2, loaded.buildings[0].rotation);
        }

        [Test]
        public void JsonRoundTrip_EmptyLayoutRemainsValid()
        {
            string json = JsonUtility.ToJson(new BuildingLayoutSave());
            BuildingLayoutSave loaded = JsonUtility.FromJson<BuildingLayoutSave>(json);

            Assert.AreEqual(1, loaded.saveVersion);
            Assert.IsNotNull(loaded.buildings);
            Assert.AreEqual(0, loaded.buildings.Count);
        }
    }
}
