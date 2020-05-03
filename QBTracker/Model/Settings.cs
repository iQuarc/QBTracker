using MaterialDesignColors;

namespace QBTracker.Model
{
    public class Settings
    {
        public int Id { get; set; }
        public string ExportFolder { get; set; }
        public string ExportFileName { get; set; }
        public bool NoRounding { get; set; } = true;
        public bool Rounding15Min { get; set; }
        public bool Rounding30Min { get; set; }
        public bool? IsDark { get; set; }
        public PrimaryColor? PrimaryColor { get; set; }
        public SecondaryColor? SecondaryColor { get; set; }
    }
}