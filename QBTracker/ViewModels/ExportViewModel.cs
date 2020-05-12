using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

using OfficeOpenXml;

using QBTracker.Model;
using QBTracker.Util;
using QBTracker.Views;

namespace QBTracker.ViewModels
{
    public class ExportViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel mainVm;
        private DateTime startDate;
        private DateTime endDate;

        public ExportViewModel(MainWindowViewModel mainVm)
        {
            this.mainVm = mainVm;
            this.ExportCommand = new RelayCommand(ExecuteExport);
            this.GoBack = new RelayCommand(_=> mainVm.GoBack());
            this.ExportSettings = mainVm.Repository.GetSettings();
            this.StartDate = new DateTime(DateTime.Today.Year, 1, 1);
            this.EndDate = DateTime.Today;
        }

        public DateTime StartDate
        {
            get => startDate;
            set
            {
                startDate = value;
                NotifyOfPropertyChange();
            }
        }

        public DateTime EndDate
        {
            get => endDate;
            set
            {
                endDate = value;
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<RoundingInterval> RoundingIntervals =>
            (RoundingInterval[]) Enum.GetValues(typeof(RoundingInterval));

        public IEnumerable<RoundingType> RoundingTypes =>
            (RoundingType[])Enum.GetValues(typeof(RoundingType));

        public IEnumerable<GroupingType> GroupingTypes =>
            (GroupingType[])Enum.GetValues(typeof(GroupingType));

        public RelayCommand ExportCommand { get; }
        public RelayCommand GoBack { get; }
        public Settings ExportSettings { get; }

        private void ExecuteExport(object o)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Excel Files | *.xlsx";
            sfd.InitialDirectory = ExportSettings.ExportFolder 
                                   ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.FileName = ExportSettings.ExportFileName;

            if (sfd.ShowDialog() == true)
            {
                ExportData(sfd.FileName);
                ExportSettings.ExportFolder = Path.GetDirectoryName(sfd.FileName);
                ExportSettings.ExportFileName = Path.GetFileName(sfd.FileName);
                mainVm.Repository.UpdateSettings();
            }
        }

        private void ExportData(string file)
        {
            try
            {
                using (var p = new ExcelPackage())
                {
                    //A workbook must have at least one cell, so lets add one... 
                    var ws = p.Workbook.Worksheets.Add("TimeSheet");
                    //To set values in the spreadsheet use the Cells indexer.
                    ws.Cells["A1"].Value = "Date";
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["B1"].Value = "Project";
                    ws.Cells["B1"].Style.Font.Bold = true;
                    ws.Cells["C1"].Value = "Task";
                    ws.Cells["C1"].Style.Font.Bold = true;
                    ws.Cells["D1"].Value = "Hours";
                    ws.Cells["D1"].Style.Font.Bold = true;
                    ws.Cells["E1"].Value = "Notes";
                    ws.Cells["E1"].Style.Font.Bold = true;
                    var date = StartDate;
                    int row = 2;
                    while (date <= EndDate)
                    {
                        foreach (var exportRecord in Group(mainVm.Repository.GetTimeRecords(date)))
                        {
                            ws.Cells[$"A{row}"].Value = date;
                            ws.Cells[$"A{row}"].Style.Numberformat.Format = "mm-dd-yy"; // In Excel speak this means Date field that will be displayed with Region format
                            ws.Cells[$"B{row}"].Value = exportRecord.ProjectName;
                            ws.Cells[$"C{row}"].Value = exportRecord.TaskName;
                            ws.Cells[$"D{row}"].Value = exportRecord.DurationHours;
                            ws.Cells[$"D{row}"].Style.Numberformat.Format = "0.##";
                            ws.Cells[$"E{row}"].Value = exportRecord.Notes;
                            row++;
                        }
                        date = date.AddDays(1);
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    //Save the new workbook. We haven't specified the filename so use the Save as method.
                    p.SaveAs(new FileInfo(file));
                    Process.Start(new ProcessStartInfo(file) { Verb = "OPEN", UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                DialogHost.Show(new ErrorDialog() {DataContext = ex.Message});
            }
        }

        private IEnumerable<ExportRecord> Group(IEnumerable<TimeRecord> timeRecords)
        {
            timeRecords = timeRecords.Where(x => x.EndTime != null);
            return ExportSettings.GroupingType switch
            {
                GroupingType.NoGrouping => timeRecords.Select(x => new ExportRecord
                {
                    ProjectName = x.ProjectName,
                    TaskName = x.TaskName,
                    Notes = x.Notes,
                    DurationHours = Round((x.EndTime.Value - x.StartTime).TotalHours)
                }),
                GroupingType.GroupBeforeRound => timeRecords
                       .GroupBy(x => (x.ProjectName, x.TaskName))
                       .Select(g => new ExportRecord
                       {
                           ProjectName = g.Key.ProjectName,
                           TaskName = g.Key.TaskName,
                           DurationHours = Round(g.Sum(x => (x.EndTime.Value - x.StartTime).TotalHours)),
                           Notes = string.Join(Environment.NewLine, g.Select(x => x.Notes))
                       }),
                GroupingType.GroupAfterRound => timeRecords
                    .GroupBy(x => (x.ProjectName, x.TaskName))
                    .Select(g => new ExportRecord
                    {
                        ProjectName = g.Key.ProjectName,
                        TaskName = g.Key.TaskName,
                        DurationHours = g.Sum(x => Round((x.EndTime.Value - x.StartTime).TotalHours)),
                        Notes = string.Join(Environment.NewLine, g.Select(x => x.Notes))
                    }),
            };
        }

        private double Round(in double hours)
        {
            if (ExportSettings.RoundingInterval == RoundingInterval.RoundTo15Min)
                return ExportSettings.RoundingType == RoundingType.MidPointRounding
                    ? RoundF(hours, 4)
                    : CeilingF(hours, 4);

            if (ExportSettings.RoundingInterval == RoundingInterval.RoundTo30Min)
                return ExportSettings.RoundingType == RoundingType.MidPointRounding
                    ? RoundF(hours, 2)
                    : CeilingF(hours, 2);

            static double RoundF(in double hours, double factor)
            {
                var rounded = Math.Round(hours * factor, MidpointRounding.AwayFromZero) / factor;
                if (Math.Abs(rounded) < 0.001)
                    return 1 / factor;
                return rounded;
            }

            static double CeilingF(in double hours, double factor)
            {
                var rounded = Math.Ceiling(hours * factor) / factor;
                if (Math.Abs(rounded) < 0.001)
                    return 1 / factor;
                return rounded;
            }
            return hours;
        }

        public class ExportRecord
        {
            public string ProjectName { get; set; }
            public string TaskName { get; set; }
            public string Notes { get; set; }
            public double DurationHours { get; set; }
        }
    }
}