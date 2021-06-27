using CommandLine;

namespace PerformanceTester
{
    public class Options
    {
        [Option('u', "users", Required = true, HelpText = "Number of users to simulate.")]
        public int Users { get; set; }

        [Option('r', "spawn-rate", Required = true, HelpText = "Users per second to spawn.")]
        public int SpawnRate { get; set; }

        [Option('s', "seconds", Required = true, HelpText = "Run time in seconds to execute load test for.")]
        public int RunTimeInSeconds { get; set; }

        [Option('f', "file", Required = false, Default = null,
            HelpText = "File to load for test. Normally PerformanceTester loads all files into memory first.")]
        public string? File { get; set; }

        [Option('t', "test", Required = true, HelpText = "Test to execute. ClassName for C# Scripts for example.")]
        public string Test { get; set; } = null!;

        [Option('h', "host", Required = true, HelpText = "Host to test against")]
        public string Host { get; set; } = null!;

        [Option("html", Default = true, Required = false,
            HelpText = "Generate a HTML report and save it to 'report.html'")]
        public bool HtmlReport { get; set; }

        [Option("threads", Default = 0, Required = false,
            HelpText = "Number of workers to spawn/expect. Default is number of logical threads on this machine.")]
        public int Threads { get; set; }

        [Option('c', "connections", Default = int.MaxValue, Required = false,
            HelpText = "Number of connections to establish with the server")]
        public int Connections { get; set; }

        [Option('b', "wait-for-body", Default = true, Required = false,
            HelpText =
                "Configures the Client to wait for the whole body to be received. This may be important if you use a streaming approach that sends headers first instead of buffering the output (for example, PHP buffers the output by default).")]
        public bool WaitForBody { get; set; }
    }
}