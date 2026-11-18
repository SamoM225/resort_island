using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ResortIsland.Resort;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class IslandSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Island.unity";

        private Scene scene;
        private bool openedByTest;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            openedByTest = !scene.isLoaded;
            if (openedByTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (openedByTest)
                EditorSceneManager.CloseScene(scene, true);
        }

        private T FindInScene<T>() where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
        }

        [Test]
        public void WaveController_HasStartWaveButtonAssigned()
        {
            WaveController controller = FindInScene<WaveController>();

            Assert.IsNotNull(controller, "Island scene has no WaveController.");
            Assert.IsNotNull(new SerializedObject(controller).FindProperty("startWaveButton").objectReferenceValue);
        }

        [Test]
        public void WaveController_HasWaveForecastAssigned()
        {
            WaveController controller = FindInScene<WaveController>();

            Assert.IsNotNull(controller, "Island scene has no WaveController.");
            Assert.IsNotNull(new SerializedObject(controller).FindProperty("waveForecast").objectReferenceValue);
        }

        [Test]
        public void Scene_HasEventSystemForButtonInput()
        {
            EventSystem eventSystem = FindInScene<EventSystem>();

            Assert.IsNotNull(eventSystem, "Island scene has no EventSystem, buttons would not receive taps.");
            Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>());
        }

        [Test]
        public void PathStyles_EveryLevelHasAPathSheet()
        {
            Assert.IsNotNull(FindInScene<PathRenderer>(), "Island scene has no PathRenderer, paths would be invisible.");
            var resort = FindInScene<ResortManager>();
            var catalog = new SerializedObject(resort).FindProperty("pathStyles").objectReferenceValue as PathStyleCatalog;

            Assert.IsNotNull(catalog, "ResortManager has no path style catalog.");
            Assert.IsNotNull(catalog.Find(catalog.DefaultStyleId), "The default path style does not exist.");
            foreach (PathStyle style in catalog.Styles)
            {
                Assert.Greater(style.LevelCount, 0, style.Id + " has no levels.");
                for (int level = 1; level <= style.LevelCount; level++)
                {
                    Texture2D sheet = style.Level(level).Sheet;
                    Assert.IsNotNull(sheet, style.Id + " Lv" + level + " has no sheet.");
                    Assert.AreEqual(0, sheet.width % 8, style.Id + " Lv" + level + ": the sheet is not 8 cells wide.");
                    Assert.IsNotNull(style.Level(level).GetSprite(15));
                }
            }
        }

        [Test]
        public void BuildModeHud_HasItsSpritesAssigned()
        {
            var hud = FindInScene<ResortIsland.UI.BuildModeHud>();

            Assert.IsNotNull(hud, "Island scene has no build menu.");
            var serialized = new SerializedObject(hud);
            foreach (string field in new[] { "resort", "placer", "buttonOrange", "buttonPurple", "buttonBlue", "coinIcon", "hammerIcon", "padlockIcon" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, "BuildModeHud." + field + " is not assigned.");
        }
    }
}
