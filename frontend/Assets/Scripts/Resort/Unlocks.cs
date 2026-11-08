using System;
using System.Collections.Generic;

namespace ResortIsland.Resort
{
    public static class Unlocks
    {
        private static readonly HashSet<string> unlocked = new HashSet<string>();

        public static event Action<string> OnUnlocked;

        public static event Action OnChanged;

        public static IEnumerable<string> All => unlocked;

        public static bool Has(string id) => string.IsNullOrEmpty(id) || unlocked.Contains(id);

        public static void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || !unlocked.Add(id)) return;

            OnUnlocked?.Invoke(id);
            OnChanged?.Invoke();
        }

        public static void Load(IEnumerable<string> ids)
        {
            unlocked.Clear();
            foreach (var id in ids) unlocked.Add(id);
            OnChanged?.Invoke();
        }

        public static void Reset()
        {
            unlocked.Clear();
            OnChanged?.Invoke();
        }
    }
}