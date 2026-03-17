namespace QBTracker.Plugin.Jira
{
   public class JiraConfig
   {
      public string JiraUrl { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string ApiToken { get; set; } = string.Empty;
      public string ProjectKey { get; set; } = string.Empty;
      public string StatusFilter { get; set; } = string.Empty;
   }
}
