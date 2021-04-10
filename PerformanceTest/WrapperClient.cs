using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTest
{
    public class WrapperClient : HttpClient
    {
        public new async Task<HttpResponse> SendAsync(HttpRequestMessage request, HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await base.SendAsync(request, httpCompletionOption, cancellationToken);
            var endTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = endTime
            };
        }
    }
}