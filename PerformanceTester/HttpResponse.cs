using System.Net.Http;

namespace PerformanceTester
{
    public class HttpResponse
    {
        public long TimeTaken;
        public HttpResponseMessage ResponseMessage = null!;
    }
}