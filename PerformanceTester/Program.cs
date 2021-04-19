using System;
using System.Globalization;
using System.Linq;
using CommandLine;

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

            var performanceTester = new PerformanceTester(options);
            var startTime = DateTime.Now;
            performanceTester.RunTest();
            var endTime = DateTime.Now;
            performanceTester.PrintResults();
            HtmlReportGenerator.GenerateReport(
                new ReportModel(
                    endTime.ToString("o", CultureInfo.InvariantCulture),
                    options.RunTimeInSeconds,
                    options.SpawnRate, startTime.ToString("o", CultureInfo.InvariantCulture),
                    performanceTester.GetStatistics(),
                    options.Test,
                    options.Users,
                    options.Host,
                    performanceTester.GetRps()
                )
            );
        }
    }
}