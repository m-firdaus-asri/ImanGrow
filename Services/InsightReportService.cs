using ImanGrow.Models;

namespace ImanGrow.Services
{
    public static class InsightReportService
    {
        public static InsightReport BuildReport(Dictionary<string, DailyProgress> all)
        {
            var weeklyStats = BuildStats(all, 7);
            var monthlyStats = BuildStats(all, 30);

            return new InsightReport
            {
                WeeklySummary = BuildSummary("Weekly", weeklyStats),
                MonthlySummary = BuildSummary("Monthly", monthlyStats)
            };
        }


        // --------------------------
        // Build stats for X days
        // --------------------------
        private static PrayerStats BuildStats(Dictionary<string, DailyProgress> all, int days)
        {
            var today = DateTime.Today;
            var start = today.AddDays(-days);

            var records = all
                .Where(x =>
                {
                    DateTime d;
                    return DateTime.TryParse(x.Key, out d)
                        && d >= start
                        && d <= today;
                })
                .Select(x => x.Value)
                .ToList();

            var stats = new PrayerStats
            {
                TotalDays = records.Count,
                MissedSubuh = records.Count(x => !x.subuh),
                MissedZohor = records.Count(x => !x.zohor),
                MissedAsar = records.Count(x => !x.asar),
                MissedMaghrib = records.Count(x => !x.maghrib),
                MissedIsya = records.Count(x => !x.isya),
                FullyCompletedDays = records.Count(x => x.IsComplete(true)),
                QuranDays = records.Count(x => x.quran)
            };

            return stats;
        }


        // --------------------------
        // Build summary string
        // --------------------------
        private static string BuildSummary(string label, PrayerStats s)
        {
            if (s.TotalDays == 0)
                return $"{label} Report: No data for this period.";

            return
$@"{label} Report:
• Fully completed days: {s.FullyCompletedDays}/{s.TotalDays}
• Quran consistency: {s.QuranDays} days

Prayer Completion:
• Subuh: {s.SubuhRate:F0}%  ({s.MissedSubuh} missed)
• Zohor: {s.ZohorRate:F0}%  ({s.MissedZohor} missed)
• Asar: {s.AsarRate:F0}%  ({s.MissedAsar} missed)
• Maghrib: {s.MaghribRate:F0}%  ({s.MissedMaghrib} missed)
• Isya: {s.IsyaRate:F0}%  ({s.MissedIsya} missed)
";
        }
    }
}
