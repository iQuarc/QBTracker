using QBTracker.Model;

using System;
using System.Collections.Generic;

namespace QBTracker.DataAccess
{
    public interface IRepository : IDisposable
    {
        List<Project> GetProjects();
        Project GetProjectById(int id);
        void AddProject(Project project);
        void UpdateProject(Project project);
        List<Task> GetTasks(int projectId);
        Task GetTaskById(int id);
        void AddTask(Task task);
        void UpdateTask(Task task);
        List<TimeRecord> GetTimeRecords(DateTime date);
        TimeRecord GetLastTimeRecord();
        void AddTimeRecord(TimeRecord record);
        void UpdateTimeRecord(TimeRecord record);
        void DeleteTimeRecord(int timeRecordId);
    }
}