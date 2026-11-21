using System;
using System.Collections.Generic;
using System.Linq;

namespace ResortIsland.Waves
{
    public class WaveArrivals
    {
        private const double Tolerance = 1e-9;

        private readonly List<(double At, string NeedId)> schedule;
        private double clock;
        private int next;

        public WaveArrivals(WavePlan plan, double duration)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (duration <= 0 || double.IsNaN(duration))
                throw new ArgumentOutOfRangeException(nameof(duration), "Guests need time to arrive.");

            Duration = duration;
            schedule = Schedule(plan, duration);
        }

        public event Action<string> UnitArrived;

        public double Duration { get; }
        public int Total => schedule.Count;
        public int Arrived => next;
        public bool AllArrived => next == schedule.Count;
        public bool TimerEnded => clock + Tolerance >= Duration;
        public double TimeUntilNext => AllArrived ? double.PositiveInfinity : Math.Max(0.0, schedule[next].At - clock);

        public void Tick(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds), "Time cannot go back.");

            clock += seconds;
            while (next < schedule.Count && schedule[next].At <= clock + Tolerance)
            {
                string needId = schedule[next].NeedId;
                next++;
                UnitArrived?.Invoke(needId);
            }
        }

        private static List<(double At, string NeedId)> Schedule(WavePlan plan, double duration)
        {
            var arrivals = new List<(double At, string NeedId)>();
            if (plan.Needs == null)
                return arrivals;

            foreach (NeedDemand need in plan.Needs)
            {
                for (int unit = 1; unit <= need.Units; unit++)
                    arrivals.Add((duration * unit / need.Units, need.NeedId));
            }

            return arrivals.OrderBy(arrival => arrival.At).ToList();
        }
    }
}
