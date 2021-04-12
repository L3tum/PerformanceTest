using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTester
{
    public class WrapperClient : HttpClient
    {
        private readonly Stopwatch stopwatch;

        public WrapperClient()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public new HttpResponse Send(HttpRequestMessage request,
            HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = base.Send(request, httpCompletionOption, cancellationToken);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }

        public new async Task<HttpResponse> SendAsync(HttpRequestMessage request,
            HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = await base.SendAsync(request, httpCompletionOption, cancellationToken);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }
    }
}