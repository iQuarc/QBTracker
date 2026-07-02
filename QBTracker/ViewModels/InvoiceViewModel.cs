using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using QBTracker.Invoice;
using QBTracker.Model;
using QBTracker.Util;
using QBTracker.Views;

namespace QBTracker.ViewModels
{
   public class InvoiceViewModel : ValidatableModel
   {
      private readonly MainWindowViewModel mainVm;
      private          DateTime            endDate;
      private          DateTime            invoiceDate;
      private          string              invoiceNumber = string.Empty;
      private          DateTime            startDate;

      public InvoiceViewModel(MainWindowViewModel mainVm)
      {
         this.mainVm       = mainVm;
         InvoiceSettings   = mainVm.Repository.GetSettings();
         GoBack            = new RelayCommand(_ => mainVm.GoBack());
         OpenConfiguration = new RelayCommand(_ => mainVm.SelectedTransitionIndex = Pages.InvoiceConfiguration);
         GenerateInvoice   = new RelayCommand(ExecuteGenerateInvoice, _ => CanGenerateInvoice());

         var (defaultStartDate, defaultEndDate) = GetDefaultPeriod();
         startDate                              = defaultStartDate;
         endDate                                = defaultEndDate;
         invoiceDate                            = defaultEndDate;
         invoiceNumber                          = InvoiceSettings.NextInvoiceNumber.ToString();
      }

      public ObservableRangeCollection<InvoiceProject> InvoiceProjects { get; } = new();

      public Settings InvoiceSettings { get; }

      public RelayCommand GoBack { get; }

      public RelayCommand OpenConfiguration { get; }

      public RelayCommand GenerateInvoice { get; }

      public string ProjectsTitle => $"Projects {InvoiceProjects.Count(x => x.IsSelected)}/{InvoiceProjects.Count}";

      public DateTime StartDate
      {
         get => startDate;
         set
         {
            startDate = value.Date;
            if (endDate < startDate)
               EndDate = startDate;
            NotifyOfPropertyChange();
            ReloadProjects();
         }
      }

      public DateTime EndDate
      {
         get => endDate;
         set
         {
            endDate = value.Date;
            NotifyOfPropertyChange();
            ReloadProjects();
         }
      }

      public DateTime InvoiceDate
      {
         get => invoiceDate;
         set
         {
            invoiceDate = value.Date;
            NotifyOfPropertyChange();
         }
      }

      public string InvoiceNumber
      {
         get => invoiceNumber;
         set
         {
            invoiceNumber = value;
            NotifyOfPropertyChange();
            CommandManager.InvalidateRequerySuggested();
         }
      }

      public void Activated()
      {
         InvoiceNumber = InvoiceSettings.NextInvoiceNumber.ToString();
         InvoiceDate   = DateTime.Today;
         ReloadProjects();
      }

      private bool CanGenerateInvoice()
      {
         return !string.IsNullOrWhiteSpace(InvoiceNumber)
                && InvoiceProjects.Any(x => x.IsSelected && x.Hours > 0);
      }

      private void ExecuteGenerateInvoice(object? _)
      {
         if (!int.TryParse(InvoiceNumber, out var parsedInvoiceNumber) || parsedInvoiceNumber <= 0)
         {
            _ = DialogHost.Show(new ErrorDialog { DataContext = "Invoice number must be a positive whole number." });
            return;
         }

         var selectedProjects = InvoiceProjects
            .Where(x => x.IsSelected && x.Hours > 0)
            .ToList();

         if (selectedProjects.Count == 0)
         {
            _ = DialogHost.Show(new ErrorDialog { DataContext = "Select at least one project with recorded time." });
            return;
         }

         try
         {
            var saveFileDialog = new SaveFileDialog
            {
               Filter = "Excel Files | *.xlsx",
               InitialDirectory = InvoiceSettings.InvoiceOutputFolder
                                  ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
               FileName = GetSuggestedFileName(parsedInvoiceNumber)
            };

            if (saveFileDialog.ShowDialog() != true)
               return;

            var invoiceData = new InvoiceData
            {
               InvoiceNumber = parsedInvoiceNumber.ToString(),
               InvoiceDate   = InvoiceDate,
               SellerName      = InvoiceSettings.InvoiceSellerName,
               SellerAddress1  = InvoiceSettings.InvoiceSellerAddress1,
               SellerAddress2  = InvoiceSettings.InvoiceSellerAddress2,
               SellerPhone     = InvoiceSettings.InvoiceSellerPhone,
               SellerEmail     = InvoiceSettings.InvoiceSellerEmail,
               ClientLegalName = InvoiceSettings.InvoiceClientLegalName,
               ClientAddress1  = InvoiceSettings.InvoiceClientAddress1,
                ClientAddress2  = InvoiceSettings.InvoiceClientAddress2,
                ClientShortName = InvoiceSettings.InvoiceClientShortName,
                Note            = InvoiceSettings.InvoiceNote,
                Currency        = string.IsNullOrWhiteSpace(InvoiceSettings.InvoiceCurrency) ? "$" : InvoiceSettings.InvoiceCurrency,
                GroupingType = InvoiceSettings.GroupingType,
                RoundingInterval = InvoiceSettings.RoundingInterval,
                RoundingType = InvoiceSettings.RoundingType,
                Projects = selectedProjects
                   .Select(x => new ProjectData
                   {
                      ProjectName = x.ProjectName,
                      PeriodFrom = StartDate,
                      PeriodTo = EndDate,
                      LineItems = GetInvoiceLineItemsForProject(x.Id, x.HourlyRate)
                   })
                   .ToArray()
            };

            InvoiceWorkbookGenerator.CreateInvoice(invoiceData, saveFileDialog.FileName);

            InvoiceSettings.InvoiceOutputFolder = Path.GetDirectoryName(saveFileDialog.FileName);
            InvoiceSettings.NextInvoiceNumber   = Math.Max(InvoiceSettings.NextInvoiceNumber, parsedInvoiceNumber) + 1;
            mainVm.Repository.UpdateSettings();
            InvoiceNumber = InvoiceSettings.NextInvoiceNumber.ToString();

            Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { Verb = "OPEN", UseShellExecute = true });
         }
         catch (Exception ex)
         {
            _ = DialogHost.Show(new ErrorDialog { DataContext = ex.Message });
         }
      }

      private void ReloadProjects()
      {
         var currentProjects = InvoiceProjects.ToDictionary(
            x => x.Id,
            x => new { x.IsSelected, x.HourlyRate });

         var invoiceProjects = mainVm.Repository.GetProjects()
            .Select(project =>
            {
               currentProjects.TryGetValue(project.Id, out var existingProject);
               var invoiceProject = new InvoiceProject
               {
                  Id          = project.Id,
                  ProjectName = project.Name,
                  Description = mainVm.Repository.GetProjectInfo(project.Id),
                  Hours       = GetHoursForProject(project.Id),
                  HourlyRate  = existingProject?.HourlyRate ?? InvoiceSettings.InvoiceDefaultHourlyRate,
                  IsSelected  = existingProject?.IsSelected ?? true
               };
               invoiceProject.PropertyChanged += InvoiceProjectOnPropertyChanged;
               return invoiceProject;
            })
            .ToList();

         foreach (var existingProject in InvoiceProjects)
            existingProject.PropertyChanged -= InvoiceProjectOnPropertyChanged;

         InvoiceProjects.ReplaceRange(invoiceProjects);
         NotifyOfPropertyChange(nameof(ProjectsTitle));
         CommandManager.InvalidateRequerySuggested();
      }

      private void InvoiceProjectOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName is nameof(InvoiceProject.IsSelected) or nameof(InvoiceProject.HourlyRate))
         {
            NotifyOfPropertyChange(nameof(ProjectsTitle));
            CommandManager.InvalidateRequerySuggested();
         }
      }

      private decimal GetHoursForProject(int projectId)
      {
         decimal hours = 0m;
         for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
         {
            var records = mainVm.Repository.GetTimeRecords(date, [projectId])
               .Where(x => x.EndTime != null);
            hours += records.Sum(x => (decimal)(x.EndTime!.Value - x.StartTime).TotalHours);
         }

         return decimal.Round(hours, 2, MidpointRounding.AwayFromZero);
      }

      private InvoiceLineItem[] GetInvoiceLineItemsForProject(int projectId, decimal hourlyRate)
      {
         var lineItems = new List<InvoiceLineItem>();
         for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
         {
            var records = mainVm.Repository.GetTimeRecords(date, [projectId])
               .Where(x => x.EndTime != null);
            lineItems.AddRange(records.Select(record => new InvoiceLineItem
            {
               Description = $"{date:dd MMM yyyy} - {record.TaskName}",
               GroupingKey = record.TaskName,
               Hours = (decimal)(record.EndTime!.Value - record.StartTime).TotalHours,
               HourlyRate = hourlyRate
            }));
         }

         return lineItems.ToArray();
      }

      private string GetSuggestedFileName(int invoiceNumber)
      {
         var clientName = InvoiceSettings.InvoiceClientShortName;
         if (string.IsNullOrWhiteSpace(clientName))
            clientName = "Client";

         var invalidChars = Path.GetInvalidFileNameChars();
         clientName = new string(clientName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

         return $"Inv ({invoiceNumber}) {StartDate:d MMM yyyy} - {EndDate:d MMM yyyy} - {clientName}.xlsx";
      }

      private static (DateTime StartDate, DateTime EndDate) GetDefaultPeriod()
      {
         var currentDate = DateTime.Today;
         var startDate   = new DateTime(currentDate.Year, currentDate.Month, 1);
         if (currentDate.Day < DateTime.DaysInMonth(currentDate.Year, currentDate.Month))
            startDate = startDate.AddMonths(-1);

         var endDate = startDate.AddMonths(1).AddDays(-1);
         return (startDate, endDate);
      }
   }

   public class InvoiceProject : ValidatableModel
   {
      public int Id { get; set; }

      public required string ProjectName { get; set; }

      public required string Description { get; set; }

      public decimal Hours
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(HoursDisplay));
            NotifyOfPropertyChange(nameof(AmountDisplay));
         }
      }

      public decimal HourlyRate
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(AmountDisplay));
         }
      }

      public bool IsSelected
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
         }
      }

      public string HoursDisplay => $"{Hours:0.##} h";

      public string AmountDisplay => $"{Hours * HourlyRate:0.00}";
   }
}
