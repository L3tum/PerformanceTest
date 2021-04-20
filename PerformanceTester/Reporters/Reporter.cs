namespace PerformanceTester.Reporters
{
    public abstract class Reporter
    {
        public abstract bool GenerateReport(ReportModel reportModel);
    }
}