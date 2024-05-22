using LiteDB;

using QBTracker.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QBTracker.Annotations;

namespace QBTracker.DataAccess
{
    public class Repository : IRepository, INotifyPropertyChanged
    {
        private Settings settingsCache;
        private int timeUpdated;

        public Repository()
        {
#if DEBUG
            var file = @"App_Data\QBData.db";
#else
            var appDAta = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(appDAta, @"QBTracker\QBData.db"); 
#endif
            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            Db = new LiteRepository(file);
            CheckDbVersion();
            EnsureIndexes();
        }

        private ILiteRepository Db { get; }

        public List<Project> GetProjects()
        {
            return Db.Query<Project>("Projects")
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public Project GetProjectById(int id)
        {
            return Db.SingleById<Project>(id, "Projects");
        }

        public void AddProject(Project project)
        {
            Db.Insert(project, "Projects");
        }

        public void UpdateProject(Project project)
        {
            Db.Update(project, "Projects");
        }


        public List<Task> GetTasks(int projectId)
        {
            return Db.Query<Task>("Tasks")
                .Where(x => x.ProjectId == projectId && !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .ToList();
        }

        public Task GetTaskById(int id)
        {
            return Db.SingleById<Task>(id, "Tasks");
        }

        public void AddTask(Task task)
        {
            Db.Insert(task, "Tasks");
        }

        public void UpdateTask(Task task)
        {
            Db.Update(task, "Tasks");
        }

        public List<TimeRecord> GetTimeRecords(DateTime date, IReadOnlyCollection<int> projectIds = null)
        {
            var start = date.Date;
            var end = start.AddDays(1).AddTicks(-1);

            var q = Db.Query<TimeRecord>("TimeRecords")
                .Where(x => x.StartTime >= start && x.StartTime <= end);

            if (projectIds != null)
                q = q.Where(x => projectIds.Contains(x.ProjectId));

            var records = q.OrderBy(x => x.StartTime)
            .ToList();
            foreach (var record in records)
            {
                record.StartTime = record.StartTime.ToUniversalTime();
                record.EndTime = record.EndTime?.ToUniversalTime();
            }
            return records;
        }

        public TimeRecord GetRunningTimeRecord()
        {
            return Db.Query<TimeRecord>("TimeRecords")
                .Where(x => x.EndTime == null)
                .OrderByDescending(x => x.StartTime)
                .FirstOrDefault();
        }

        public void AddTimeRecord(TimeRecord record)
        {
            Db.Insert(record, "TimeRecords");
            UpdateAggregatedTime(record.StartTime);
            TimeUpdated++;
        }

        public void UpdateTimeRecord(TimeRecord record)
        {
            var old = Db.SingleOrDefault<TimeRecord>(x => x.Id == record.Id, "TimeRecords");
            if (old != null && old.StartTime.Day != record.StartTime.Day)
                UpdateAggregatedTime(old.StartTime);
            Db.Update(record, "TimeRecords");
            UpdateAggregatedTime(record.StartTime);
            TimeUpdated++;
        }

        public void DeleteTimeRecord(TimeRecord record)
        {
            Db.Delete<TimeRecord>(record.Id, "TimeRecords");
            UpdateAggregatedTime(record.StartTime);
            TimeUpdated++;
        }

        public Settings GetSettings()
        {
            if (this.settingsCache == null)
            {
                this.settingsCache = Db.SingleOrDefault<Settings>(x => x.Id == 1, "Settings");
                if (this.settingsCache == null)
                {
                    this.settingsCache = new Settings();
                    Db.Insert(settingsCache, "Settings");
                }
            }

            return settingsCache;
        }

        public void UpdateSettings()
        {
            if (settingsCache != null)
                Db.Update(settingsCache, "Settings");
        }

        public ILiteRepository GetLiteRepository()
        {
            return Db;
        }

        public TimeSpan GetDayAggregatedDayTime(DateTime date)
        {
            var rec = GetTimeRecords(date);
            return rec.Select(x => (x.EndTime ?? DateTime.Now) - x.StartTime).Aggregate(TimeSpan.Zero, (acc, x) => acc + x);
        }

        public TimeSpan GetDayAggregatedMonthTime(DateTime date)
        {
            return GetMonthAggregate(date).AggregateTime;
        }

        public string GetProjectInfo(int projectId)
        {
            return $"Tasks: {Db.Query<Task>("Tasks").Where(x => x.ProjectId == projectId && !x.IsDeleted).Count()}";
        }

        public void Dispose()
        {
            Db?.Dispose();
        }

        private void UpdateAggregatedTime(DateTime day)
        {
            var aggregate = GetMonthAggregate(day);

            aggregate.DayAggregate[day.Day-1] = GetDayAggregatedDayTime(day);
            aggregate.AggregateTime = TimeSpan.FromTicks(aggregate.DayAggregate.Sum(x => x.Ticks));
            Db.Upsert(aggregate, "TimeAggregates");
        }

        private TimeAggregate GetMonthAggregate(DateTime day)
        {
            var aggregate = Db.FirstOrDefault<TimeAggregate>(x => x.Year == day.Year && x.Month == day.Month, "TimeAggregates");
            if (aggregate == null)
            {
                aggregate = new TimeAggregate
                {
                    Year = day.Year,
                    Month = day.Month
                };
                var firstDay = new DateTime(day.Year, day.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddTicks(-1);
                var dailyAggregates = Db.Query<TimeRecord>("TimeRecords")
                    .Where(x => x.StartTime >= firstDay && x.StartTime <= lastDay)
                    .ToList()
                    .GroupBy(x => x.StartTime.Day)
                    .Select(x => new
                    {
                        Day = x.Key,
                        Time = x.Select(x => (x.EndTime ?? DateTime.Now) - x.StartTime)
                            .Aggregate(TimeSpan.Zero, (acc, x) => acc + x)
                    });
                foreach (var dailyAggregate in dailyAggregates)
                {
                    aggregate.DayAggregate[dailyAggregate.Day-1] = dailyAggregate.Time;
                }
                aggregate.AggregateTime = TimeSpan.FromTicks(aggregate.DayAggregate.Sum(x => x.Ticks));
            }

            return aggregate;
        }

        public void ClearAggregates()
        {
            Db.Database.GetCollection<TimeAggregate>("TimeAggregates").DeleteAll();
        }

        private void EnsureIndexes()
        {
            var tasks = Db.Database.GetCollection<Task>("Tasks");
            tasks.EnsureIndex(x => x.ProjectId);

            var timeRecords = Db.Database.GetCollection<TimeRecord>("TimeRecords");
            timeRecords.EnsureIndex(x => x.StartTime);

            var timeAggregates = Db.Database.GetCollection<TimeAggregate>("TimeAggregates");
            timeAggregates.EnsureIndex(x => new { x.Year, x.Month }, true);
        }

        public int TimeUpdated
        {
            get => timeUpdated;
            set
            {
                if (value == timeUpdated) return;
                timeUpdated = value;
                OnPropertyChanged();
            }
        }

        private void CheckDbVersion()
        {
            //if (Db.Database.UserVersion == 0)
            //{
            //    // Schema migration goes here
            //    Db.Database.UserVersion = 1;
            //}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}