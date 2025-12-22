using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace NTC.FamilyManager
{
    internal class AssemblyLoader : IDisposable
    {
        private static string ExecutingPath => Assembly.GetExecutingAssembly().Location;

        internal AssemblyLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadAssemblies;
            ForceLoadAssemblies();
        }

        /// <summary>
        /// Nạp cưỡng bức các thư viện hay bị xung đột trong Revit
        /// </summary>
        private void ForceLoadAssemblies()
        {
            // Danh sách các assembly hay gây lỗi type mismatch trong Revit
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
                        // Nạp bằng byte array để tránh bị khóa file và "đè" lên các phiên bản đã nạp bởi host
                        Assembly.Load(File.ReadAllBytes(path));
                    }
                }
                catch { /* Ignore if fails */ }
            }
        }

        private static Assembly LoadAssemblies(object sender, ResolveEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(ExecutingPath)) return null;

                // Lấy thông tin assembly yêu cầu
                AssemblyName requestedAssemblyName = new AssemblyName(args.Name);
                string requestedName = requestedAssemblyName.Name;

                // Tránh loop vô tận cho các thư viện hệ thống cơ bản
                if (requestedName == "mscorlib" || requestedName == "System" || requestedName == "System.Core")
                    return null;

                var assemblyDir = Path.GetDirectoryName(ExecutingPath);
                if (assemblyDir == null) return null;

                // Ưu tiên các thư viện hệ thống hay bị Revit nạp sẵn bản cũ
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
                        // Nạp bằng byte array để ghi đè (Override)
                        return Assembly.Load(File.ReadAllBytes(path));
                    }
                }

                // Tìm trong thư mục root hoặc Lib cho các assembly khác
                string assemblyPath = Path.Combine(assemblyDir, requestedName + ".dll");
                if (!File.Exists(assemblyPath))
                    assemblyPath = Path.Combine(assemblyDir, "Lib", requestedName + ".dll");

                if (File.Exists(assemblyPath))
                {
                    // Kiểm tra phiên bản nếu đã được nạp
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