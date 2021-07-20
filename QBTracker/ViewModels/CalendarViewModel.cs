using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class CalendarViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel mainVm;
        public CalendarViewModel(MainWindowViewModel mainVm)
        {
            this.mainVm = mainVm;
        }


    }
}