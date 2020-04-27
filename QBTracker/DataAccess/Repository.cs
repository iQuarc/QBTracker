using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using QBTracker.Model;

namespace QBTracker.DataAccess
{
    public class Repository: IRepository
    {
        public Repository()
        {
            var appDAta = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(appDAta,  @"QBTracker\QBData.db");
            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            Db = new LiteRepository(file);
        }

        private ILiteRepository Db { get; }

        public List<Project> GetProjects()
        {
            return Db.Query<Project>("Projects").ToList();
        }

        public void AddProject(Project project)
        {
            Db.Insert(project, "Projects");
        }

        public void Dispose()
        {
            Db?.Dispose();
        }
    }
}