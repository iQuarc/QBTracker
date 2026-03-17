namespace QBTracker.Model
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public Stats Stats { get; set; }
    }

    public class Stats
    {
        public TimeSpan RecordedTime { get; set; }
    }
}