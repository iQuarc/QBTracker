namespace QBTracker.Plugin.Contracts
{
   public interface IPluginTaskProvider
   {
      string                                       Name { get; }
      object                                       GetConfigurationView(IPluginConfigRepository configRepository);
      Task<IEnumerable<(string Key, string Name)>> GetTasksAsync(IPluginConfigRepository        configRepository);
   }
}