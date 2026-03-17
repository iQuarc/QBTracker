using QBTracker.Model;

namespace QBTracker.DataAccess;

public interface ILogger
{
   void AddLogEntry(LogEntry entry);

   public void Info(string message)
   {
      AddLogEntry(new LogEntry
      {
         Type    = LogEntryType.Info,
         Message = message
      });
   }

   public void Error(string error)
   {
      AddLogEntry(new LogEntry
      {
         Type    = LogEntryType.Error,
         Message = error
      });
   }
}