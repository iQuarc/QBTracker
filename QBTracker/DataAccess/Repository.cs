using LiteDB;

using QBTracker.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace QBTracker.DataAccess
{
    public class Repository : IRepository
    {
        private ExportSettings settingsCache;

        public Repository()
        {
#if !DEBUG
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

        public List<TimeRecord> GetTimeRecords(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1).AddTicks(-1);
            return Db.Query<TimeRecord>("TimeRecords")
                .Where(x => x.StartTime >= start && x.StartTime <= end)
                .OrderBy(x => x.StartTime)
                .ToList();
        }

        public TimeRecord GetLastTimeRecord()
        {
            return Db.Query<TimeRecord>("TimeRecords")
                .OrderByDescending(x => x.StartTime)
                .FirstOrDefault();
        }

        public void AddTimeRecord(TimeRecord record)
        {
            Db.Insert(record, "TimeRecords");
        }

        public void UpdateTimeRecord(TimeRecord record)
        {
            Db.Update(record, "TimeRecords");
        }

        public void DeleteTimeRecord(int timeRecordId)
        {
            Db.Delete<TimeRecord>(timeRecordId, "TimeRecords");
        }

        public ExportSettings GetExportSettings()
        {
            if (this.settingsCache == null)
            {
                this.settingsCache = Db.SingleOrDefault<ExportSettings>(x => x.Id == 1, "Settings");
                if (this.settingsCache == null)
                {
                    this.settingsCache = new ExportSettings();
                    Db.Insert(settingsCache, "Settings");
                }
            }

            return settingsCache;
        }

        public void UpdateExportSettings()
        {
            if (settingsCache != null)
                Db.Update(settingsCache, "Settings");
        }

        public void Dispose()
        {
            Db?.Dispose();
        }

        private void EnsureIndexes()
        {
            var tasks = Db.Database.GetCollection<Task>("Tasks");
            tasks.EnsureIndex(x => x.ProjectId);

            var timeRecords = Db.Database.GetCollection<TimeRecord>("TimeRecords");
            timeRecords.EnsureIndex(x => x.StartTime);
        }

        private void CheckDbVersion()
        {
            //if (Db.Database.UserVersion == 0)
            //{
            //    // Schema migration goes here
            //    Db.Database.UserVersion = 1;
            //}
        }
    }
}