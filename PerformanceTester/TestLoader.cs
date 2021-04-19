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
        public static Type[] LoadTests(string? file)
        {
            if (file != null)
            {
                return LoadFile(file);
            }

            var performanceTests = new List<Type>();
            var files = LoadFiles();

            foreach (var fileInfo in files)
            {
                var tests = fileInfo.Extension.ToLowerInvariant() switch
                {
                    ".dll" => LoadDll(fileInfo.FullName),
                    // ".py" => LoadPython(),
                    // ".lua" => LoadLua(),
                    // ".rb" => LoadRuby(),
                    _ => null
                };

                if (tests != null)
                {
                    performanceTests.AddRange(tests);
                }
            }

            return performanceTests.ToArray();
        }

        private static Type[] LoadDll(string file)
        {
            try
            {
                var assembly = Assembly.LoadFile(file);
                return (from type in assembly.GetTypes()
                    where type.BaseType == typeof(IPerformanceTest)
                    select type).ToArray();
            }
            catch
            {
                // Intentionally left blank
            }

            return Array.Empty<Type>();
        }

        private static Type[] LoadFile(string file)
        {
            return Array.Empty<Type>();
        }

        private static FileInfo[] LoadFiles()
        {
            var path = new DirectoryInfo(Environment.CurrentDirectory);
            var files = path.GetFiles("*", SearchOption.AllDirectories);

            return files;
        }
    }
}