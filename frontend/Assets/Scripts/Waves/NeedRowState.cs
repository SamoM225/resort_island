namespace ResortIsland.Waves
{
    public class NeedRowState
    {
        public NeedRowState(string name, int served, int total, int queue)
        {
            Name = name;
            Served = served;
            Total = total;
            Queue = queue;
        }

        public string Name { get; }
        public int Served { get; }
        public int Total { get; }
        public int Queue { get; }
    }
}