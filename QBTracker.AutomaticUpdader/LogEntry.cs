using System;

namespace QBTracker.AutomaticUpdader
{
    public class LogEntry
    {
        public int Id { get; set; }
        public Category Category { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime Date { get; set; }
    }
}