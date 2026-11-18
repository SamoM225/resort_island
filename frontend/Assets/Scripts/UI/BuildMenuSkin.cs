using UnityEngine;

namespace ResortIsland.UI
{
    /// ResortEmpire UI sprites of the build menu. The copy in Resources/BuildMenuSkin is used for every
    /// sprite the BuildModeHud component does not have assigned.
    [CreateAssetMenu(fileName = "BuildMenuSkin", menuName = "ResortIsland/UI/BuildMenuSkin")]
    public class BuildMenuSkin : ScriptableObject
    {
        [SerializeField] private Sprite buttonOrange;
        [SerializeField] private Sprite buttonPurple;
        [SerializeField] private Sprite buttonBlue;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite hammerIcon;
        [SerializeField] private Sprite padlockIcon;

        [Header("Bottom bar")]
        [SerializeField] private Sprite buttonTeal;
        [SerializeField] private Sprite shipIcon;
        [SerializeField] private Sprite workersIcon;
        [Tooltip("Icon per need id (rooms, food, drinks, relax, ocean_view) for the wave forecast.")]
        [SerializeField] private NeedIcon[] needIcons = new NeedIcon[0];

        [System.Serializable]
        public class NeedIcon
        {
            public string needId;
            public Sprite icon;
        }

        [Header("Game HUD (predloha)")]
        [SerializeField] private Sprite toolbarPlank;
        [SerializeField] private Sprite buttonGrey;
        [SerializeField] private Sprite buttonRoundGreen;
        [SerializeField] private Sprite spyglassIcon;
        [SerializeField] private Sprite gearIcon;
        [SerializeField] private Sprite energyIcon;
        [SerializeField] private Sprite crownIcon;

        public Sprite ToolbarPlank => toolbarPlank;
        public Sprite ButtonGrey => buttonGrey;
        public Sprite ButtonRoundGreen => buttonRoundGreen;
        public Sprite SpyglassIcon => spyglassIcon;
        public Sprite GearIcon => gearIcon;
        public Sprite EnergyIcon => energyIcon;
        public Sprite CrownIcon => crownIcon;

        public Sprite ButtonTeal => buttonTeal;
        public Sprite ShipIcon => shipIcon;
        public Sprite WorkersIcon => workersIcon;

        public Sprite NeedIconFor(string needId)
        {
            foreach (NeedIcon entry in needIcons)
                if (entry != null && entry.needId == needId) return entry.icon;
            return null;
        }

        public Sprite ButtonOrange => buttonOrange;
        public Sprite ButtonPurple => buttonPurple;
        public Sprite ButtonBlue => buttonBlue;
        public Sprite CoinIcon => coinIcon;
        public Sprite HammerIcon => hammerIcon;
        public Sprite PadlockIcon => padlockIcon;
    }
}
