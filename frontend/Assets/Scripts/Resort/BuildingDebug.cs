using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Resort
{

    public class BuildingDebug : MonoBehaviour
    {
        [SerializeField] private ResortManager resort;
        [SerializeField] private BuildingConfig building;
        [SerializeField] private Vector3Int cell = new Vector3Int(4, -4, 0);

        [ContextMenu("Place building")]
        private void PlaceBuilding()
        {
            if (resort == null || !resort.IsReady)
            {
                Debug.LogWarning("Najprv spusti Play a vyplň pole Resort.");
                return;
            }
            resort.TryPlaceBuilding(building, cell);
        }
    }
}