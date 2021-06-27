using System.Net.Http;

namespace PerformanceTester
{
    public struct HttpResponseWrapper
    {
        public HttpResponseMessage ResponseMessage;
        public long TimeTaken;
    }
}