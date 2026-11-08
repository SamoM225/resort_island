using UnityEngine;
using ResortIsland.Backend;

namespace ResortIsland.Analytics
{
    public class AnalyticsService : MonoBehaviour
    {
        private const float FlushIntervalSeconds = 20f;

        private static AnalyticsService instance;

        private IAnalytics tracker;
        private SessionLengthTracker sessionLength;
        private AnalyticsDispatcher dispatcher;
        private float nextFlushAt;

        public static IAnalytics Current => instance != null ? instance.tracker : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            GameObject go = new GameObject("Analytics Service");
            DontDestroyOnLoad(go);
            go.AddComponent<AnalyticsService>();
        }

        private void Awake()
        {
            instance = this;

            IClock clock = new SystemClock();
            AnalyticsQueue queue = new AnalyticsQueue(AnalyticsFileStore.CreateDefault());
            AnalyticsSession session = new AnalyticsSession();
            tracker = new AnalyticsTracker(queue, session, clock);
            sessionLength = new SessionLengthTracker(session, tracker, clock);

            IBackendClient client = new UnityBackendClient();
            IPlayerAuth auth = new AnonymousPlayerAuth(client, new PlayerPrefsTokenStore());
            dispatcher = new AnalyticsDispatcher(queue, auth, client, Application.version, Application.platform.ToString().ToLowerInvariant());

            sessionLength.Start();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextFlushAt)
                return;

            nextFlushAt = Time.unscaledTime + FlushIntervalSeconds;
            dispatcher.Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                sessionLength.End();
                dispatcher.Flush();
            }
            else
            {
                sessionLength.Start();
            }
        }

        private void OnApplicationQuit() => sessionLength.End();

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
