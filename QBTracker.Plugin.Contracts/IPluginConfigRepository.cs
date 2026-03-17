namespace QBTracker.Plugin.Contracts
{
    public interface IPluginConfigRepository
    {
        T? GetConfiguration<T>() where T : class, new();
        void SaveConfiguration<T>(T configuration) where T : class;
    }
}
