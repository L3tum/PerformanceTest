using System;
using System.Net.Http;

namespace PerformanceTester
{
    public class HttpResponse : IDisposable
    {
        public long TimeTaken;
        public HttpResponseMessage ResponseMessage = null!;

        public void Dispose()
        {
            ResponseMessage.Dispose();
        }
    }
}