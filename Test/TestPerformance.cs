using System.Net.Http;
using System.Runtime.CompilerServices;
using PerformanceTest;

namespace Test
{
    public class TestPerformance : IPerformanceTest
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override HttpRequestMessage GetRequest()
        {
            return new(HttpMethod.Get, "/");
        }
    }
}