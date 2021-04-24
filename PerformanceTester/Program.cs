using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommandLine;
using PerformanceTester.Reporters;

namespace PerformanceTester
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var optionsResult = Parser.Default.ParseArguments<Options>(args)!;
            if (optionsResult.Errors.Any())
            {
                return;
            }

            var options = optionsResult.Value;

            if (options.Threads == 0)
            {
                options.Threads = Environment.ProcessorCount;
            }

            var performanceTester = new PerformanceTester(options);
            var startTime = DateTime.Now;
            performanceTester.RunTest();
            var endTime = DateTime.Now;

            var reportModel = new ReportModel(
                endTime.ToString("o", CultureInfo.InvariantCulture),
                options.RunTimeInSeconds,
                options.SpawnRate,
                startTime.ToString("o", CultureInfo.InvariantCulture),
                performanceTester.GetStatistics(),
                options.Test,
                options.Users,
                options.Host,
                performanceTester.GetRps()
            );

            if (reportModel.Statistics.Count == 0)
            {
                reportModel.Statistics.Add("no tests executed", new List<Statistic>
                {
                    new()
                    {
                        RequestMethod = "GET", RequestUri = "/", StatusCode = 415, Success = false,
                        TimeTakenMilliseconds = 0
                    }
                });
            }

            new ConsoleReportGenerator().GenerateReport(reportModel);

            if (options.HtmlReport)
            {
                new HtmlReportGenerator().GenerateReport(reportModel);
            }
        }
    }
}