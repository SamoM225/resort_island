using UnityEngine;
using ResortIsland.UI;

namespace ResortIsland.Waves
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private StartWaveButton startWaveButton;

        private readonly GameStateMachine stateMachine = new GameStateMachine();
        private StartWavePresenter startWavePresenter;

        public IGameState State => stateMachine;

        private void Start()
        {
            if (startWaveButton == null)
            {
                Debug.LogError("[WaveController] Start Wave button is not assigned! Please assign it in the inspector.");
                return;
            }

            startWavePresenter = new StartWavePresenter(stateMachine, stateMachine, startWaveButton);
        }

        private void OnDestroy()
        {
            startWavePresenter?.Dispose();
        }
    }
}
