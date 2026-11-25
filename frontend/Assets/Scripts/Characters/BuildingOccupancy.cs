using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Characters
{
    public sealed class BuildingOccupancy : MonoBehaviour
    {
        private readonly HashSet<GuestAI> occupants = new HashSet<GuestAI>();
        private int capacity = 1;

        public int Capacity => capacity;
        public int Occupancy => occupants.Count;
        public int Available => Mathf.Max(0, capacity - occupants.Count);

        /// <summary>capacity: guests served at once, from the game configs (GameTuning.Capacity).</summary>
        public void Init(int capacity)
        {
            this.capacity = Mathf.Max(1, capacity);
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
