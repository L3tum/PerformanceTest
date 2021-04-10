using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Test
{
    public class TestPerformance : PerformanceTest.PerformanceTest
    {
        public override int MinWaitTime()
        {
            return 1000;
        }

        public override int MaxWaitTime()
        {
            return 1000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override HttpRequestMessage GetRequest()
        {
            return new(HttpMethod.Get, "http://127.0.0.1:8080");
        }
    }
}