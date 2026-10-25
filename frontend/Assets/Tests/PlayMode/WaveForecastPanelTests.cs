using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.UI;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WaveForecastPanelTests
    {
        private GameObject go;
        private Text label;
        private WaveForecastPanel panel;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Wave Forecast (test)", typeof(RectTransform));
            label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(go.transform, false);
            panel = go.AddComponent<WaveForecastPanel>();
            typeof(WaveForecastPanel).GetField("label", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(panel, label);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Show_WritesForecastAndShowsPanel()
        {
            go.SetActive(false);

            panel.Show(new WavePlan(1, new[] { new NeedDemand("rooms", 8) }));

            Assert.IsTrue(go.activeSelf);
            Assert.AreEqual("Next wave: Rooms 8", label.text);
        }

        [Test]
        public void Hide_HidesPanel()
        {
            panel.Hide();

            Assert.IsFalse(go.activeSelf);
        }
    }
}
