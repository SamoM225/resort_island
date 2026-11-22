using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Config;

namespace ResortIsland.Characters
{
    public sealed class BuildingOccupancy : MonoBehaviour
    {
        private readonly HashSet<GuestAI> occupants = new HashSet<GuestAI>();
        private int capacity = 1;

        public int Capacity => capacity;
        public int Occupancy => occupants.Count;
        public int Available => Mathf.Max(0, capacity - occupants.Count);

        public void Init(BuildingConfig config)
        {
            capacity = config != null ? config.Capacity : 1;
        }

        public bool TryOccupy(GuestAI guest)
        {
            if (guest == null || occupants.Count >= capacity) return false;
            return occupants.Add(guest);
        }

        public void Release(GuestAI guest)
        {
            if (guest != null) occupants.Remove(guest);
        }
    }
}
