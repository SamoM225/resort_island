using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ResortIsland.Save
{
    /// <summary>Upgrades a save from FromVersion to FromVersion + 1.</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        void Apply(JObject save);
    }

    /// <summary>
    /// Runs migrations one version at a time, so a save from any older version
    /// walks through every step up to the current one.
    /// </summary>
    public class SaveMigrator
    {
        private readonly Dictionary<int, ISaveMigration> migrations = new Dictionary<int, ISaveMigration>();

        public int CurrentVersion { get; }

        public SaveMigrator(int currentVersion, IEnumerable<ISaveMigration> steps)
        {
            if (currentVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(currentVersion), "Save version starts at 1.");

            CurrentVersion = currentVersion;

            foreach (ISaveMigration step in steps)
            {
                if (step.FromVersion < 1 || step.FromVersion >= currentVersion)
                    throw new InvalidOperationException($"Migration {step.GetType().Name} starts at v{step.FromVersion}, outside v1..v{currentVersion - 1}.");
                if (migrations.ContainsKey(step.FromVersion))
                    throw new InvalidOperationException($"Two migrations start at v{step.FromVersion}.");
                migrations.Add(step.FromVersion, step);
            }

            for (int version = 1; version < currentVersion; version++)
            {
                if (!migrations.ContainsKey(version))
                    throw new InvalidOperationException($"Missing save migration v{version} -> v{version + 1}.");
            }
        }

        public void Migrate(JObject save, int fromVersion)
        {
            for (int version = fromVersion; version < CurrentVersion; version++)
            {
                migrations[version].Apply(save);
                save["saveVersion"] = version + 1;
            }
        }
    }
}
