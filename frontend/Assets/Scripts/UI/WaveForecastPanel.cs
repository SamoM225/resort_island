using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    public class WaveForecastPanel : MonoBehaviour, IWaveForecastView
    {
        [SerializeField] private Text label;

        public void Show(WavePlan plan)
        {
            label.text = WaveForecastFormatter.Format(plan);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
