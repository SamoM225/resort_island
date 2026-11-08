using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public class WavePlan
    {
        public WavePlan(int wave, IReadOnlyList<NeedDemand> needs)
        {
            Wave = wave;
            Needs = needs;
        }

        public int Wave { get; }
        public IReadOnlyList<NeedDemand> Needs { get; }
    }
}
