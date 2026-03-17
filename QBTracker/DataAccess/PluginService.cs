using System.IO;
using System.Reflection;
using System.Windows;
using QBTracker.Plugin.Contracts;

namespace QBTracker.DataAccess
{
   public class PluginService
   {
      private readonly List<IPluginTaskProvider> plugins = new();
      private readonly IRepository repository;

      public PluginService()
      {
         this.repository = (IRepository)Application.Current.Resources["Repository"];
         DiscoverPlugins();
      }

      public IReadOnlyList<IPluginTaskProvider> AvailablePlugins => plugins;

      public IPluginConfigRepository CreateConfigRepository(int projectId, string pluginName)
      {
         return new PluginConfigRepository(repository.GetLiteRepository(), projectId, pluginName);
      }

      private void DiscoverPlugins()
      {
         var pluginsDir = AppDomain.CurrentDomain.BaseDirectory;

         foreach (var dll in Directory.GetFiles(pluginsDir, "QBTracker.Plugin.*.dll"))
         {
            try
            {
               var assembly = LoadPluginAssembly(dll);
               var providerTypes = assembly.GetTypes()
                  .Where(t => typeof(IPluginTaskProvider).IsAssignableFrom(t)
                              && t is { IsInterface: false, IsAbstract: false });

               foreach (var type in providerTypes)
               {
                  if (Activator.CreateInstance(type) is IPluginTaskProvider provider)
                     plugins.Add(provider);
               }
            }
            catch
            {
               // Skip assemblies that fail to load
            }
         }
      }

      private static Assembly LoadPluginAssembly(string path)
      {
         var context = new PluginLoadContext(path);
         return context.LoadFromAssemblyName(
            new AssemblyName(Path.GetFileNameWithoutExtension(path)));
      }
   }
}
