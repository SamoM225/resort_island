namespace ResortIsland.Save
{
    /// <summary>
    /// The list of all save migrations shipped with the game.
    ///
    /// How to change the save format:
    /// 1. Change SaveData (e.g. rename "coins" to "gold").
    /// 2. Bump SaveData.CurrentVersion (1 -> 2).
    /// 3. Add a migration that converts the old JSON to the new shape:
    ///
    ///    class V1ToV2 : ISaveMigration
    ///    {
    ///        public int FromVersion => 1;
    ///        public void Apply(JObject save)
    ///        {
    ///            JObject economy = (JObject)save["economy"];
    ///            economy["gold"] = economy["coins"];
    ///            economy.Remove("coins");
    ///        }
    ///    }
    ///
    /// 4. Register it in Steps below and add Tests/EditMode/Fixtures/save_v2.json.
    /// Never edit or delete an existing migration or fixture: players still have those saves.
    ///
    /// Only adding a new field needs no migration; it gets its value from SaveData.CreateNew.
    /// </summary>
    public static class SaveMigrations
    {
        private static readonly ISaveMigration[] Steps =
        {
        };

        public static SaveMigrator CreateDefault() => new SaveMigrator(SaveData.CurrentVersion, Steps);
    }
}
