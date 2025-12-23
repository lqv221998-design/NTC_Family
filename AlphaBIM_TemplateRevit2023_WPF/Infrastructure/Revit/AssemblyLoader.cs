using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace NTC.FamilyManager.Infrastructure.Revit
{
    internal class AssemblyLoader : IDisposable
    {
        private static string ExecutingPath => Assembly.GetExecutingAssembly().Location;

        internal AssemblyLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadAssemblies;
            ForceLoadAssemblies();
        }

        private void ForceLoadAssemblies()
        {
            string[] criticalAssemblies = { 
                "System.Diagnostics.DiagnosticSource", 
                "System.Runtime.CompilerServices.Unsafe",
                "Microsoft.Bcl.AsyncInterfaces"
            };

            var assemblyDir = Path.GetDirectoryName(ExecutingPath);
            if (assemblyDir == null) return;

            foreach (var name in criticalAssemblies)
            {
                try
                {
                    string path = Path.Combine(assemblyDir, name + ".dll");
                    if (File.Exists(path))
                    {
                        Assembly.Load(File.ReadAllBytes(path));
                    }
                }
                catch { }
            }
        }

        private static Assembly LoadAssemblies(object sender, ResolveEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(ExecutingPath)) return null;

                AssemblyName requestedAssemblyName = new AssemblyName(args.Name);
                string requestedName = requestedAssemblyName.Name;

                if (requestedName == "mscorlib" || requestedName == "System" || requestedName == "System.Core")
                    return null;

                var assemblyDir = Path.GetDirectoryName(ExecutingPath);
                if (assemblyDir == null) return null;

                string[] forceOverwrite = { 
                    "System.Diagnostics.DiagnosticSource", 
                    "System.Runtime.CompilerServices.Unsafe",
                    "System.Text.Json",
                    "Microsoft.Bcl.AsyncInterfaces"
                };

                if (forceOverwrite.Any(name => requestedName.StartsWith(name)))
                {
                    string path = Path.Combine(assemblyDir, requestedName + ".dll");
                    if (!File.Exists(path)) path = Path.Combine(assemblyDir, "Lib", requestedName + ".dll");
                    
                    if (File.Exists(path)) 
                    {
                        return Assembly.Load(File.ReadAllBytes(path));
                    }
                }

                string assemblyPath = Path.Combine(assemblyDir, requestedName + ".dll");
                if (!File.Exists(assemblyPath))
                    assemblyPath = Path.Combine(assemblyDir, "Lib", requestedName + ".dll");

                if (File.Exists(assemblyPath))
                {
                    var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == requestedName);

                    if (loadedAssembly != null)
                    {
                        if (loadedAssembly.GetName().Version >= requestedAssemblyName.Version)
                            return loadedAssembly;
                    }

                    return Assembly.LoadFrom(assemblyPath);
                }
            }
            catch { }

            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= LoadAssemblies;
        }
    }
}
