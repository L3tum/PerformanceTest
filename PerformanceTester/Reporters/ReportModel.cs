using System.Collections.Generic;

namespace PerformanceTester.Reporters
{
    public record ReportModel(string EndTime,
        int RunTimeInSeconds,
        int SpawnRate,
        string StartTime,
        Dictionary<string, List<Statistic>> Statistics,
        string Test,
        int Users,
        string Host,
        List<int> RequestsPerSeconds
    );
}