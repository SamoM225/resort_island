namespace ResortIsland.Backend
{
    public readonly struct BackendResponse
    {
        public long StatusCode { get; }
        public string Body { get; }

        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        public BackendResponse(long statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
        }
    }
}
