namespace QBTracker.Model
{
    public class Task
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProjectId { get; set; }
        public bool IsDeleted { get; set; }
        public Stats Stats { get; set; }
    }
}