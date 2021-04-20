using System;
using System.Globalization;
using System.Linq;
using CommandLine;
using PerformanceTester.Reporters;

namespace PerformanceTester
{
    internal static class Program
    {
        private static readonly Reporter[] Reporters =
        {
            new HtmlReportGenerator(),
            new ConsoleReportGenerator()
        };

        private static void Main(string[] args)
        {
            var optionsResult = Parser.Default.ParseArguments<Options>(args)!;
            if (optionsResult.Errors.Any())
            {
                return;
            }

            var options = optionsResult.Value;

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

            foreach (var reporter in Reporters)
            {
                reporter.GenerateReport(reportModel);
            }
        }
    }
}