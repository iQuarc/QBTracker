using System.Net.Http;
using System.Text;
using System.Text.Json;
using QBTracker.Plugin.Contracts;

namespace QBTracker.Plugin.Jira
{
   public class JiraTaskProvider : IPluginTaskProvider
   {
      public string Name => "Jira";

      public object GetConfigurationView(IPluginConfigRepository configRepository)
      {
         var viewModel = new JiraConfigViewModel(configRepository);
         return new JiraConfigView { DataContext = viewModel };
      }

      public async Task<IEnumerable<(string Key, string Name)>> GetTasksAsync(IPluginConfigRepository configRepository)
      {
         var config = configRepository.GetConfiguration<JiraConfig>();
         if (config == null || string.IsNullOrWhiteSpace(config.JiraUrl)
                            || string.IsNullOrWhiteSpace(config.Email)
                            || string.IsNullOrWhiteSpace(config.ApiToken)
                            || string.IsNullOrWhiteSpace(config.ProjectKey))
         {
            return Enumerable.Empty<(string, string)>();
         }

         using var client = JiraHttpClientFactory.Create(config.JiraUrl, config.Email, config.ApiToken);

         var jql = $"project = {config.ProjectKey} AND assignee = currentUser()";
         if (!string.IsNullOrWhiteSpace(config.StatusFilter))
         {
            var statuses = config.StatusFilter
               .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(s => $"\"{s}\"");
            jql += $" AND status IN ({string.Join(", ", statuses)})";
         }

         jql += " ORDER BY created DESC";
         var url = $"/rest/api/3/search/jql";

         var requestBody = new
         {
            jql        = jql,
            maxResults = 50, // Adjust as needed; for more results, implement pagination
            fields     = new[] { "key", "summary", "status", "assignee" }
         };
         var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
         var response    = await client.PostAsync(url, jsonContent);
         var json        = await response.Content.ReadAsStringAsync();
         response.EnsureSuccessStatusCode();
         using var doc = JsonDocument.Parse(json);

         var tasks = new List<(string Key, string Name)>();
         foreach (var issue in doc.RootElement.GetProperty("issues").EnumerateArray())
         {
            var key     = issue.GetProperty("key").GetString();
            var summary = issue.GetProperty("fields").GetProperty("summary").GetString();
            tasks.Add((key! ,summary!));
         }

         return tasks;
      }
   }
}