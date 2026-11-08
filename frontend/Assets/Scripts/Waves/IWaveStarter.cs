namespace ResortIsland.Waves
{
    public interface IWaveStarter
    {
        bool CanStartWave { get; }
        bool TryStartWave();
    }
}
