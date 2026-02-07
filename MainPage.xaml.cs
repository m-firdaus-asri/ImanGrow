using System.Reflection;
using System.Text.Json;
using ImanGrow.Models;


namespace ImanGrow
{
    public partial class MainPage : ContentPage
    {
        // Local prayer state
        public bool Subuh { get; set; }
        public bool Zohor { get; set; }
        public bool Asar { get; set; }
        public bool Maghrib { get; set; }
        public bool Isya { get; set; }

        public bool Quran { get; set; }
        public int ZikrCount { get; set; }

        // Prayer JSON dictionary
        Dictionary<string, Dictionary<string, string>> prayerData = new();

        public MainPage()
        {
            InitializeComponent();
            LoadPageData();
        }

        // Load page
        // Load page
        private async void LoadPageData()
        {
            LoadTodayDate();

            // Load JSON
            await LoadPrayerJson();

            // Determine next prayer
            DetermineNextPrayer();

        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadTodayProgressFromFirebase();
            await LoadLongestStreakFromFirebase();

            DetermineNextPrayer(); // refresh when returning
        }


        // =======================================
        // DATE
        // =======================================
        private void LoadTodayDate()
        {
            TodayDateLabel.Text = DateTime.Now.ToString("dddd, dd MMM yyyy");
        }


        // =======================================
        // LOAD PRAYER JSON (EMBEDDED RESOURCE)
        // =======================================
        private Task LoadPrayerJson()
        {
            try
            {
                var assembly = typeof(MainPage).GetTypeInfo().Assembly;

                string? resource = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("prayer_times_nov_dec.json"));

                if (resource == null)
                {
                    Console.WriteLine("❌ prayer_times_nov_dec.json NOT FOUND.");
                    return Task.CompletedTask;
                }

                using var stream = assembly.GetManifestResourceStream(resource);
                using var reader = new StreamReader(stream);

                string json = reader.ReadToEnd();

                prayerData =
                    JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)!;

                Console.WriteLine("✅ JSON Loaded Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ JSON Load Error: " + ex.Message);
            }

            return Task.CompletedTask;
        }



        // =======================================
        // DETERMINE NEXT PRAYER
        // =======================================
        private void DetermineNextPrayer()
        {
            if (prayerData == null || prayerData.Count == 0)
            {
                Console.WriteLine("❌ No JSON Loaded");
                return;
            }

            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (!prayerData.ContainsKey(today))
            {
                NextPrayerNameLabel.Text = "Next: --";
                NextPrayerTimeLabel.Text = "--:--";
                CurrentPrayerLabel.Text = "Current: --";
                return;
            }

            var times = prayerData[today];

            string Get(string key)
            {
                if (times.ContainsKey(key)) return times[key];
                var match = times.FirstOrDefault(x => x.Key.ToLower() == key.ToLower());
                return match.Value ?? "--:--";
            }

            var schedule = new List<(string Name, TimeSpan Time)>
            {
                ("Subuh",   TimeSpan.Parse(Get("subuh"))),
                ("Zohor",   TimeSpan.Parse(Get("zohor"))),
                ("Asar",    TimeSpan.Parse(Get("asar"))),
                ("Maghrib", TimeSpan.Parse(Get("maghrib"))),
                ("Isya",    TimeSpan.Parse(Get("isya")))
            };

            TimeSpan now = DateTime.Now.TimeOfDay;

            string current = "Before Subuh";
            string next = schedule[0].Name;
            TimeSpan? nextTime = schedule[0].Time;

            for (int i = 0; i < schedule.Count; i++)
            {
                if (now >= schedule[i].Time)
                {
                    current = schedule[i].Name;

                    if (i < schedule.Count - 1)
                    {
                        next = schedule[i + 1].Name;
                        nextTime = schedule[i + 1].Time;
                    }
                    else
                    {
                        next = "Subuh (Tomorrow)";
                        nextTime = null;
                    }
                }
            }

            // Update UI
            NextPrayerNameLabel.Text = $"Next: {next}";
            NextPrayerTimeLabel.Text = nextTime?.ToString(@"hh\:mm") ?? "--:--";
            CurrentPrayerLabel.Text = $"Current: {current}";
        }



        // =======================================
        // PRAYER TAPS
        // =======================================
        private void SubuhTapped(object sender, EventArgs e)
        { Subuh = !Subuh; SubuhTick.IsVisible = Subuh; }

        private void ZohorTapped(object sender, EventArgs e)
        { Zohor = !Zohor; ZohorTick.IsVisible = Zohor; }

        private void AsarTapped(object sender, EventArgs e)
        { Asar = !Asar; AsarTick.IsVisible = Asar; }

        private void MaghribTapped(object sender, EventArgs e)
        { Maghrib = !Maghrib; MaghribTick.IsVisible = Maghrib; }

        private void IsyaTapped(object sender, EventArgs e)
        { Isya = !Isya; IsyaTick.IsVisible = Isya; }

        private void QuranTapped(object sender, EventArgs e)
        { Quran = !Quran; QuranTick.IsVisible = Quran; }


        // =======================================
        // ZIKR COUNTER
        // =======================================
        private void IncreaseZikrTapped(object sender, EventArgs e)
        {
            ZikrCount++;
            ZikrLabel.Text = ZikrCount.ToString();
        }

        private void DecreaseZikrTapped(object sender, EventArgs e)
        {
            if (ZikrCount > 0) ZikrCount--;
            ZikrLabel.Text = ZikrCount.ToString();
        }


        // =======================================
        // SAVE PROGRESS
        // =======================================
        private async void SaveAllButtonClicked(object sender, EventArgs e)
        {
            string todayKey = DateTime.Now.ToString("yyyy-MM-dd");

            int quranPage = 0;
            if (!string.IsNullOrWhiteSpace(QuranPageEntry.Text))
                int.TryParse(QuranPageEntry.Text, out quranPage);

            var progress = new DailyProgress
            {
                subuh = Subuh,
                zohor = Zohor,
                asar = Asar,
                maghrib = Maghrib,
                isya = Isya,
                quran = Quran,
                quranPage = quranPage,
                zikr = ZikrCount
            };

            try
            {
                await FirebaseService.SaveDailyProgressAsync(todayKey, progress);
                await DisplayAlert("Saved", "Your daily progress has been saved.", "OK");
                await LoadLongestStreakFromFirebase();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        // =======================================
        // LOAD TODAY FROM FIREBASE
        // =======================================
        private async Task LoadTodayProgressFromFirebase()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                var progress = await FirebaseService.GetDailyProgressAsync(today);

                if (progress == null) return;

                Subuh = progress.subuh;
                Zohor = progress.zohor;
                Asar = progress.asar;
                Maghrib = progress.maghrib;
                Isya = progress.isya;
                Quran = progress.quran;
                ZikrCount = progress.zikr;

                SubuhTick.IsVisible = Subuh;
                ZohorTick.IsVisible = Zohor;
                AsarTick.IsVisible = Asar;
                MaghribTick.IsVisible = Maghrib;
                IsyaTick.IsVisible = Isya;
                QuranTick.IsVisible = Quran;

                ZikrLabel.Text = ZikrCount.ToString();
                QuranPageEntry.Text = progress.quranPage > 0 ? progress.quranPage.ToString() : "";
            }
            catch { }
        }


        // =======================================
        // LONGEST STREAK
        // =======================================
        private async Task LoadLongestStreakFromFirebase()
        {
            try
            {
                var all = await FirebaseService.GetAllProgressAsync();
                if (all == null || all.Count == 0)
                {
                    LongestStreakLabel.Text = "0 Days";
                    return;
                }

                var list = all.Where(kv => DateTime.TryParse(kv.Key, out _))
                              .Select(kv => (date: DateTime.Parse(kv.Key), progress: kv.Value))
                              .OrderByDescending(x => x.date)
                              .ToList();

                int streak = 0;
                DateTime day = DateTime.Today;

                while (list.Any(x => x.date == day && x.progress.IsComplete()))
                {
                    streak++;
                    day = day.AddDays(-1);
                }

                LongestStreakLabel.Text = $"{streak} Days";
            }
            catch
            {
                LongestStreakLabel.Text = "0 Days";
            }
        }
    }
}
