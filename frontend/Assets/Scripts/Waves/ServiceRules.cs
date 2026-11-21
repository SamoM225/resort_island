using System;
using ResortIsland.Config;

namespace ResortIsland.Waves
{
    public class ServiceRules
    {
        public ServiceRules(double serviceTime, double utilizationStaffed, double utilizationEmpty)
        {
            if (serviceTime <= 0)
                throw new ArgumentOutOfRangeException(nameof(serviceTime), "Service time must be above 0.");
            if (utilizationStaffed <= 0)
                throw new ArgumentOutOfRangeException(nameof(utilizationStaffed), "Utilization must be above 0, otherwise a guest is never served.");
            if (utilizationEmpty <= 0)
                throw new ArgumentOutOfRangeException(nameof(utilizationEmpty), "Utilization must be above 0, otherwise a guest is never served.");

            ServiceTime = serviceTime;
            UtilizationStaffed = utilizationStaffed;
            UtilizationEmpty = utilizationEmpty;
        }

        public double ServiceTime { get; }
        public double UtilizationStaffed { get; }
        public double UtilizationEmpty { get; }

        public double ProgressPerSecond(bool staffed) => (staffed ? UtilizationStaffed : UtilizationEmpty) / ServiceTime;

        public static ServiceRules From(WaveSystemConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new ServiceRules(Exact(config.ServiceTimeS), Exact(config.UtilizationStaffed), Exact(config.UtilizationEmpty));
        }

        private static double Exact(float value) => (double)(decimal)value;
    }
}
