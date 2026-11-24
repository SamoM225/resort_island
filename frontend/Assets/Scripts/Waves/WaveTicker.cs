using System;

namespace ResortIsland.Waves
{
    public class WaveTicker
    {
        private readonly WaveArrivalsPresenter arrivals;
        private readonly WaveBuildings buildings;

        public WaveTicker(WaveArrivalsPresenter arrivals, WaveBuildings buildings)
        {
            this.arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        }

        public void Tick(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds), "Time cannot go back.");

            do
            {
                double step = Math.Min(seconds, arrivals.TimeUntilNext);
                buildings.Tick(step);
                arrivals.Tick(step);
                seconds -= step;
            }
            while (seconds > 0);
        }
    }
}
