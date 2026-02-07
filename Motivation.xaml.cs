using ImanGrow.Models;
using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImanGrow
{
    public partial class Motivation : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly Random _random = new();

        // ============================
        // QUOTES
        // ============================
        private readonly string[] _quotes =
        {
            "Every prayer brings you closer to peace. 🌿",
            "Keep your faith steady — progress starts small but grows strong. 💪",
            "Consistency in worship builds strength in the soul. 🌙",
            "You improved more than you think — keep moving forward. ✨",
            "Small steps in sincerity lead to great success. 🌸",
            "Stay grateful, stay growing. 🌱",
            "Your effort in every prayer is a victory itself. 🕌",
            "When you improve even one prayer, your day becomes brighter. ☀️"
        };

        // ============================
        // IMAGES
        // ============================
        private readonly string[] _images =
        {
            "motivation1.jpg",
            "motivation2.jpg",
            "motivation3.jpg"
        };

        // ============================
        // INSIGHT TEXT
        // ============================
        private string _insightMessage = "Loading your insight...";
        public string InsightMessage
        {
            get => _insightMessage;
            set { _insightMessage = value; OnPropertyChanged(nameof(InsightMessage)); }
        }

        // WEEKLY REPORT
        private string _weeklyReport = "Loading weekly report...";
        public string WeeklyReport
        {
            get => _weeklyReport;
            set { _weeklyReport = value; OnPropertyChanged(nameof(WeeklyReport)); }
        }

        // MONTHLY REPORT
        private string _monthlyReport = "Loading monthly report...";
        public string MonthlyReport
        {
            get => _monthlyReport;
            set { _monthlyReport = value; OnPropertyChanged(nameof(MonthlyReport)); }
        }

        // Prayer JSON storage
        Dictionary<string, Dictionary<string, string>> prayerData = new();

        public Motivation()
        {
            InitializeComponent();
            BindingContext = this;
        }

        // ============================
        // PAGE LOADING
        // ============================
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            LoadRandomMotivation();
            await LoadPrayerJson();
            LoadTodayPrayerTimes();
            await GenerateInsight();

        }

        // ============================
        // RANDOM QUOTE & IMAGE
        // ============================
        private void LoadRandomMotivation()
        {
            MotivationQuote.Text = _quotes[_random.Next(_quotes.Length)];

            var img = _images[_random.Next(_images.Length)];
            MotivationImage.Source = img;
            MotivationImage.IsVisible = true;
        }

        // ============================
        // LOAD EMBEDDED JSON
        // ============================
        private async Task LoadPrayerJson()
        {
            try
            {
                var assembly = typeof(Motivation).GetTypeInfo().Assembly;

                var resource = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("prayer_times_nov_dec.json"));

                if (resource == null)
                {
                    Console.WriteLine("❌ JSON file not found.");
                    return;
                }

                using var stream = assembly.GetManifestResourceStream(resource);
                using var reader = new StreamReader(stream);

                string json = await reader.ReadToEndAsync();
                prayerData = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, string>>
                >(json)!;

                Console.WriteLine("✅ JSON Loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ JSON Load Error: " + ex.Message);
            }
        }

        // ============================
        // LOAD TODAY PRAYER TIMES
        // ============================
        private void LoadTodayPrayerTimes()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (!prayerData.ContainsKey(today))
            {
                SubuhLabel.Text = ZohorLabel.Text = AsarLabel.Text =
                MaghribLabel.Text = IsyaLabel.Text = "--:--";
                return;
            }

            var t = prayerData[today];

            SubuhLabel.Text = t["subuh"];
            ZohorLabel.Text = t["zohor"];
            AsarLabel.Text = t["asar"];
            MaghribLabel.Text = t["maghrib"];
            IsyaLabel.Text = t["isya"];
        }

        // ============================
        // DAILY INSIGHT
        // ============================
        private async Task GenerateInsight()
        {
            var all = await FirebaseService.GetAllProgressAsync();
            if (all == null || all.Count < 2)
            {
                InsightMessage = "Track more days to unlock personalized insights 🌱";
                return;
            }

            var sorted = all
                .Where(kv => DateTime.TryParse(kv.Key, out _))
                .Select(kv => (date: DateTime.Parse(kv.Key), p: kv.Value))
                .OrderBy(x => x.date)
                .ToList();

            var yesterday = sorted[^2].p;
            var today = sorted[^1].p;

            string improvement = "";

            void check(string name, bool oldVal, bool newVal)
            {
                if (!oldVal && newVal) improvement = name;
            }

            check("Subuh", yesterday.subuh, today.subuh);
            check("Zohor", yesterday.zohor, today.zohor);
            check("Asar", yesterday.asar, today.asar);
            check("Maghrib", yesterday.maghrib, today.maghrib);
            check("Isya’", yesterday.isya, today.isya);

            if (today.quranPage > yesterday.quranPage)
                improvement = "your Quran recitation";

            if (today.zikr > yesterday.zikr)
                improvement = "your Zikr consistency";

            InsightMessage =
                improvement == "" ?
                "Stay consistent — even small steps bring you closer 🌙" :
                $"You're improving in {improvement}! Keep it up 🌟";
        }
    }
}

        

        

        


        