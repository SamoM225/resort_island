using System;
using System.Collections.Generic;
using System.Linq;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    public class ScriptedWavePlanner : IWavePlanner
    {
        private readonly WavesConfig waves;
        private readonly HashSet<string> activeNeeds;

        public ScriptedWavePlanner(WavesConfig waves, IEnumerable<string> activeNeeds)
        {
            this.waves = waves ?? throw new ArgumentNullException(nameof(waves));
            this.activeNeeds = new HashSet<string>(activeNeeds ?? throw new ArgumentNullException(nameof(activeNeeds)));
        }

        public WavePlan Plan(int wave)
        {
            WaveConfig config = waves.Waves.FirstOrDefault(entry => entry.Wave == wave);
            if (config == null)
                return null;

            List<NeedDemand> needs = config.Demand
                .Where(demand => activeNeeds.Contains(demand.Key))
                .Select(demand => new NeedDemand(demand.Key, demand.Value))
                .ToList();
            return new WavePlan(wave, needs);
        }
    }
}
