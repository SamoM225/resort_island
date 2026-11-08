using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ResortIsland.Backend;
using ResortIsland.Config;

namespace ResortIsland.Tests
{
    public class BackendEnvironmentTests
    {
        private BackendConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<BackendConfig>();
            SetUrls("https://dev.example.com", "https://prod.example.com/", "");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        private void SetUrls(string development, string production, string editorOverride)
        {
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("developmentUrl").stringValue = development;
            serialized.FindProperty("productionUrl").stringValue = production;
            serialized.FindProperty("editorOverrideUrl").stringValue = editorOverride;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void DevelopmentBuild_UsesDevelopmentBackend()
        {
            Assert.AreEqual(BackendEnvironmentType.Development, BackendEnvironment.ForBuild(isDevelopmentBuild: true));
        }

        [Test]
        public void ReleaseBuild_UsesProductionBackend()
        {
            Assert.AreEqual(BackendEnvironmentType.Production, BackendEnvironment.ForBuild(isDevelopmentBuild: false));
        }

        [Test]
        public void Editor_UsesDevelopmentBackend()
        {
            Assert.AreEqual(BackendEnvironmentType.Development, BackendEnvironment.Current);
        }

        [Test]
        public void ResolveUrl_ReturnsUrlOfEnvironmentWithoutTrailingSlash()
        {
            Assert.AreEqual("https://dev.example.com", BackendEnvironment.ResolveUrl(config, BackendEnvironmentType.Development, isEditor: false));
            Assert.AreEqual("https://prod.example.com", BackendEnvironment.ResolveUrl(config, BackendEnvironmentType.Production, isEditor: false));
        }

        [Test]
        public void ResolveUrl_EditorOverrideAppliesOnlyInEditor()
        {
            SetUrls("https://dev.example.com", "https://prod.example.com", "http://localhost:8000/");

            Assert.AreEqual("http://localhost:8000", BackendEnvironment.ResolveUrl(config, BackendEnvironmentType.Development, isEditor: true));
            Assert.AreEqual("https://prod.example.com", BackendEnvironment.ResolveUrl(config, BackendEnvironmentType.Production, isEditor: false));
        }

        [Test]
        public void ResolveUrl_WithoutUrl_Throws()
        {
            SetUrls("https://dev.example.com", "", "");

            Assert.Throws<InvalidOperationException>(() => BackendEnvironment.ResolveUrl(config, BackendEnvironmentType.Production, isEditor: false));
        }

        [Test]
        public void BundledConfig_IsLoadableFromResources()
        {
            BackendConfig bundled = Resources.Load<BackendConfig>(BackendConfig.ResourcePath);

            Assert.IsNotNull(bundled);
            Assert.IsNotEmpty(bundled.DevelopmentUrl);
            Assert.IsNotEmpty(bundled.ProductionUrl);
        }
    }
}
