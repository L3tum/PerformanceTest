using System.Diagnostics;
using System.Threading.Tasks;
using PerformanceTest;

namespace PerformanceTester.Workers
{
    public abstract class Worker
    {
        protected WrapperClient HttpClient = null!;
        protected IPerformanceTest PerformanceTest = null!;
        protected int RequestsPerSecond;
        protected Stopwatch Stopwatch = null!;

        public void SetPerformanceTest(ref IPerformanceTest performanceTest)
        {
            PerformanceTest = performanceTest;
        }

        public void SetHttpClient(ref WrapperClient httpClient)
        {
            HttpClient = httpClient;
        }

        public void SetRequestsPerSecond(int rps)
        {
            RequestsPerSecond = rps;
        }

        public void SetStopwatch(ref Stopwatch stopwatch)
        {
            Stopwatch = stopwatch;
        }

        public abstract void Launch();

        public abstract Task<Statistic[]> Stop();

        public abstract int GetCompletedRequests();
    }
}