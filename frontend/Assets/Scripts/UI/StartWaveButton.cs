using System;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    [RequireComponent(typeof(Button))]
    public class StartWaveButton : MonoBehaviour, IStartWaveView
    {
        private Button button;

        public event Action Clicked;

        private Button Button => button != null ? button : button = GetComponent<Button>();

        private void OnEnable() => Button.onClick.AddListener(OnClick);

        private void OnDisable() => Button.onClick.RemoveListener(OnClick);

        public void SetInteractable(bool interactable) => Button.interactable = interactable;

        private void OnClick() => Clicked?.Invoke();
    }
}
