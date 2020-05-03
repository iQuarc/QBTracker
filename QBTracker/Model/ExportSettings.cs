namespace QBTracker.Model
{
    public class ExportSettings
    {
        public int Id { get; set; }
        public string ExportFolder { get; set; }
        public string ExportFileName { get; set; }
        public bool NoRounding { get; set; } = true;
        public bool Rounding15Min { get; set; }
        public bool Rounding30Min { get; set; }
    }
}