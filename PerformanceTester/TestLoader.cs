using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PerformanceTest;

namespace PerformanceTester
{
    public static class TestLoader
    {
        public static IPerformanceTest[] LoadTests(string? file)
        {
            if (file != null)
            {
                return LoadFile(file);
            }

            var performanceTests = new List<IPerformanceTest>();
            var files = LoadFiles();

            foreach (var fileInfo in files)
            {
                var test = fileInfo.Extension.ToLowerInvariant() switch
                {
                    ".dll" => LoadDll(fileInfo.FullName),
                    // ".py" => LoadPython(),
                    // ".lua" => LoadLua(),
                    // ".rb" => LoadRuby(),
                    _ => null
                };

                if (test != null)
                {
                    performanceTests.AddRange(test);
                }
            }

            return performanceTests.ToArray();
        }

        private static IPerformanceTest[] LoadDll(string file)
        {
            try
            {
                var assembly = Assembly.LoadFile(file);
                var tests = (from type in assembly.GetTypes()
                    where type.BaseType == typeof(IPerformanceTest)
                    select Activator.CreateInstance(type) as IPerformanceTest ??
                           throw new InvalidOperationException(type.FullName)).ToList();
                return tests.ToArray();
            }
            catch
            {
                // Intentionally left blank
            }

            return Array.Empty<IPerformanceTest>();
        }

        private static IPerformanceTest[] LoadFile(string file)
        {
            return Array.Empty<IPerformanceTest>();
        }

        private static FileInfo[] LoadFiles()
        {
            var path = new DirectoryInfo(Environment.CurrentDirectory);
            var files = path.GetFiles("*", SearchOption.AllDirectories);

            return files;
        }
    }
}