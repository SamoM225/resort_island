using System;

namespace ResortIsland.Waves
{
    public interface IStartWaveView
    {
        event Action Clicked;
        void SetInteractable(bool interactable);
    }
}
