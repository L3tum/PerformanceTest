using System.Net.Http;
using System.Runtime.CompilerServices;

namespace PerformanceTest
{
    public abstract class IPerformanceTest
    {
        /**
         * Gets the request to be executed. Is called for every request.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public abstract HttpRequestMessage GetRequest();

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