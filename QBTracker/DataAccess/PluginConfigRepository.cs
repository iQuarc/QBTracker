using JsonSerializer = System.Text.Json.JsonSerializer;
using LiteDB;

namespace QBTracker.DataAccess
{
   public class PluginConfigRepository : Plugin.Contracts.IPluginConfigRepository
   {
      private readonly ILiteRepository db;
      private readonly int projectId;
      private readonly string pluginName;

      public PluginConfigRepository(ILiteRepository db, int projectId, string pluginName)
      {
         this.db = db;
         this.projectId = projectId;
         this.pluginName = pluginName;
      }

      public T? GetConfiguration<T>() where T : class, new()
      {
         var record = db.Query<PluginConfigRecord>("PluginConfigs")
            .Where(x => x.ProjectId == projectId && x.PluginName == pluginName)
            .FirstOrDefault();

         if (record?.ConfigJson == null)
            return null;

         return JsonSerializer.Deserialize<T>(record.ConfigJson);
      }

      public void SaveConfiguration<T>(T configuration) where T : class
      {
         var record = db.Query<PluginConfigRecord>("PluginConfigs")
            .Where(x => x.ProjectId == projectId && x.PluginName == pluginName)
            .FirstOrDefault();

         var json = JsonSerializer.Serialize(configuration);

         if (record != null)
         {
            record.ConfigJson = json;
            db.Update(record, "PluginConfigs");
         }
         else
         {
            record = new PluginConfigRecord
            {
               ProjectId = projectId,
               PluginName = pluginName,
               ConfigJson = json
            };
            db.Insert(record, "PluginConfigs");
         }
      }
   }

   public class PluginConfigRecord
   {
      public int Id { get; set; }
      public int ProjectId { get; set; }
      public string PluginName { get; set; } = string.Empty;
      public string ConfigJson { get; set; } = string.Empty;
   }
}
