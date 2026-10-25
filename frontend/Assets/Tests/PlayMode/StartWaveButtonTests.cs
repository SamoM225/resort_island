using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.UI;

namespace ResortIsland.Tests
{
    public class StartWaveButtonTests
    {
        private GameObject go;
        private Button button;
        private StartWaveButton view;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Start Wave Button (test)", typeof(RectTransform), typeof(Button));
            button = go.GetComponent<Button>();
            view = go.AddComponent<StartWaveButton>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Click_RaisesClicked()
        {
            int clicks = 0;
            view.Clicked += () => clicks++;

            button.onClick.Invoke();

            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void SetInteractable_TogglesButton()
        {
            view.SetInteractable(false);
            Assert.IsFalse(button.interactable);

            view.SetInteractable(true);
            Assert.IsTrue(button.interactable);
        }
    }
}
