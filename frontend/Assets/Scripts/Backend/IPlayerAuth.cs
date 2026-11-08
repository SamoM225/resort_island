using System;

namespace ResortIsland.Backend
{
    public interface IPlayerAuth
    {
        void RequestToken(Action<string> done);
        void Invalidate();
    }
}
