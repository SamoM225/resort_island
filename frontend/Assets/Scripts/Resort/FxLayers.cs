using System;
using UnityEngine;

namespace ResortIsland.Resort
{
    [Serializable]
    public class FxLayer
    {
        [SerializeField] private string unlockId;   
        [SerializeField] private GameObject fx;

        public string UnlockId => unlockId;
        public GameObject Fx => fx;
    }

    public class FxLayers : MonoBehaviour
    {
        [SerializeField] private FxLayer[] layers;

        private void OnEnable()
        {
            Unlocks.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => Unlocks.OnChanged -= Refresh;

        private void Refresh()
        {
            if (layers == null) return;

            foreach (var layer in layers)
                if (layer.Fx != null) layer.Fx.SetActive(Unlocks.Has(layer.UnlockId));
        }
    }
}