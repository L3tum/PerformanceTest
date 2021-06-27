using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PerformanceTest;

namespace PerformanceTester.Workers
{
    public abstract class Worker
    {
        protected HttpClient HttpClient = null!;
        protected HttpCompletionOption HttpCompletionOption;
        protected IPerformanceTest PerformanceTest = null!;
        protected int RequestsPerSecond;

        public void SetPerformanceTest(ref IPerformanceTest performanceTest)
        {
            PerformanceTest = performanceTest;
        }

        public void SetHttpClient(ref HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        public void SetRequestsPerSecond(int rps)
        {
            RequestsPerSecond = rps;
        }

        public void SetHttpCompletionOption(HttpCompletionOption option)
        {
            HttpCompletionOption = option;
        }

        public void SetHttpCompletionOption(bool waitForBody)
        {
            HttpCompletionOption = waitForBody
                ? HttpCompletionOption.ResponseContentRead
                : HttpCompletionOption.ResponseHeadersRead;
        }

        public abstract void Launch();

        public abstract Task<Statistic[]> Stop();

        public abstract int GetCompletedRequests();
    }
}