namespace ResortIsland.Backend
{
    public interface IPlayerTokenStore
    {
        string Load();
        void Save(string token);
        void Clear();
    }
}
