using System;

namespace ResortIsland.Backend
{
    public interface IBackendClient
    {
        void PostJson(string path, string json, string token, Action<BackendResponse> done);
    }
}
