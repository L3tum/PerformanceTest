using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTester.Workers
{
    public class TaskWorker : Worker
    {
        private static readonly int TicksPerMs = (int) (Stopwatch.Frequency / 1000);
        private readonly List<Task<HttpResponseWrapper?>> tasks = new();
        private CountdownEvent countdownEvent = null!;
        private bool stopRequested;
        private EventWaitHandle waitHandle = null!;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        public override void Launch()
        {
            waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            countdownEvent = new CountdownEvent(RequestsPerSecond);
            var sleepInTicks = PerformanceTester.SLEEP_IN_MS * TicksPerMs;
            var totalRps = 0;
            while (!stopRequested)
            {
                var start = Stopwatch.GetTimestamp();
                var rps = RequestsPerSecond;
                totalRps += rps;
                countdownEvent.Reset(countdownEvent.CurrentCount + rps);
                tasks.EnsureCapacity(totalRps);

                for (var i = 0; i < rps; i++)
                {
                    tasks.Add(SendRequest(PerformanceTest.GetRequest()));
                }

                countdownEvent.Wait(1000);
                var elapsed = Stopwatch.GetTimestamp() - start;
                var sleep = sleepInTicks - elapsed;

                if (sleep > 0)
                {
                    sleep /= TicksPerMs;
                    Thread.Sleep((int) sleep);
                }
            }

            countdownEvent.Wait(5000);
            waitHandle.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        private async Task<HttpResponseWrapper?> SendRequest(HttpRequestMessage request)
        {
            try
            {
                var startTime = Stopwatch.GetTimestamp();
                var response = await HttpClient.SendAsync(request, HttpCompletionOption);
                var elapsed = Stopwatch.GetTimestamp() - startTime;
                countdownEvent.Signal();
                return new HttpResponseWrapper {ResponseMessage = response, TimeTaken = elapsed};
            }
            catch
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        public override Task<Statistic[]> Stop()
        {
            return Task.Run(() =>
            {
                stopRequested = true;
                waitHandle.WaitOne();
                var stats = new List<Statistic>(tasks.Count);

                foreach (var task in tasks)
                {
                    var responseWrapper = task.Result;

                    if (responseWrapper is not null)
                    {
                        var response = responseWrapper.Value;
                        Statistic statistic;
                        statistic.Success = PerformanceTest.IsSuccessful(response.ResponseMessage);
                        statistic.RequestMethod = response.ResponseMessage.RequestMessage!.Method.Method;
                        statistic.RequestUri = response.ResponseMessage.RequestMessage!.RequestUri!.PathAndQuery;
                        statistic.StatusCode = (int) response.ResponseMessage.StatusCode;
                        statistic.TimeTakenMilliseconds = (int) (response.TimeTaken / (Stopwatch.Frequency / 1000));
                        stats.Add(statistic);
                    }
                }

                return stats.ToArray();
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        public override int GetCompletedRequests()
        {
            return tasks.Count;
        }
    }
}