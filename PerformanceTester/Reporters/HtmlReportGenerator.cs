using System;
using System.Collections.Generic;
using System.IO;
using RazorEngineCore;

namespace PerformanceTester.Reporters
{
    public class HtmlReportGenerator : Reporter
    {
        private const string TEMPLATE = "report_template.html";
        private const string OUTPUT_FILE = "report.html";

        public override bool GenerateReport(ReportModel reportModel)
        {
            var templateContents = File.ReadAllText(TEMPLATE);
            IRazorEngine engine = new RazorEngine();
            IRazorEngineCompiledTemplate<RazorEngineTemplateBase<ReportModel>> compiledTemplate =
                engine.Compile<RazorEngineTemplateBase<ReportModel>>(templateContents,
                    builder =>
                    {
                        builder.AddAssemblyReference(typeof(Dictionary<string, Statistic[]>));
                        builder.AddAssemblyReferenceByName("System.Collections");
                        builder.AddAssemblyReference(typeof(Math));
                        builder.AddAssemblyReference(typeof(Util));
                        builder.AddUsing("System");
                    });
            string result = compiledTemplate.Run(instance => { instance.Model = reportModel; });

            if (File.Exists(OUTPUT_FILE))
            {
                File.Delete(OUTPUT_FILE);
            }

            File.WriteAllText(OUTPUT_FILE, result);

            return true;
        }
    }
}