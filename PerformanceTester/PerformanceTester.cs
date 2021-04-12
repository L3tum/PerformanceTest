using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PerformanceTest;

namespace PerformanceTester
{
    public class PerformanceTester
    {
        private readonly string host;
        private readonly IPerformanceTest[] performanceTests;
        private readonly int runTimeInSeconds;
        private readonly int spawnRate;
        private readonly string testToExecute;
        private readonly int users;
        private CountdownEvent countdownEvent = null!;
        private ConcurrentStack<Statistic> globalStats = null!;
        private WrapperClient httpClient = null!;
        private List<int> rps = null!;

        public PerformanceTester(int users, int spawnRate, int runTimeInSeconds, string testToExecute,
            string? fileToLoad, string host)
        {
            this.users = users;
            this.spawnRate = spawnRate;
            this.runTimeInSeconds = runTimeInSeconds;
            this.testToExecute = testToExecute;
            this.host = host;
            performanceTests = TestLoader.LoadTests(fileToLoad);
        }

        public PerformanceTester(Options options) : this(options.Users, options.SpawnRate, options.RunTimeInSeconds,
            options.Test, options.File, options.Host)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private void QueueTask(CancellationToken token, IPerformanceTest test, bool delay)
        {
            if (token.IsCancellationRequested)
            {
                countdownEvent.Signal();
                return;
            }

            ThreadPool.QueueUserWorkItem(async state =>
            {
                var (tok, tes) = (ValueTuple<CancellationToken, IPerformanceTest>) state!;
                await Task.Delay(delay ? new Random().Next(tes.MinWaitTime(), tes.MaxWaitTime()) : 0)
                    .ContinueWith(async t =>
                    {
                        var request = tes.GetRequest();
                        request.RequestUri = new Uri($"{host}{request.RequestUri}");
                        var response = await httpClient.SendAsync(request,
                            tes.WaitForBody()
                                ? HttpCompletionOption.ResponseContentRead
                                : HttpCompletionOption.ResponseHeadersRead, tok);

                        if (tok.IsCancellationRequested)
                        {
                            countdownEvent.Signal();
                            return;
                        }

                        Statistic statistic;
                        statistic.Success = tes.IsSuccessful(response.ResponseMessage);
                        statistic.RequestMethod = response.ResponseMessage.RequestMessage!.Method.Method;
                        statistic.RequestUri = response.ResponseMessage.RequestMessage!.RequestUri!.PathAndQuery;
                        statistic.StatusCode = (int) response.ResponseMessage.StatusCode;
                        statistic.TimeTakenMilliseconds = (int) response.TimeTaken;
                        globalStats.Push(statistic);

                        if (tok.IsCancellationRequested)
                        {
                            countdownEvent.Signal();
                            return;
                        }

                        QueueTask(tok, test, true);
                    });
            }, (token, test));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void LaunchTest(IPerformanceTest test)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var oldCount = 0;
            var queued = 0;
            var runTimeInMilliseconds = runTimeInSeconds * 1000;
            var sw = Stopwatch.StartNew();

            Console.WriteLine("Starting up...");

            while (sw.ElapsedMilliseconds < runTimeInMilliseconds)
            {
                if (queued < users)
                {
                    for (var i = 0; i < spawnRate && queued < users; i++)
                    {
                        QueueTask(token, test, false);
                        queued++;
                    }

                    Console.WriteLine("Spawned {0} out of {1} threads", queued, users);

                    if (queued == users)
                    {
                        Console.WriteLine("Finished spawning {0} threads", users);
                    }
                }

                Thread.Sleep(1000);
                var newCount = globalStats.Count;
                var diff = newCount - oldCount;
                rps.Add(diff);
                Console.WriteLine($"Executed {newCount} Requests | {diff} RPS");
                oldCount = newCount;
            }

            Console.WriteLine("Shutting down...");
            cancellationTokenSource.Cancel();
            countdownEvent.Wait(10000);

            Console.WriteLine(
                $"{countdownEvent.InitialCount - countdownEvent.CurrentCount} out of {countdownEvent.InitialCount} shut down.");
        }

        public void RunTest()
        {
            var test = performanceTests.FirstOrDefault(test => test.GetType().Name == testToExecute);

            if (test == null)
            {
                throw new KeyNotFoundException($"Test {testToExecute} could not be found!");
            }

            countdownEvent = new CountdownEvent(users);
            globalStats = new ConcurrentStack<Statistic>();
            rps = new List<int>();
            httpClient = new WrapperClient();
            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);
            Thread.Sleep(100);

            var oldLatencyMode = GCSettings.LatencyMode;
            var oldThreadPriority = Thread.CurrentThread.Priority;
            GC.Collect();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            GC.Collect();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            LaunchTest(test);
            Thread.CurrentThread.Priority = oldThreadPriority;
            GCSettings.LatencyMode = oldLatencyMode;
        }

        public void PrintResults()
        {
            var responseTimes = globalStats.Select(stat => (double) stat.TimeTakenMilliseconds).ToArray();
            Console.WriteLine();
            Console.WriteLine($"Min Response time: {responseTimes.Min()}ms");
            Console.WriteLine($"Average Response time: {Math.Round(responseTimes.Average(), 0)}ms");
            Console.WriteLine($"95th Response time: {responseTimes.Percentile(0.95)}ms");
            Console.WriteLine($"99th Response time: {responseTimes.Percentile(0.99)}ms");
            Console.WriteLine($"Max Response time: {responseTimes.Max()}ms");
            Console.WriteLine($"Average RPS: {Math.Round(rps.Average(), 0)}");
            Console.WriteLine($"Requests: {globalStats.Count}");
            Console.WriteLine($"Successful Requests {globalStats.Count(stat => stat.Success)}");
            Console.WriteLine($"Failed Requests {globalStats.Count(stat => !stat.Success)}");
        }

        public Dictionary<string, List<Statistic>> GetStatistics()
        {
            var stats = new Dictionary<string, List<Statistic>>();

            foreach (var statistic in globalStats)
            {
                var key = $"{statistic.RequestMethod}:{statistic.RequestUri}";
                if (!stats.ContainsKey(key))
                {
                    stats.Add(key, new List<Statistic>());
                }

                stats[key].Add(statistic);
            }

            return stats;
        }
    }
}