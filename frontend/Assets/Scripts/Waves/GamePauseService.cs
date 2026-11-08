using UnityEngine;

namespace ResortIsland.Waves
{
    public class GamePauseService : MonoBehaviour
    {
        private readonly GamePause pause = new GamePause(new UnityTimeScale());

        public bool IsPaused => pause.IsPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            GameObject go = new GameObject("Game Pause Service");
            DontDestroyOnLoad(go);
            go.AddComponent<GamePauseService>();
        }

        private void Update() => pause.Tick(Time.frameCount);

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                pause.Pause();
            else
                pause.ResumeAfter(Time.frameCount);
        }
    }
}
