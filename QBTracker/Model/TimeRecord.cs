using System;
using System.Diagnostics;

namespace QBTracker.Model
{
    [DebuggerDisplay("{TaskName} - {StartTime} : {EndTime}")]
    public class TimeRecord
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string Notes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}