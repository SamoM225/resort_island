using System;

namespace ResortIsland.Waves
{
    public interface INeedsProgress
    {
        event Action Changed;
        int Served(string needId);
        int Queue(string needId);
    }

    public class NoNeedsProgress : INeedsProgress
    {
        public event Action Changed
        {
            add { }
            remove { }
        }

        public int Served(string needId) => 0;

        public int Queue(string needId) => 0;
    }
}