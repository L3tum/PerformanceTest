using System.Net.Http;
using System.Runtime.CompilerServices;

namespace PerformanceTest
{
    public abstract class IPerformanceTest
    {
        /**
         * Returns wait time between requests in milliseconds
         */
        public abstract int WaitTime();

        /**
         * Gets the request to be executed. Is called for every request.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public abstract HttpRequestMessage GetRequest();

        /**
         * Determines whether the request body should be awaited for or if the request is finished
         * once the headers are received. Is called for every request.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public virtual bool WaitForBody()
        {
            return true;
        }

        /**
         * Determines whether a request was successful.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public virtual bool IsSuccessful(HttpResponseMessage response)
        {
            return response.IsSuccessStatusCode;
        }
    }
}