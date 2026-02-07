namespace ImanGrow.Models
{
    public class PrayerStats
    {
        public int TotalDays { get; set; }
        public int FullyCompletedDays { get; set; }
        public int MissedSubuh { get; set; }
        public int MissedZohor { get; set; }
        public int MissedAsar { get; set; }
        public int MissedMaghrib { get; set; }
        public int MissedIsya { get; set; }
        public int QuranDays { get; set; }

        public double SubuhRate => CalcRate(TotalDays, MissedSubuh);
        public double ZohorRate => CalcRate(TotalDays, MissedZohor);
        public double AsarRate => CalcRate(TotalDays, MissedAsar);
        public double MaghribRate => CalcRate(TotalDays, MissedMaghrib);
        public double IsyaRate => CalcRate(TotalDays, MissedIsya);

        private double CalcRate(int days, int missed)
        {
            if (days == 0) return 0;
            return ((days - missed) / (double)days) * 100;
        }
    }
}
