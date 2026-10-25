namespace ResortIsland.Waves
{
    public class NeedDemand
    {
        public NeedDemand(string needId, int units)
        {
            NeedId = needId;
            Units = units;
        }

        public string NeedId { get; }
        public int Units { get; }
    }
}
