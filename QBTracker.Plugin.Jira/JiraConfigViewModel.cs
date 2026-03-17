using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using QBTracker.Plugin.Contracts;

namespace QBTracker.Plugin.Jira
{
   public class JiraConfigViewModel : INotifyPropertyChanged
   {
      private readonly IPluginConfigRepository configRepository;
      private readonly JiraConfig config;
      private string? statusMessage;
      private bool isTesting;

      public JiraConfigViewModel(IPluginConfigRepository configRepository)
      {
         this.configRepository = configRepository;
         config = configRepository.GetConfiguration<JiraConfig>() ?? new JiraConfig();
         SaveCommand = new JiraRelayCommand(ExecuteSave);
         TestConnectionCommand = new JiraRelayCommand(ExecuteTestConnection);
      }

      public string JiraUrl
      {
         get => config.JiraUrl;
         set { config.JiraUrl = value; OnPropertyChanged(); }
      }

      public string Email
      {
         get => config.Email;
         set { config.Email = value; OnPropertyChanged(); }
      }

      public string ApiToken
      {
         get => config.ApiToken;
         set { config.ApiToken = value; OnPropertyChanged(); }
      }

      public string ProjectKey
      {
         get => config.ProjectKey;
         set { config.ProjectKey = value; OnPropertyChanged(); }
      }

      public string StatusFilter
      {
         get => config.StatusFilter;
         set { config.StatusFilter = value; OnPropertyChanged(); }
      }

      public string? StatusMessage
      {
         get => statusMessage;
         set { statusMessage = value; OnPropertyChanged(); }
      }

      public bool IsNotTesting => !isTesting;

      public bool IsTesting
      {
         get => isTesting;
         set { isTesting = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotTesting)); }
      }

      public ICommand SaveCommand { get; }
      public ICommand TestConnectionCommand { get; }

      private void ExecuteSave(object? _)
      {
         configRepository.SaveConfiguration(config);
         StatusMessage = "Configuration saved.";
      }

      private async void ExecuteTestConnection(object? _)
      {
         if (string.IsNullOrWhiteSpace(JiraUrl) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(ApiToken))
         {
            StatusMessage = "Please fill in URL, Email, and API Token.";
            return;
         }

         IsTesting = true;
         StatusMessage = "Testing connection...";

         try
         {
            using var client = JiraHttpClientFactory.Create(JiraUrl, Email, ApiToken);
            var response = await client.GetAsync("rest/api/2/myself");

            StatusMessage = response.IsSuccessStatusCode
               ? "Connection successful!"
               : $"Connection failed: {response.StatusCode}";
         }
         catch (Exception ex)
         {
            StatusMessage = $"Connection failed: {ex.Message}";
         }
         finally
         {
            IsTesting = false;
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
