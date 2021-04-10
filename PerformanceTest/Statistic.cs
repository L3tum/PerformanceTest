namespace PerformanceTest
{
    public struct Statistic
    {
        public string RequestUri;
        public string RequestMethod;
        public int StatusCode;
        public bool Success;
        public int TimeTakenMilliseconds;
    }
}