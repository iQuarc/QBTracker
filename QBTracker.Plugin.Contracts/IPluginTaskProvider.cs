namespace QBTracker.Plugin.Contracts
{
   public interface IPluginTaskProvider
   {
      string Name { get; }
      object GetConfigurationView(IPluginConfigRepository configRepository);
      Task<IEnumerable<string>> GetTasksAsync(IPluginConfigRepository configRepository);
   }
}