namespace ImanGrow
{
    public class DailyProgress
    {
        public bool subuh { get; set; }
        public bool zohor { get; set; }
        public bool asar { get; set; }
        public bool maghrib { get; set; }
        public bool isya { get; set; }

        public bool quran { get; set; }
        public int quranPage { get; set; }
        public int zikr { get; set; }

        // 🔥 REQUIRED FOR STREAK CALCULATION
        public bool IsComplete(bool ignoreQuran = false)
        {
            bool prayers = subuh && zohor && asar && maghrib && isya;

            if (ignoreQuran)
                return prayers;

            return prayers && quran;
        }


        // 🔥 Percent of solat completed
        public double CompletionPercent()
        {
            int count = 0;

            if (subuh) count++;
            if (zohor) count++;
            if (asar) count++;
            if (maghrib) count++;
            if (isya) count++;

            return (count / 5.0) * 100;
        }

        public double SolatPercent() => CompletionPercent();

        public double QuranPercent() => quran ? 100 : 0;

        public double ZikrPercent()
        {
            // Normalize zikr progress (target = 33)
            return Math.Min(zikr / 33.0, 1.0) * 100;
        }
    }
}
