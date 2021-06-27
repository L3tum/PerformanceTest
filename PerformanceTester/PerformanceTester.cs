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
using PerformanceTester.Workers;

namespace PerformanceTester
{
    public class PerformanceTester
    {
        public const long SLEEP_IN_MS = 1000;

        private static HttpClient httpClient = null!;
        private readonly int numberOfThreads;
        private readonly Type[] performanceTests;
        private readonly int runTimeInSeconds;
        private readonly int spawnRate;
        private readonly string testToExecute;
        private readonly int users;
        private readonly bool waitForBody;
        private ConcurrentQueue<Statistic> globalStats = null!;
        private List<int> rps = null!;
        private IPerformanceTest test = null!;

        public PerformanceTester(int users, int spawnRate, int runTimeInSeconds, string testToExecute,
            string? fileToLoad, string host, int threads, int connections, bool waitForBody)
        {
            this.users = users;
            this.spawnRate = spawnRate;
            this.runTimeInSeconds = runTimeInSeconds;
            this.testToExecute = testToExecute;
            this.waitForBody = waitForBody;
            numberOfThreads = threads;
            performanceTests = TestLoader.LoadTests(fileToLoad);
            httpClient = new HttpClient(new SocketsHttpHandler
            {
                PreAuthenticate = false,
                UseCookies = false,
                UseProxy = false,
                MaxAutomaticRedirections = 50,
                MaxConnectionsPerServer = connections
            })
            {
                BaseAddress = new Uri(host)
            };
            ThreadPool.SetMaxThreads(int.MaxValue, int.MaxValue);
            ThreadPool.SetMinThreads(0, 128);
        }

        public PerformanceTester(Options options) : this(options.Users, options.SpawnRate, options.RunTimeInSeconds,
            options.Test, options.File, options.Host, options.Threads, options.Connections, options.WaitForBody)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [SkipLocalsInit]
        private void LaunchTest()
        {
            var oldCount = 0;
            var queued = 0;
            var runTimeInTicks = runTimeInSeconds * Stopwatch.Frequency;
            var workers = new Worker[numberOfThreads];
            var threads = new Thread[numberOfThreads];
            var started = false;

            Console.WriteLine("Starting up...");

            for (var i = 0; i < numberOfThreads; i++)
            {
                var worker = new TaskWorker();
                worker.SetHttpClient(ref httpClient);
                worker.SetPerformanceTest(ref test);
                worker.SetRequestsPerSecond(0);
                worker.SetHttpCompletionOption(waitForBody);
                workers[i] = worker;
                threads[i] = new Thread(worker.Launch);
            }

            var startTicks = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() - startTicks < runTimeInTicks)
            {
                if (queued < users)
                {
                    queued += spawnRate;

                    if (queued > users)
                    {
                        queued = users;
                    }

                    var queuedPerWorker = (int) Math.Round(queued / (double) workers.Length, 0,
                        MidpointRounding.ToPositiveInfinity);

                    foreach (var worker in workers)
                    {
                        worker.SetRequestsPerSecond(queuedPerWorker);
                    }

                    if (!started)
                    {
                        foreach (var thread in threads)
                        {
                            thread.Start();
                        }

                        started = true;
                    }

                    Console.Out.WriteLineAsync($"Spawned {queued} out of {users} threads");

                    if (queued == users)
                    {
                        Console.Out.WriteLineAsync($"Finished spawning {users} threads");
                    }
                }

                Thread.Sleep(1000);
                var newCount = workers.Select(worker => worker.GetCompletedRequests()).Sum();
                var diff = newCount - oldCount;
                rps.Add(diff);
                Console.Out.WriteLineAsync($"Executed {newCount} Requests | {diff} RPS | Target {queued} RPS");
                oldCount = newCount;
            }

            Console.WriteLine("Shutting down...");

            var stopTasks = new List<Task<Statistic[]>>(workers.Length);
            stopTasks.AddRange(workers.Select(worker => worker.Stop()));

            Task.WaitAll(stopTasks.ToArray());

            foreach (var statistic in stopTasks.SelectMany(stopTask => stopTask.Result))
            {
                globalStats.Enqueue(statistic);
            }
        }

        public void RunTest()
        {
            var performanceTest = performanceTests.FirstOrDefault(test => test.Name == testToExecute);

            if (performanceTest == null)
            {
                throw new KeyNotFoundException($"Test {testToExecute} could not be found!");
            }

            test = (IPerformanceTest) Activator.CreateInstance(performanceTest)!;

            var oldLatencyMode = GCSettings.LatencyMode;
            var oldThreadPriority = Thread.CurrentThread.Priority;
            GC.Collect();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            globalStats = new ConcurrentQueue<Statistic>();
            rps = new List<int>(Math.Min(5000, runTimeInSeconds));
            GC.Collect();

            LaunchTest();

            Thread.CurrentThread.Priority = oldThreadPriority;
            GCSettings.LatencyMode = oldLatencyMode;
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

        public List<int> GetRps()
        {
            return rps;
        }
    }
}