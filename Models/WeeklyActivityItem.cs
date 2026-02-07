namespace ImanGrow.Models
{
    public class WeeklyActivityItem
    {
        public string Name { get; set; } = "";

        // Stored as 0–1 range
        public double Percentage { get; set; }

        // "85%"
        public string PercentText => $"{Math.Round(Percentage * 100)}%";

        // For ProgressBar
        public double Fraction => Percentage;
    }
}
