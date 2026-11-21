using System;
using System.Collections.Generic;
using System.Linq;

namespace ResortIsland.Waves
{
    public class BuildingService : IBuildingQueue
    {
        private const double Done = 1.0 - 1e-9;

        private readonly ServiceRules rules;
        private readonly List<double> progress = new List<double>();
        private readonly Queue<double> arrivals = new Queue<double>();
        private double clock;

        public BuildingService(string needId, int seats, ServiceRules rules)
        {
            if (string.IsNullOrEmpty(needId))
                throw new ArgumentException("A building serves one need.", nameof(needId));
            if (seats < 1)
                throw new ArgumentOutOfRangeException(nameof(seats), "A building needs at least one seat.");

            NeedId = needId;
            Seats = seats;
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public event Action<double> UnitSeated;
        public event Action UnitServed;

        public string NeedId { get; }
        public int Seats { get; }
        public int Seated => progress.Count;
        public int Waiting => arrivals.Count;
        public bool IsClear => progress.Count == 0 && arrivals.Count == 0;
        public bool Staffed { get; set; }

        public void Arrive()
        {
            arrivals.Enqueue(clock);
            SeatWaiting();
        }

        public void Tick(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds), "Time cannot go back.");

            double rate = rules.ProgressPerSecond(Staffed);
            while (progress.Count > 0)
            {
                double untilNextServed = (1.0 - progress.Max()) / rate;
                if (untilNextServed > seconds)
                    break;

                Advance(untilNextServed, rate);
                seconds -= untilNextServed;
                FinishServed();
                SeatWaiting();
            }

            Advance(seconds, rate);
        }

        private void Advance(double seconds, double rate)
        {
            clock += seconds;
            for (int seat = 0; seat < progress.Count; seat++)
                progress[seat] += seconds * rate;
        }

        private void FinishServed()
        {
            for (int seat = progress.Count - 1; seat >= 0; seat--)
            {
                if (progress[seat] < Done)
                    continue;

                progress.RemoveAt(seat);
                UnitServed?.Invoke();
            }
        }

        private void SeatWaiting()
        {
            while (progress.Count < Seats && arrivals.Count > 0)
            {
                double wait = clock - arrivals.Dequeue();
                progress.Add(0.0);
                UnitSeated?.Invoke(wait);
            }
        }
    }
}
