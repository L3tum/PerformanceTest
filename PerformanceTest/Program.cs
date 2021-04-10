using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTest
{
    internal static class Program
    {
        private static WrapperClient httpClient;
        private static List<PerformanceTest> performanceTests;
        private static readonly int users = 7500;
        private static readonly int spawnRate = 500;
        private static readonly int runTimeInSeconds = 30;
        private static readonly string testToExecute = "TestPerformance";
        private static CountdownEvent countdownEvent;
        private static Stopwatch stopwatch;
        private static ConcurrentStack<Statistic> globalStats;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void QueueTask(CancellationToken token, PerformanceTest test, bool delay)
        {
            if (token.IsCancellationRequested)
            {
                countdownEvent.Signal();
                return;
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
                var (tok, tes) = (ValueTuple<CancellationToken, PerformanceTest>) state!;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void DoRequest(object state)
        {
            var (token, test) = (ValueTuple<CancellationToken, PerformanceTest>) state;
            try
            {
                if (token.IsCancellationRequested)
                {
                    countdownEvent.Signal();
                    return;
                }

                var waitTime = new Random().Next(test.MinWaitTime(), test.MaxWaitTime());
                var request = test.GetRequest();
                var startTime = stopwatch.ElapsedMilliseconds;
                var response = httpClient.Send(request,
                    test.WaitForBody()
                        ? HttpCompletionOption.ResponseContentRead
                        : HttpCompletionOption.ResponseHeadersRead);
                var timeTaken = stopwatch.ElapsedMilliseconds - startTime;
                Statistic statistic;
                statistic.Success = test.IsSuccessful(response);
                statistic.RequestMethod = request.Method.Method;
                statistic.RequestUri = request.RequestUri!.ToString();
                statistic.StatusCode = (int) response.StatusCode;
                statistic.TimeTakenMilliseconds = (int) timeTaken;
                globalStats.Push(statistic);

                if (token.IsCancellationRequested)
                {
                    countdownEvent.Signal();
                    return;
                }

                ThreadPool.QueueUserWorkItem(stat => Task.Delay(waitTime).ContinueWith(t => DoRequest(stat)),
                    (token, test));
            }
            catch (Exception e)
            {
                // Intentionally left blank
            }
        }

        private static void Main(string[] args)
        {
            httpClient = new WrapperClient();
            performanceTests = new List<PerformanceTest>();
            globalStats = new ConcurrentStack<Statistic>();
            countdownEvent = new CountdownEvent(users);
            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            LoadModules();

            PerformanceTest test = null;

            foreach (var performanceTest in performanceTests)
            {
                if (performanceTest.GetType().Name == testToExecute)
                {
                    test = performanceTest;
                    break;
                }
            }

            if (test == null)
            {
                throw new Exception($"Could not find test with name {testToExecute}");
            }

            stopwatch = Stopwatch.StartNew();
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var oldCount = 0;
            var queued = 0;
            var rps = new List<int>();

            Console.WriteLine("Starting up...");
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            while (stopwatch.ElapsedMilliseconds < runTimeInSeconds * 1000)
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

        private static void LoadModules()
        {
            foreach (var file in Directory.GetFiles(
                new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName!))
            {
                if (!file.EndsWith(".dll"))
                {
                    continue;
                }

                try
                {
                    var assembly = Assembly.LoadFile(file);

                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.BaseType != null && type.BaseType == typeof(PerformanceTest))
                        {
                            performanceTests.Add((PerformanceTest) Activator.CreateInstance(type));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static double Percentile(this IEnumerable<double> seq, double percentile)
        {
            var elements = seq.ToArray();
            Array.Sort(elements);
            var realIndex = percentile * (elements.Length - 1);
            var index = (int) realIndex;
            var frac = realIndex - index;
            if (index + 1 < elements.Length)
            {
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            }

            return elements[index];
        }
    }
}