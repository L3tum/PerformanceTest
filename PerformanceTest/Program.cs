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

namespace PerformanceTest
{
    internal static class Program
    {
        private static HttpClient httpClient;
        private static List<PerformanceTest> performanceTests;
        private static readonly int users = 5200;
        private static readonly int spawnRate = 500;
        private static readonly int runTimeInSeconds = 30;
        private static readonly string testToExecute = "TestPerformance";
        private static CountdownEvent countdownEvent;
        private static Thread[] threads;
        private static Stopwatch stopwatch;
        private static ConcurrentStack<Statistic> globalStats;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DoRequests(ref CancellationToken token, ref PerformanceTest test)
        {
            try
            {
                var waitTime = new Random().Next(test.MinWaitTime(), test.MaxWaitTime());

                while (!token.IsCancellationRequested)
                {
                    Statistic statistic;
                    var request = test.GetRequest();
                    statistic.RequestMethod = request.Method.Method;
                    statistic.RequestUri = request.RequestUri!.ToString();
                    var startTime = stopwatch.ElapsedMilliseconds;
                    var response = httpClient.Send(request,
                        test.WaitForBody()
                            ? HttpCompletionOption.ResponseContentRead
                            : HttpCompletionOption.ResponseHeadersRead, token);
                    var timeTaken = stopwatch.ElapsedMilliseconds - startTime;
                    statistic.Success = test.IsSuccessful(response);
                    statistic.TimeTakenMilliseconds = (int) timeTaken;
                    statistic.StatusCode = (int) response.StatusCode;
                    globalStats.Push(statistic);

                    if (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(waitTime);
                    }
                }

                countdownEvent.Signal();
            }
            catch (Exception e)
            {
                // Intentionally left blank
            }
        }

        private static void Main(string[] args)
        {
            httpClient = new HttpClient();
            performanceTests = new List<PerformanceTest>();
            globalStats = new ConcurrentStack<Statistic>();
            threads = new Thread[users];
            countdownEvent = new CountdownEvent(users);

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
                        var thread = new Thread(() => DoRequests(ref token, ref test))
                        {
                            Priority = ThreadPriority.Lowest
                        };
                        threads[queued] = thread;
                        queued++;
                        thread.Start();
                    }

                    Console.WriteLine($"Spawned {queued} out of {users}");

                    if (queued == users)
                    {
                        Console.WriteLine("Finished spawning users");
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
            var finishedThreads = 0;

            foreach (var thread in threads)
            {
                if (thread != null && thread.Join(100))
                {
                    finishedThreads++;
                }
            }

            Console.WriteLine($"{finishedThreads} out of {countdownEvent.InitialCount} finished.");
            Console.WriteLine("Shut down.");

            var responseTimes = globalStats.Select(stat => (double) stat.TimeTakenMilliseconds).ToArray();
            Console.WriteLine($"Average Response time: {Math.Round(responseTimes.Average(), 0)}ms");
            Console.WriteLine($"95th Response time: {responseTimes.Percentile(0.95)}ms");
            Console.WriteLine($"99th Response time: {responseTimes.Percentile(0.99)}ms");
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