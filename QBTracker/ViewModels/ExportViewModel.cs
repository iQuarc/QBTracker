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

using static QBTracker.ViewModels.ExportViewModel;

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
            this.GoBack = new RelayCommand(_ => mainVm.GoBack());
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

        public IEnumerable<RoundingInterval> RoundingIntervals => Enum.GetValues<RoundingInterval>();
        public IEnumerable<RoundingType> RoundingTypes => Enum.GetValues<RoundingType>();
        public IEnumerable<GroupingType> GroupingTypes => Enum.GetValues<GroupingType>();
        public IEnumerable<WorksheetOption> WorksheetOptions => Enum.GetValues<WorksheetOption>();
        public IEnumerable<AutoFilterOption> AutoFilterOptions => Enum.GetValues<AutoFilterOption>();
        public IEnumerable<SummaryType> SummaryTypes => Enum.GetValues<SummaryType>();

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
                    var timeRecords = GetTimeRecords()
                        .ToList();
                    foreach (var exportGroup in GroupByWorkseetSheet(timeRecords))
                    {
                        //A workbook must have at least one cell, so lets add one... 
                        var ws = p.Workbook.Worksheets.Add(exportGroup.Key);
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

                        if (ExportSettings.AutoFilter == AutoFilterOption.AutoFilter)
                            ws.Cells["A1:E1"].AutoFilter = true;

                        foreach (var exportRecord in exportGroup)
                        {
                            ws.Cells[$"A{row}"].Value = exportRecord.Date;
                            ws.Cells[$"A{row}"].Style.Numberformat.Format = "mm-dd-yy"; // In Excel speak this means Date field that will be displayed with Region format
                            ws.Cells[$"B{row}"].Value = exportRecord.ProjectName;
                            ws.Cells[$"C{row}"].Value = exportRecord.TaskName;
                            ws.Cells[$"D{row}"].Value = exportRecord.DurationHours;
                            ws.Cells[$"D{row}"].Style.Numberformat.Format = "0.##";
                            ws.Cells[$"E{row}"].Value = exportRecord.Notes;
                            row++;
                        }
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    }

                    if (ExportSettings.Summary == SummaryType.Monthly)
                    {
                        var ws = p.Workbook.Worksheets.Add("Summary");
                        p.Workbook.Worksheets.MoveToStart("Summary");

                        var monthly = timeRecords
                                        .OrderBy(x => x.Date.Year)
                                        .OrderBy(x => x.Date.Month)
                                        .GroupBy(x => x.Date.ToString("MMMM yyyy"));
                        var projects = timeRecords
                                        .Select(x => x.ProjectName)
                                        .Distinct()
                                        .ToList();

                        ws.Cells["A1"].Value = "Month";
                        ws.Cells["A1"].Style.Font.Bold = true;
                        int idx = 1;
                        var pc = new Dictionary<string, string>();
                        foreach (var project in projects)
                        {
                            var col = GenerateSequence(idx++);
                            ws.Cells[$"{col}1"].Value = project;
                            ws.Cells[$"{col}1"].Style.Font.Bold = true;
                            pc[project] = col;
                        }
                        var total = GenerateSequence(idx);
                        ws.Cells[$"{total}1"].Value = "Montly Total";
                        ws.Cells[$"{total}1"].Style.Font.Bold = true;
                        if (ExportSettings.AutoFilter == AutoFilterOption.AutoFilter)
                            ws.Cells[$"A1:{total}1"].AutoFilter = true;

                        var row = 2;
                        foreach (var item in monthly)
                        {
                            ws.Cells[$"A{row}"].Value = item.Key;
                            var projectGoups = item
                                .GroupBy(x => x.ProjectName)
                                .Select(g => new { Project = g.Key, Hours = g.Sum(x => x.DurationHours) });
                            foreach (var group in projectGoups)
                            {
                                ws.Cells[$"{pc[group.Project]}{row}"].Value = group.Hours;
                                ws.Cells[$"{pc[group.Project]}{row}"].Style.Numberformat.Format = "0.##";
                            }
                            ws.Cells[$"{total}{row}"].Value = projectGoups.Sum(x => x.Hours);
                            ws.Cells[$"{total}{row}"].Style.Numberformat.Format = "0.##";
                            row++;
                        }

                        double grandTotal = 0;

                        ws.Cells[$"A{row}"].Value = "Totals";
                        ws.Cells[$"A{row}"].Style.Font.Bold = true;

                        foreach (var project in projects)
                        {
                            var sum = timeRecords.Where(x => x.ProjectName == project).Sum(x => x.DurationHours);
                            ws.Cells[$"{pc[project]}{row}"].Value = sum;
                            ws.Cells[$"{pc[project]}{row}"].Style.Numberformat.Format = "0.##";
                            ws.Cells[$"A{row}"].Style.Font.Bold = true;
                            grandTotal += sum;
                        }

                        ws.Cells[$"{total}{row}"].Value = grandTotal;
                        ws.Cells[$"{total}{row}"].Style.Numberformat.Format = "0.##";
                        ws.Cells[$"{total}{row}"].Style.Font.Bold = true;

                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    }
                    //Save the new workbook. We haven't specified the filename so use the Save as method.
                    p.SaveAs(new FileInfo(file));
                    Process.Start(new ProcessStartInfo(file) { Verb = "OPEN", UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                DialogHost.Show(new ErrorDialog() { DataContext = ex.Message });
            }
        }

        private IEnumerable<IGrouping<string, ExportRecord>> GroupByWorkseetSheet(IEnumerable<ExportRecord> timeRecords)
        {
            return ExportSettings.WorksheetOption switch
            {
                WorksheetOption.WorksheetPerMonth => timeRecords
                    .OrderByDescending(x => x.Date.Year)
                    .ThenByDescending(x => x.Date.Month)
                    .GroupBy(x => x.Date.ToString("MMMM yyyy")),
                WorksheetOption.WorksheetPerYear => timeRecords
                    .OrderByDescending(x => x.Date.Year)
                    .GroupBy(x => x.Date.ToString("yyyy")),
                WorksheetOption.SingleWorksheet => timeRecords.GroupBy(x => "TimeSheet"),
                _ => timeRecords.GroupBy(x => "TimeSheet")
            };
        }

        private IEnumerable<ExportRecord> GetTimeRecords()
        {
            for (DateTime date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                foreach (var record in GroupRecords(mainVm.Repository.GetTimeRecords(date), date))
                {
                    yield return record;
                }
            }
        }

        private IEnumerable<ExportRecord> GroupRecords(IEnumerable<TimeRecord> timeRecords, DateTime date)
        {
            timeRecords = timeRecords.Where(x => x.EndTime != null);
            return ExportSettings.GroupingType switch
            {
                GroupingType.NoGrouping => timeRecords.Select(x => new ExportRecord
                {
                    Date = date,
                    ProjectName = x.ProjectName,
                    TaskName = x.TaskName,
                    Notes = x.Notes,
                    DurationHours = Round((x.EndTime.Value - x.StartTime).TotalHours)
                }),
                GroupingType.GroupBeforeRound => timeRecords
                       .GroupBy(x => (x.ProjectName, x.TaskName))
                       .Select(g => new ExportRecord
                       {
                           Date = date,
                           ProjectName = g.Key.ProjectName,
                           TaskName = g.Key.TaskName,
                           DurationHours = Round(g.Sum(x => (x.EndTime.Value - x.StartTime).TotalHours)),
                           Notes = string.Join(Environment.NewLine, g.Select(x => x.Notes))
                       }),
                GroupingType.GroupAfterRound => timeRecords
                    .GroupBy(x => (x.ProjectName, x.TaskName))
                    .Select(g => new ExportRecord
                    {
                        Date = date,
                        ProjectName = g.Key.ProjectName,
                        TaskName = g.Key.TaskName,
                        DurationHours = g.Sum(x => Round((x.EndTime.Value - x.StartTime).TotalHours)),
                        Notes = string.Join(Environment.NewLine, g.Select(x => x.Notes))
                    }),
                    _ => throw new NotSupportedException(),
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
            public DateTime Date { get; set; }
            public string ProjectName { get; set; }
            public string TaskName { get; set; }
            public string Notes { get; set; }
            public double DurationHours { get; set; }
        }

        private string GenerateSequence(int num)
        {
            string str = "";
            char achar;
            int mod;
            while (true)
            {
                mod = (num % 26) + 65;
                num = (int)(num / 26);
                achar = (char)mod;
                str = achar + str;
                if (num > 0) num--;
                else if (num == 0) break;
            }
            return str;
        }
    }
}