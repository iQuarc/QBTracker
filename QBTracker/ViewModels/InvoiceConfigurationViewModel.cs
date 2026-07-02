using System.Runtime.CompilerServices;
using QBTracker.Model;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
   public class InvoiceConfigurationViewModel : ValidatableModel
   {
      private readonly MainWindowViewModel mainVm;

      public InvoiceConfigurationViewModel(MainWindowViewModel mainVm)
      {
         this.mainVm = mainVm;
         Settings = mainVm.Repository.GetSettings();
         GoBack = new RelayCommand(_ => mainVm.GoBack());
      }

      public Settings Settings { get; }

      public RelayCommand GoBack { get; }

      public void Activated()
      {
         NotifyOfPropertyChange(nameof(OutputFolder));
         NotifyOfPropertyChange(nameof(SellerName));
         NotifyOfPropertyChange(nameof(SellerAddress1));
         NotifyOfPropertyChange(nameof(SellerAddress2));
         NotifyOfPropertyChange(nameof(SellerPhone));
         NotifyOfPropertyChange(nameof(SellerEmail));
         NotifyOfPropertyChange(nameof(ClientLegalName));
         NotifyOfPropertyChange(nameof(ClientAddress1));
         NotifyOfPropertyChange(nameof(ClientAddress2));
         NotifyOfPropertyChange(nameof(ClientShortName));
         NotifyOfPropertyChange(nameof(Note));
         NotifyOfPropertyChange(nameof(Currency));
         NotifyOfPropertyChange(nameof(DefaultHourlyRate));
         NotifyOfPropertyChange(nameof(NextInvoiceNumber));
      }

      public string OutputFolder
      {
         get => Settings.InvoiceOutputFolder ?? string.Empty;
         set => SetValue(value, x => Settings.InvoiceOutputFolder = string.IsNullOrWhiteSpace(x) ? null : x);
      }

      public string SellerName
      {
         get => Settings.InvoiceSellerName;
         set => SetValue(value, x => Settings.InvoiceSellerName = x);
      }

      public string SellerAddress1
      {
         get => Settings.InvoiceSellerAddress1;
         set => SetValue(value, x => Settings.InvoiceSellerAddress1 = x);
      }

      public string SellerAddress2
      {
         get => Settings.InvoiceSellerAddress2;
         set => SetValue(value, x => Settings.InvoiceSellerAddress2 = x);
      }

      public string SellerPhone
      {
         get => Settings.InvoiceSellerPhone;
         set => SetValue(value, x => Settings.InvoiceSellerPhone = x);
      }

      public string SellerEmail
      {
         get => Settings.InvoiceSellerEmail;
         set => SetValue(value, x => Settings.InvoiceSellerEmail = x);
      }

      public string ClientLegalName
      {
         get => Settings.InvoiceClientLegalName;
         set => SetValue(value, x => Settings.InvoiceClientLegalName = x);
      }

      public string ClientAddress1
      {
         get => Settings.InvoiceClientAddress1;
         set => SetValue(value, x => Settings.InvoiceClientAddress1 = x);
      }

      public string ClientAddress2
      {
         get => Settings.InvoiceClientAddress2;
         set => SetValue(value, x => Settings.InvoiceClientAddress2 = x);
      }

      public string ClientShortName
      {
         get => Settings.InvoiceClientShortName;
         set => SetValue(value, x => Settings.InvoiceClientShortName = x);
      }

      public string Note
      {
         get => Settings.InvoiceNote;
         set => SetValue(value, x => Settings.InvoiceNote = x);
      }

      public string Currency
      {
         get => Settings.InvoiceCurrency;
         set => SetValue(value, x => Settings.InvoiceCurrency = string.IsNullOrWhiteSpace(x) ? "$" : x);
      }

      public decimal DefaultHourlyRate
      {
         get => Settings.InvoiceDefaultHourlyRate;
         set => SetValue(value, x => Settings.InvoiceDefaultHourlyRate = x <= 0 ? 0 : x);
      }

      public int NextInvoiceNumber
      {
         get => Settings.NextInvoiceNumber;
         set => SetValue(value, x => Settings.NextInvoiceNumber = Math.Max(1, x));
      }

      private void SetValue<T>(T value, Action<T> setter, [CallerMemberName] string propertyName = "")
      {
         setter(value);
         mainVm.Repository.UpdateSettings();
         NotifyOfPropertyChange(propertyName);
      }
   }
}
