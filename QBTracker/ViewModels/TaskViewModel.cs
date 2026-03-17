using MaterialDesignThemes.Wpf;
using QBTracker.DataAccess;
using QBTracker.Util;
using QBTracker.Views;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using QBTracker.Plugin.Contracts;

#pragma warning disable CA1416

namespace QBTracker.ViewModels
{
   public class TaskViewModel : ValidatableModel
   {
      private readonly MainWindowViewModel                       mainVm;
      private          ObservableRangeCollection<TaskViewModel>? tasks;
      private          string?                                   importButtonText;
      private          bool                                      isImporting;

      public TaskViewModel(Task task, MainWindowViewModel mainWindowViewModel)
      {
         mainVm        = mainWindowViewModel;
         Save          = new RelayCommand(ExecuteSave, CanExecuteSave);
         Import        = new AsyncRelayCommand(ExecuteImport, CanExecuteImport);
         GoBack        = new RelayCommand(ExecuteGoBack);
         DeleteCommand = new AsyncRelayCommand(ExecuteDelete);
         Task          = task;
         ClearTouched();
         UpdateImportButtonText();
      }

      private IPluginTaskProvider? GetPluginForProject()
      {
         if (!Application.Current.Resources.Contains("PluginService"))
            return null;
         var pluginService = (PluginService)Application.Current.Resources["PluginService"];
         return pluginService.AvailablePlugins.FirstOrDefault();
      }

      private void UpdateImportButtonText()
      {
         var plugin = GetPluginForProject();
         ImportButtonText = plugin != null ? $"Import from {plugin.Name}" : "Import";
      }

      public string? ImportButtonText
      {
         get => importButtonText;
         private set
         {
            importButtonText = value;
            NotifyOfPropertyChange();
         }
      }

      private async ValueTask ExecuteImport(object? obj)
      {
         var plugin = GetPluginForProject();
         if (plugin == null) return;

         var pluginService = (PluginService)Application.Current.Resources["PluginService"];
         var configRepo = pluginService.CreateConfigRepository(Task.ProjectId, plugin.Name);

         isImporting = true;
         try
         {
            var taskNames = (await plugin.GetTasksAsync(configRepo)).ToList();
            if (taskNames.Count == 0)
            {
               await DialogHost.Show(new ConfirmDialog
               {
                  DataContext = "No tasks found."
               });
               return;
            }

            var existingTasks = mainVm.Repository.GetTasks(Task.ProjectId)
               .Select(t => t.Name)
               .ToHashSet();

            var newTaskNames = taskNames.Where(n => !existingTasks.Contains(n)).ToList();
            if (newTaskNames.Count == 0)
            {
               await DialogHost.Show(new ConfirmDialog
               {
                  DataContext = "All tasks already exist."
               });
               return;
            }

            var dialogVm = new ImportDialogViewModel($"Import from {plugin.Name}", newTaskNames);
            var result = await DialogHost.Show(new ImportDialog { DataContext = dialogVm });

            if (result is not true)
               return;

            var selected = dialogVm.SelectedNames.ToList();
            foreach (var taskName in selected)
            {
               var newTask = new Task
               {
                  Name = taskName,
                  ProjectId = Task.ProjectId
               };
               mainVm.Repository.AddTask(newTask);
               Tasks.Insert(0, new TaskViewModel(newTask, mainVm));
            }

            mainVm.LoadTasks();
         }
         catch (Exception ex)
         {
            await DialogHost.Show(new ConfirmDialog
            {
               DataContext = $"Import failed: {ex.Message}"
            });
         }
         finally
         {
            isImporting = false;
         }
      }

      private bool CanExecuteImport(object? arg)
      {
         return GetPluginForProject() != null && !isImporting;
      }


      public Task Task { get; }

      [Required]
      [DisplayName("Task Name")]
      [StringLength(400)]
      public string Name
      {
         get => Task.Name;
         set
         {
            Task.Name = value;
            NotifyOfPropertyChange();
         }
      }

      public bool IsFocused { get; set; } = true;

      public ObservableRangeCollection<TaskViewModel> Tasks
      {
         get
         {
            if (tasks == null)
            {
               tasks = new ObservableRangeCollection<TaskViewModel>();
               LoadTasks();
            }

            return tasks;
         }
      }

      public RelayCommand          Save     { get; }
      public AsyncRelayCommand     Import   { get; }
      public Action                OnSave   { get; set; }
      public Action<TaskViewModel> OnRemove { get; set; }

      private void ExecuteSave(object? o)
      {
         Validate();
         if (HasErrors)
            return;
         mainVm.Repository.AddTask(Task);
         mainVm.GoBack();
         OnSave?.Invoke();
      }

      private bool CanExecuteSave(object? o)
      {
         Validate();
         return !HasErrors;
      }

      public RelayCommand GoBack { get; }

      private void ExecuteGoBack(object? o)
      {
         mainVm.CreatedTask = null;
         mainVm.GoBack();
      }

      public AsyncRelayCommand DeleteCommand { get; }

      private async ValueTask ExecuteDelete(object? o)
      {
         if ((bool?)await DialogHost.Show(new ConfirmDialog
             {
                DataContext = "Are you sure?"
             }) == true)
         {
            this.Task.IsDeleted = true;
            this.mainVm.Repository.UpdateTask(this.Task);
            this.mainVm.Tasks.Remove(this);
            this.mainVm.CreatedTask.Tasks.Remove(this);
            this.OnRemove?.Invoke(this);
         }
      }

      private void LoadTasks()
      {
         Tasks.Clear();
         Tasks.AddRange(mainVm.Repository.GetTasks(this.Task.ProjectId)
            .Select(x => new TaskViewModel(x, this.mainVm)));
      }
   }
}