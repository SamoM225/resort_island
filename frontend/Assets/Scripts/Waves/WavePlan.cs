using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public class WavePlan
    {
        public WavePlan(int wave, int guests, IReadOnlyList<NeedDemand> needs)
        {
            Wave = wave;
            Guests = guests;
            Needs = needs;
        }

        public int Wave { get; }
        public int Guests { get; }
        public IReadOnlyList<NeedDemand> Needs { get; }
    }
}
