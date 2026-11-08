using UnityEngine;

namespace ResortIsland.Resort
{
    public class UnlockDebug : MonoBehaviour
    {
        [SerializeField] private string unlockId = "island_lights";

        [ContextMenu("Unlock now")]
        private void UnlockNow() => Unlocks.Unlock(unlockId);
    }
}