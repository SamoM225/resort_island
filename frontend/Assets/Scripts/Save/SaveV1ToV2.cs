using Newtonsoft.Json.Linq;

namespace ResortIsland.Save
{
    /// <summary>
    /// v2 keeps the wave progress (wave) and the building layout of SAVE LAYOUT (buildingLayout, until then
    /// its own building_layout.json; ResortManager imports that file once). A v1 save never stored the wave,
    /// so the game goes on from wave 1, waiting for the player to start it, like it did before.
    /// </summary>
    public class SaveV1ToV2 : ISaveMigration
    {
        public int FromVersion => 1;

        public void Apply(JObject save)
        {
            save["wave"] = new JObject
            {
                ["nextWave"] = 1,
                ["state"] = WaveSaveData.AwaitingWave,
                ["unclaimedWaits"] = null
            };
            save["buildingLayout"] = null;
        }
    }
}
