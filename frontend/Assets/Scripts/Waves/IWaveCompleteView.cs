using System;

namespace ResortIsland.Waves
{
    public interface IWaveCompleteView
    {
        event Action Claimed;
        void Show(int stars, bool perfect, int satisfactionPercent, int coins);
        void Hide();
    }
}