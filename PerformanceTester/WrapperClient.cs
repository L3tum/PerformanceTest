using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public new HttpResponse Send(HttpRequestMessage request,
            HttpCompletionOption httpCompletionOption)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = base.Send(request, httpCompletionOption);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public new HttpResponse Send(HttpRequestMessage request)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = base.Send(request);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public new async Task<HttpResponse> SendAsync(HttpRequestMessage request,
            HttpCompletionOption httpCompletionOption)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = await base.SendAsync(request, httpCompletionOption).ConfigureAwait(false);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public new async Task<HttpResponse> SendAsync(HttpRequestMessage request)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var response = await base.SendAsync(request);
            var timeTaken = stopwatch.ElapsedMilliseconds - startTime;

            return new HttpResponse
            {
                ResponseMessage = response,
                TimeTaken = timeTaken
            };
        }
    }
}