using System;
using System.IO;
using System.Reflection;

namespace NTC.FamilyManager
{
    internal class AssemblyLoader : IDisposable
    {
        private static string ExecutingPath => Assembly.GetExecutingAssembly().Location;

        internal AssemblyLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadAssemblies;
        }

        private static Assembly LoadAssemblies(object sender, ResolveEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(ExecutingPath)) return null;

                // Lấy tên assembly đang yêu cầu (vd: MahApps.Metro.ALB)
                string requestedName = args.Name.Contains(",") 
                    ? args.Name.Substring(0, args.Name.IndexOf(",")) 
                    : args.Name;

                var assemblyDir = Path.GetDirectoryName(ExecutingPath);
                if (assemblyDir == null) return null;

                // Danh sách các thư mục cần tìm kiếm DLL
                string[] searchPaths = new string[]
                {
                    assemblyDir,
                    Path.Combine(assemblyDir, "Lib")
                };

                foreach (var path in searchPaths)
                {
                    if (!Directory.Exists(path)) continue;

                    string assemblyPath = Path.Combine(path, requestedName + ".dll");
                    if (File.Exists(assemblyPath))
                    {
                        // Kiểm tra xem assembly đã được load chưa để tránh loop
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.FullName.Contains(requestedName)) return assembly;
                        }

                        return Assembly.LoadFrom(assemblyPath);
                    }
                }
            }
            catch (Exception)
            {
                // Tránh throw exception trong Resolve event vì sẽ gây crash Revit
            }

            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= LoadAssemblies;
        }
    }
}