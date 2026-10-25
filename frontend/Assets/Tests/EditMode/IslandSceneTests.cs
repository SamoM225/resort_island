using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        public void Scene_HasEventSystemForButtonInput()
        {
            EventSystem eventSystem = FindInScene<EventSystem>();

            Assert.IsNotNull(eventSystem, "Island scene has no EventSystem, buttons would not receive taps.");
            Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>());
        }
    }
}
