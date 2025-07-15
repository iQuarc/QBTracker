namespace QBTracker.Model;

public class LogEntry
{
   public int Id { get; set; }
   public LogEntryType Type { get; set; }
   public required string Message { get; set; }
}

public enum LogEntryType
{
   Info,
   Error
}