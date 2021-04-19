using System.Net.Http;
using System.Runtime.CompilerServices;
using PerformanceTest;

namespace Test
{
    public class TestPerformance : IPerformanceTest
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override int WaitTime()
        {
            return 1000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override HttpRequestMessage GetRequest()
        {
            return new(HttpMethod.Get, "/");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override bool WaitForBody()
        {
            return false;
        }
    }
}