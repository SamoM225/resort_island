using System.Collections.Generic;

namespace ResortIsland.Characters
{
    public sealed class GuestNeedManager
    {
        private readonly List<string> remaining = new List<string>();

        public IReadOnlyList<string> Remaining => remaining;
        public bool IsEmpty => remaining.Count == 0;

        public void Set(params string[] needIds)
        {
            remaining.Clear();
            if (needIds == null) return;
            foreach (string needId in needIds)
                if (!string.IsNullOrEmpty(needId) && !remaining.Contains(needId))
                    remaining.Add(needId);
        }

        public void Fulfill(string needId)
        {
            remaining.Remove(needId);
        }
    }
}
