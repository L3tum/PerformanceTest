using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTester.Workers
{
    public class TaskWorker : Worker
    {
        private readonly List<Task<HttpResponse>> tasks = new();
        private bool stopRequested;
        private EventWaitHandle waitHandle = null!;

        public override void Launch()
        {
            waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            var tempTasks = new List<Task<HttpResponse>>();
            while (!stopRequested)
            {
                var start = Stopwatch.ElapsedMilliseconds;
                tempTasks.EnsureCapacity(RequestsPerSecond);

                for (var i = 0; i < RequestsPerSecond; i++)
                {
                    tempTasks.Add(Task.Run(() => HttpClient.Send(PerformanceTest.GetRequest())));
                }

                Task.WaitAll(tempTasks.ToArray());
                tasks.AddRange(tempTasks);
                tempTasks.Clear();
                var elapsed = Stopwatch.ElapsedMilliseconds - start;
                var sleep = PerformanceTest.WaitTime() - elapsed;

                if (sleep > 0)
                {
                    Thread.Sleep((int) sleep);
                }
            }

            waitHandle.Set();
        }

        public override Task<Statistic[]> Stop()
        {
            return Task.Run(() =>
            {
                stopRequested = true;
                waitHandle.WaitOne(5000);
                Task.WaitAll(tasks.ToArray());
                var stats = new List<Statistic>(tasks.Count);

                foreach (var task in tasks)
                {
                    var response = task.Result;
                    Statistic statistic;
                    statistic.Success = PerformanceTest.IsSuccessful(response.ResponseMessage);
                    statistic.RequestMethod = response.ResponseMessage.RequestMessage!.Method.Method;
                    statistic.RequestUri = response.ResponseMessage.RequestMessage!.RequestUri!.PathAndQuery;
                    statistic.StatusCode = (int) response.ResponseMessage.StatusCode;
                    statistic.TimeTakenMilliseconds = (int) response.TimeTaken;
                    stats.Add(statistic);
                }

                return stats.ToArray();
            });
        }

        public override int GetCompletedRequests()
        {
            return tasks.Count;
        }
    }
}