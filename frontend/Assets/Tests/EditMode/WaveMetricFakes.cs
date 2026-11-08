using System;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    internal class FakeWaveResult : IWaveResult
    {
        public int Rating { get; set; }
        public float Satisfaction { get; set; }
    }

    internal class FakeWorkerMoves : IWorkerMoves
    {
        public event Action Moved;

        public void Move() => Moved?.Invoke();
    }
}
