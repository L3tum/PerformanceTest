using System;
using System.Net.Http;
using PerformanceTest;

namespace PerformanceTester.Modules
{
    public class DynamicPerformanceTest : IPerformanceTest
    {
        public override int WaitTime()
        {
            return 1000;
        }

        public override HttpRequestMessage GetRequest()
        {
            return new(HttpMethod.Get, "/?dynamic=true");
        }
    }
}