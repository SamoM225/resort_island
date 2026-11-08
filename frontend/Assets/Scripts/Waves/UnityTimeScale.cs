using UnityEngine;

namespace ResortIsland.Waves
{
    public class UnityTimeScale : ITimeScale
    {
        public float Value
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }
}
