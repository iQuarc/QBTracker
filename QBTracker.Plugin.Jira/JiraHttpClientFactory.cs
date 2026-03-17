using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace QBTracker.Plugin.Jira
{
   internal static class JiraHttpClientFactory
   {
      public static HttpClient Create(string jiraUrl, string email, string apiToken)
      {
         var client = new HttpClient();
         var baseUrl = jiraUrl.TrimEnd('/');
         client.BaseAddress = new Uri(baseUrl + "/");

         var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{apiToken}"));
         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

         return client;
      }
   }
}
