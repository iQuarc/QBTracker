using System.Reflection;
using System.Runtime.Loader;

namespace QBTracker.DataAccess
{
   internal class PluginLoadContext : AssemblyLoadContext
   {
      private readonly AssemblyDependencyResolver resolver;

      public PluginLoadContext(string pluginPath)
      {
         resolver = new AssemblyDependencyResolver(pluginPath);
      }

      protected override Assembly? Load(AssemblyName assemblyName)
      {
         // Let shared assemblies (like Plugin.Contracts) resolve from the default context
         var defaultAssembly = Default.Assemblies
            .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
         if (defaultAssembly != null)
            return defaultAssembly;

         var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
         return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
      }

      protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
      {
         var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
         return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
      }
   }
}
