using System;
using System.Collections.Generic;
using QBTracker.Model;

namespace QBTracker.DataAccess
{
    public interface IRepository: IDisposable
    {
        List<Project> GetProjects();
        void AddProject(Project project);
    }
}