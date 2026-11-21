using System.Collections.Generic;

namespace ResortIsland.Waves
{
    public interface INeedsView
    {
        void Show(IReadOnlyList<NeedRowState> needs);
    }
}