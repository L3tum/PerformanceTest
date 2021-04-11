using System.Linq;
using CommandLine;

namespace PerformanceTester
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args)!;
            if (options.Errors.Any())
            {
                return;
            }

            var performanceTester = new PerformanceTester(options.Value);
            performanceTester.RunTest();
            performanceTester.PrintResults();
        }
    }
}