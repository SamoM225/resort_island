using System;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Backend
{
    public enum BackendEnvironmentType
    {
        Development,
        Production
    }

    /// <summary>
    /// Which backend the game talks to, decided by the build type: the Editor and Development Builds
    /// use the dev backend, release builds use prod. URLs come from Resources/BackendConfig.
    /// </summary>
    public static class BackendEnvironment
    {
        private static BackendConfig config;

        public static BackendEnvironmentType Current => ForBuild(Debug.isDebugBuild);

        /// <summary>Base URL of the current backend without a trailing slash.</summary>
        public static string BaseUrl => ResolveUrl(LoadConfig(), Current, Application.isEditor);

        public static BackendEnvironmentType ForBuild(bool isDevelopmentBuild)
        {
            return isDevelopmentBuild ? BackendEnvironmentType.Development : BackendEnvironmentType.Production;
        }

        public static string ResolveUrl(BackendConfig backendConfig, BackendEnvironmentType environment, bool isEditor)
        {
            string url = environment == BackendEnvironmentType.Production ? backendConfig.ProductionUrl : backendConfig.DevelopmentUrl;
            if (isEditor && !string.IsNullOrWhiteSpace(backendConfig.EditorOverrideUrl))
                url = backendConfig.EditorOverrideUrl;

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException($"BackendConfig has no URL for {environment}.");

            return url.Trim().TrimEnd('/');
        }

        private static BackendConfig LoadConfig()
        {
            if (config == null)
                config = Resources.Load<BackendConfig>(BackendConfig.ResourcePath);
            if (config == null)
                throw new InvalidOperationException($"Missing Resources/{BackendConfig.ResourcePath}.asset.");

            return config;
        }
    }
}
