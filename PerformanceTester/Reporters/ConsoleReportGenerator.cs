using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceTester.Reporters
{
    public class ConsoleReportGenerator : Reporter
    {
        public override bool GenerateReport(ReportModel reportModel)
        {
            var responseTimes = new List<double>();

            foreach (var stats in reportModel.Statistics.Values)
            {
                responseTimes.AddRange(stats.Select(stat => (double) stat.TimeTakenMilliseconds));
            }

            Console.WriteLine();
            Console.WriteLine($"Min Response time: {responseTimes.Min()}ms");
            Console.WriteLine($"Average Response time: {Math.Round(responseTimes.Average(), 0)}ms");
            Console.WriteLine($"90th Response time: {responseTimes.Percentile(0.90)}ms");
            Console.WriteLine($"95th Response time: {responseTimes.Percentile(0.95)}ms");
            Console.WriteLine($"99th Response time: {responseTimes.Percentile(0.99)}ms");
            Console.WriteLine($"Max Response time: {responseTimes.Max()}ms");
            Console.WriteLine($"Average RPS: {Math.Round(reportModel.RequestsPerSeconds.Average(), 0)}");
            Console.WriteLine($"Requests: {reportModel.Statistics.Count}");
            Console.WriteLine(
                $"Successful Requests {reportModel.Statistics.Values.Select(stat => stat.Count(sta => sta.Success)).Aggregate((x, y) => x + y)}");
            Console.WriteLine(
                $"Failed Requests {reportModel.Statistics.Values.Select(stat => stat.Count(sta => !sta.Success)).Aggregate((x, y) => x + y)}");

            return true;
        }
    }
}