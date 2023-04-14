using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Common.Composition
{
    public class AssemblyLoader
    {
        static AssemblyLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ContainerResolveEventHandler);
            RegisterSQLiteResolver();
        }

        public static IList<Assembly> Load(IList<string> assemblyNames)
        {
            var toLoad = assemblyNames.ToList();
            toLoad.Add("Prowlarr.Common");
            toLoad.Add(OsInfo.IsWindows ? "Prowlarr.Windows" : "Prowlarr.Mono");

            var startupPath = AppDomain.CurrentDomain.BaseDirectory;

            return toLoad
                .Select(x => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(startupPath, $"{x}.dll")))
                .ToList();
        }

        private static Assembly ContainerResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var resolver = new AssemblyDependencyResolver(args.RequestingAssembly.Location);
            var assemblyPath = resolver.ResolveAssemblyToPath(new AssemblyName(args.Name));

            if (assemblyPath == null)
            {
                return null;
            }

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }

        public static void RegisterSQLiteResolver()
        {
            // This ensures we look for sqlite3 using libsqlite3.so.0 on Linux and not libsqlite3.so which
            // is less likely to exist.
            var sqliteAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.Data.SQLite.dll"));

            try
            {
                NativeLibrary.SetDllImportResolver(sqliteAssembly, LoadSqliteNativeLib);
            }
            catch (InvalidOperationException)
            {
                // This can only be set once per assembly
                // Catch required for NzbDrone.Host tests
            }
        }

        private static IntPtr LoadSqliteNativeLib(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            var mappedName = OsInfo.IsLinux && libraryName == "sqlite3" ? "libsqlite3.so.0" : libraryName;
            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }
    }
}
