using UnityEngine;

namespace ResortIsland.Backend
{
    public class PlayerPrefsTokenStore : IPlayerTokenStore
    {
        public const string Key = "player_token";

        public string Load()
        {
            string token = PlayerPrefs.GetString(Key, string.Empty);
            return token.Length == 0 ? null : token;
        }

        public void Save(string token)
        {
            PlayerPrefs.SetString(Key, token);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
