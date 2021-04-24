using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
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
                    ".cs" => LoadCSScript(fileInfo.FullName),
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

        private static Type[] LoadCSScript(string file)
        {
            var text = File.ReadAllText(file);

            // define source code, then parse it (to the type used for compilation)
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(text);

            // define other necessary objects for compilation
            string assemblyName = Path.GetRandomFileName();
            List<MetadataReference> references = new()
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IPerformanceTest).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(HttpRequestMessage).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GCLatencyMode).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.GetEntryAssembly()!.Location)
            };
            Assembly.GetEntryAssembly()!.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            // analyse and generate IL code from syntax tree
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] {syntaxTree},
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            // write IL code into memory
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                // handle exceptions
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
            }
            else
            {
                // load this 'virtual' DLL so that we can use
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                return (from type in assembly.GetTypes()
                    where type.BaseType == typeof(IPerformanceTest)
                    select type).ToArray();
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