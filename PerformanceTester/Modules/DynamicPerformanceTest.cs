using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using PerformanceTest;

namespace PerformanceTester.Modules
{
    public class DynamicPerformanceTest : IPerformanceTest
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override HttpRequestMessage GetRequest()
        {
            return new(HttpMethod.Get, "/?dynamic=true");
        }
    }
}