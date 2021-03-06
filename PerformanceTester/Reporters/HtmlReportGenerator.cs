using System;
using System.Collections.Generic;
using System.IO;
using RazorEngineCore;

namespace PerformanceTester.Reporters
{
    public class HtmlReportGenerator : Reporter
    {
        private const string OUTPUT_FILE = "report.html";

        #region TEMPLATE

        private const string TEMPLATE_STRING = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <meta name=""description"" content="""">
    <meta name=""author"" content="""">
    <title>L3tum's PerformanceTester Dashboard</title>
    <style>
        * {
            box-sizing: border-box;
        }

        .container {
            display: flex;
            flex-wrap: wrap;
            margin: 5% 10%;
            border: 1px solid black;
            justify-content: center;
        }

        h3 {
            background-color: green;
            text-align: center;
            padding: 10px;
            margin: 0;
            width: 100%;
            color: white
        }

        table {
            border-collapse: collapse;
            align-self: center;
            width: 90%;
            margin: 1%;
        }

        table, td, th {
            border: 1px solid black;
        }

        td:first-child {
            font-weight: bold;
        }

        td {
            padding: 1px;
        }

        .pie {
            align-self: center;
            width: 200px;
            height: 200px;
            border-radius: 50%;
            margin: 20px;
        }

        .statistics td {
            padding-left: 10px;
        }
    </style>
</head>
<body>
<div class=""container"">
    <h3>Test Information</h3>
    <table>
        <tr>
            <td>
                Test
            </td>
            <td>
                @Model.Test
            </td>
        </tr>
        <tr>
            <td>
                Users
            </td>
            <td>
                @Model.Users
            </td>
        </tr>
        <tr>
            <td>
                Spawn Rate
            </td>
            <td>
                @Model.SpawnRate
            </td>
        </tr>
        <tr>
            <td>
                Runtime
            </td>
            <td>
                @Model.RunTimeInSeconds
            </td>
        </tr>
        <tr>
            <td>
                Host
            </td>
            <td>
                @Model.Host
            </td>
        </tr>       
        <tr>
            <td>
                Average RPS
            </td>
            <td>
                @Math.Round(Model.RequestsPerSeconds.Average(), 0)
            </td>
        </tr>
        <tr>
            <td>
                Start Time
            </td>
            <td>
                @Model.StartTime
            </td>
        </tr>
        <tr>
            <td>
                End Time
            </td>
            <td>
                @Model.EndTime
            </td>
        </tr>
    </table>
</div>

<div class=""container"">
    <h3>Request Summary</h3>
    <div class=""pie""
         style=""background: conic-gradient(#ff0000 @Model.Statistics.Values.Select(stat => stat.Count(sta => !sta.Success)).Aggregate((x, y) => x + y)%, yellowgreen 0);""></div>
    <div style=""width: 50px; height: 20px; margin-left: 10px;"">
        <p>
            <span style=""background: red; display: inline-block; width: 20px; height: 20px;"">  </span>
            KO
        </p>
        <p>
            <span style=""background: yellowgreen; display: inline-block; width: 20px; height: 20px;"">  </span>
            OK
        </p>
    </div>
</div>
<div class=""container"">
    <h3>Statistics</h3>
    <table class=""statistics"">
        <thead>
        <tr>
            <th>
                Method
            </th>
            <th>
                Name
            </th>
            <th>
                # Requests
            </th>
            <th>
                # Fails
            </th>
            <th>
                Min (ms)
            </th>
            <th>
                Average (ms)
            </th>
            <th>
                90th Percentile (ms)
            </th> 
            <th>
                95th Percentile (ms)
            </th>
            <th>
                99th Percentile (ms)
            </th>
            <th>
                Max (ms)
            </th>
        </tr>
        </thead>
        <tbody>
        @foreach(var kvp in Model.Statistics) {
        <tr>
            <td>
                @kvp.Key.Split(':')[0]
            </td>
            <td>
                @kvp.Key.Split(':')[1]
            </td>
            <td>
                @kvp.Value.Count
            </td>
            <td>
                @kvp.Value.Count(stat => !stat.Success)
            </td>
            <td>
                @Math.Round(kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds).Min(), 0)
            </td>
            <td>
                @Math.Round(kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds).Average(), 0)
            </td>
            <td>
                @Math.Round(PerformanceTester.Util.GetPercentile(kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds), 0.90), 0)
            </td>            
            <td>
                @Math.Round(PerformanceTester.Util.GetPercentile(kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds), 0.95), 0)
            </td>
            <td>
                @Math.Round(PerformanceTester.Util.GetPercentile(kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds), 0.99), 0)
            </td>
            <td>
                @kvp.Value.Select(stat => (double) stat.TimeTakenMilliseconds).Max()
            </td>
        </tr>
        }
        </tbody>
    </table>
</div>
</body>
</html>";

        #endregion

        public override bool GenerateReport(ReportModel reportModel)
        {
            IRazorEngine engine = new RazorEngine();
            IRazorEngineCompiledTemplate<RazorEngineTemplateBase<ReportModel>> compiledTemplate =
                engine.Compile<RazorEngineTemplateBase<ReportModel>>(TEMPLATE_STRING,
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