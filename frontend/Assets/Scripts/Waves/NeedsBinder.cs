using System.Collections;
using UnityEngine;
using ResortIsland.Backend;
using ResortIsland.Config;
using ResortIsland.UI;

namespace ResortIsland.Waves
{

    public class NeedsBinder : MonoBehaviour
    {
        [SerializeField] private WaveController waveController;
        [SerializeField] private NeedsPanel panel;

        private NeedsPresenter presenter;

        private IEnumerator Start()
        {
            
            yield return null;

            if (waveController == null || panel == null)
            {
                Debug.LogError("[NeedsBinder] Priraď Wave Controller a Needs Panel v Inspectore.");
                yield break;
            }

            if (waveController.Planner == null)
            {
                Debug.LogError("[NeedsBinder] Plán vĺn nie je k dispozícii, panel sa nenapojí.");
                yield break;
            }

            GameConfigs configs = GameConfigService.Current;
            if (configs != null && configs.WaveSystem != null)
                panel.SetWarningThreshold(configs.WaveSystem.BottleneckThreshold);
            else
                Debug.LogError("[NeedsBinder] Game configs are not loaded, the panel will not warn about long queues (bottleneck_threshold).");

            presenter = new NeedsPresenter(waveController.State, waveController.Planner, FindProgress(), panel);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }

        private static INeedsProgress FindProgress()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is INeedsProgress found)
                    return found;
            }

            Debug.LogWarning("[NeedsBinder] Nikto zatiaľ nedodáva počty obslúžených a radov (INeedsProgress), panel ukazuje 0.");
            return new NoNeedsProgress();
        }
    }
}