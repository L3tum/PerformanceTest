using System.Net.Http;
using System.Runtime.CompilerServices;
using PerformanceTest;

namespace Test
{
    public class TestPerformance : IPerformanceTest
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
            return new(HttpMethod.Get, "/");
        }
    }
}