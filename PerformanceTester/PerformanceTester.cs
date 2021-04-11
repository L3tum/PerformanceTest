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
        private readonly CountdownEvent countdownEvent;
        private readonly ConcurrentStack<Statistic> globalStats;
        private readonly WrapperClient httpClient;
        private readonly IPerformanceTest[] performanceTests;
        private readonly List<int> rps;
        private readonly int runTimeInSeconds;
        private readonly int spawnRate;
        private readonly string testToExecute;
        private readonly int users;
        private Stopwatch? stopwatch;

        public PerformanceTester(int users, int spawnRate, int runTimeInSeconds, string testToExecute,
            string? fileToLoad)
        {
            httpClient = new WrapperClient();
            this.users = users;
            this.spawnRate = spawnRate;
            this.runTimeInSeconds = runTimeInSeconds;
            this.testToExecute = testToExecute;
            performanceTests = TestLoader.LoadTests(fileToLoad);
            countdownEvent = new CountdownEvent(users);
            globalStats = new ConcurrentStack<Statistic>();
            rps = new List<int>();
        }

        public PerformanceTester(Options options) : this(options.Users, options.SpawnRate, options.RunTimeInSeconds,
            options.Test, options.File)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void QueueTask(CancellationToken token, IPerformanceTest test, bool delay)
        {
            if (token.IsCancellationRequested)
            {
                countdownEvent.Signal();
                return;
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
                var (tok, tes) = (ValueTuple<CancellationToken, IPerformanceTest>) state!;
                Task.Delay(delay ? new Random().Next(tes.MinWaitTime(), tes.MaxWaitTime()) : 0)
                    .ContinueWith(t =>
                    {
                        var request = tes.GetRequest();
                        return httpClient.SendAsync(request,
                            tes.WaitForBody()
                                ? HttpCompletionOption.ResponseContentRead
                                : HttpCompletionOption.ResponseHeadersRead, tok);
                    })
                    .ContinueWith(async t =>
                    {
                        if (tok.IsCancellationRequested)
                        {
                            countdownEvent.Signal();
                            return;
                        }

                        var response = await await t;
                        Statistic statistic;
                        statistic.Success = tes.IsSuccessful(response.ResponseMessage);
                        statistic.RequestMethod = response.ResponseMessage.RequestMessage!.Method.Method;
                        statistic.RequestUri = response.ResponseMessage.RequestMessage!.RequestUri!.ToString();
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

            Console.WriteLine("Starting up...");

            while (stopwatch!.ElapsedMilliseconds < runTimeInSeconds * 1000)
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

            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            var oldLatencyMode = GCSettings.LatencyMode;
            var oldThreadPriority = Thread.CurrentThread.Priority;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            stopwatch = Stopwatch.StartNew();
            LaunchTest(test);
            stopwatch.Stop();
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
    }
}