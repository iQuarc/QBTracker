namespace QBTracker.Plugin.Contracts
{
    internal interface IPluginConfigRepository
    {
        object GetProjectConfiguration();
        void SaveProjectConfiguration(object configurationObject);
    }
}
