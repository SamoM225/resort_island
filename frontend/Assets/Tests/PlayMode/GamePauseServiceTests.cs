using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class GamePauseServiceTests
    {
        private GamePauseService service;

        [SetUp]
        public void SetUp()
        {
            service = Object.FindFirstObjectByType<GamePauseService>();
            Assert.IsNotNull(service, "Game Pause Service is created on start.");
        }

        [TearDown]
        public void TearDown()
        {
            service.SendMessage("OnApplicationPause", false);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Background_StopsGameTime()
        {
            service.SendMessage("OnApplicationPause", true);
            float frozenAt = Time.time;

            yield return null;
            yield return null;

            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(frozenAt, Time.time);
            Assert.IsTrue(service.IsPaused);
        }

        [UnityTest]
        public IEnumerator ComingBack_ContinuesGameTime()
        {
            service.SendMessage("OnApplicationPause", true);
            yield return null;

            service.SendMessage("OnApplicationPause", false);
            yield return null;
            yield return null;
            float resumedAt = Time.time;
            yield return null;

            Assert.AreEqual(1f, Time.timeScale);
            Assert.Greater(Time.time, resumedAt);
            Assert.IsFalse(service.IsPaused);
        }
    }
}
