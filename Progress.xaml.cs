using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace ImanGrow
{
    public partial class Progress : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // --------------------------------------------------------------
        // BASIC BINDINGS
        // --------------------------------------------------------------
        public string TodayDate { get; set; } =
            DateTime.Now.ToString("dddd, dd MMM yyyy");

        private string _longestStreak = "0";
        public string LongestStreak
        {
            get => _longestStreak;
            set { _longestStreak = value; OnPropertyChanged(nameof(LongestStreak)); }
        }

        private string _totalDaysTracked = "0";
        public string TotalDaysTracked
        {
            get => _totalDaysTracked;
            set { _totalDaysTracked = value; OnPropertyChanged(nameof(TotalDaysTracked)); }
        }

        public string WeekLabel { get; set; } =
            $"Week {ISOWeek.GetWeekOfYear(DateTime.Today)}";

        public string MonthLabel { get; set; } =
            DateTime.Now.ToString("MMMM");

        private double _weekOverallPercent;
        public double WeekOverallPercent
        {
            get => _weekOverallPercent;
            set { _weekOverallPercent = value; OnPropertyChanged(nameof(WeekOverallPercent)); }
        }

        private double _monthOverallPercent;
        public double MonthOverallPercent
        {
            get => _monthOverallPercent;
            set { _monthOverallPercent = value; OnPropertyChanged(nameof(MonthOverallPercent)); }
        }


        // --------------------------------------------------------------
        // WEEK/MONTH TOGGLE
        // --------------------------------------------------------------
        private bool _isWeeklySelected = true;
        public bool IsWeeklySelected
        {
            get => _isWeeklySelected;
            set
            {
                _isWeeklySelected = value;
                OnPropertyChanged(nameof(IsWeeklySelected));
                OnPropertyChanged(nameof(IsWeekMode));
                OnPropertyChanged(nameof(IsMonthMode));
            }
        }

        public bool IsWeekMode => IsWeeklySelected;
        public bool IsMonthMode => !IsWeeklySelected;

        public string WeekSelectedColor =>
            IsWeekMode ? "#2CA82E" : "#E6E6E6";

        public string MonthSelectedColor =>
            IsMonthMode ? "#2CA82E" : "#E6E6E6";


        // --------------------------------------------------------------
        // NEW: BREAKDOWN / WEEKLY ACTIVITY TOGGLE
        // --------------------------------------------------------------
        private bool _isBreakdownMode = true;
        public bool IsBreakdownMode
        {
            get => _isBreakdownMode;
            set
            {
                _isBreakdownMode = value;
                OnPropertyChanged(nameof(IsBreakdownMode));
                OnPropertyChanged(nameof(IsWeeklyActivityMode));
            }
        }

        public bool IsWeeklyActivityMode => !IsBreakdownMode;

        public string BreakdownSelectedColor =>
            IsBreakdownMode ? "#2CA82E" : "#E6E6E6";

        public string WeeklyActivitySelectedColor =>
            IsWeeklyActivityMode ? "#2CA82E" : "#E6E6E6";


        // --------------------------------------------------------------
        // COLLECTION
        // --------------------------------------------------------------
        public ObservableCollection<ActivityItem> ActivityProgress { get; set; }
            = new ObservableCollection<ActivityItem>();


        // --------------------------------------------------------------
        // WEEK NAVIGATION
        // --------------------------------------------------------------
        private int selectedWeekNumber =
            ISOWeek.GetWeekOfYear(DateTime.Today);


        // --------------------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------------------
        public Progress()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOverallData();
            await LoadActivityBreakdown();
            await LoadWeeklyHistory();
        }


        // --------------------------------------------------------------
        // OVERALL DATA
        // --------------------------------------------------------------
        private async Task LoadOverallData()
        {
            var all = await FirebaseService.GetAllProgressAsync();
            if (all == null || all.Count == 0) return;

            TotalDaysTracked = all.Count.ToString();

            var ordered = all
                .Where(kv => DateTime.TryParse(kv.Key, out _))
                .Select(kv => (date: DateTime.Parse(kv.Key), progress: kv.Value))
                .OrderByDescending(x => x.date)
                .ToList();


            // LONGEST STREAK
            int streak = 0;
            DateTime day = DateTime.Today;

            while (ordered.Any(x => x.date == day && x.progress.IsComplete()))
            {
                streak++;
                day = day.AddDays(-1);
            }

            LongestStreak = streak.ToString();


            // WEEK SUMMARY
            DateTime today = DateTime.Today;
            int iso = (int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek;
            DateTime weekStart = today.AddDays(-(iso - 1));
            var weekItems = ordered.Where(x => x.date >= weekStart).ToList();

            if (weekItems.Any())
                WeekOverallPercent = Math.Round(weekItems.Average(x => x.progress.CompletionPercent()));

            // MONTH SUMMARY
            var monthItems = ordered.Where(x => x.date.Month == today.Month).ToList();

            if (monthItems.Any())
                MonthOverallPercent = Math.Round(monthItems.Average(x => x.progress.CompletionPercent()));
        }


        // --------------------------------------------------------------
        // WEEK/MONTH TOGGLE HANDLERS
        // --------------------------------------------------------------
        private async void WeekToggleTapped(object sender, TappedEventArgs e)
        {
            IsWeeklySelected = true;
            OnPropertyChanged(nameof(WeekSelectedColor));
            OnPropertyChanged(nameof(MonthSelectedColor));

            await LoadActivityBreakdown();
        }

        private async void MonthToggleTapped(object sender, TappedEventArgs e)
        {
            IsWeeklySelected = false;
            OnPropertyChanged(nameof(WeekSelectedColor));
            OnPropertyChanged(nameof(MonthSelectedColor));

            await LoadActivityBreakdown();
        }


        // --------------------------------------------------------------
        // NEW: BREAKDOWN / WEEKLY ACTIVITY TOGGLE HANDLERS
        // --------------------------------------------------------------
        private async void OnBreakdownTapped(object sender, TappedEventArgs e)
        {
            IsBreakdownMode = true;
            OnPropertyChanged(nameof(BreakdownSelectedColor));
            OnPropertyChanged(nameof(WeeklyActivitySelectedColor));

            await LoadActivityBreakdown();
        }

        private async void OnWeeklyActivityTapped(object sender, TappedEventArgs e)
        {
            IsBreakdownMode = false;
            OnPropertyChanged(nameof(BreakdownSelectedColor));
            OnPropertyChanged(nameof(WeeklyActivitySelectedColor));

            await LoadWeeklyHistory();
        }


        // --------------------------------------------------------------
        // ACTIVITY BREAKDOWN
        // --------------------------------------------------------------
        private async Task LoadActivityBreakdown()
        {
            ActivityProgress.Clear();

            var all = await FirebaseService.GetAllProgressAsync();
            if (all == null || all.Count == 0) return;

            if (IsWeeklySelected)
                await LoadWeeklyBreakdown(all);
            else
                await LoadMonthlyBreakdown(all);
        }

        private Task LoadWeeklyBreakdown(Dictionary<string, DailyProgress> all)
        {
            DateTime today = DateTime.Today;

            int iso = (int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek;
            DateTime weekStart = today.AddDays(-(iso - 1));

            var range = all
                .Where(kv => DateTime.TryParse(kv.Key, out var d) &&
                             d >= weekStart && d <= today)
                .Select(kv => kv.Value)
                .ToList();

            if (range.Any())
                AddActivityBars(range);

            return Task.CompletedTask;
        }

        private Task LoadMonthlyBreakdown(Dictionary<string, DailyProgress> all)
        {
            int month = DateTime.Today.Month;

            var range = all
                .Where(kv => DateTime.TryParse(kv.Key, out var d) && d.Month == month)
                .Select(kv => kv.Value)
                .ToList();

            if (range.Any())
                AddActivityBars(range);

            return Task.CompletedTask;
        }

        private void AddActivityBars(List<DailyProgress> range)
        {
            double Avg(Func<DailyProgress, bool> fn) =>
                range.Average(x => fn(x) ? 1 : 0) * 100;

            ActivityProgress.Add(new ActivityItem("Subuh", Avg(x => x.subuh)));
            ActivityProgress.Add(new ActivityItem("Zohor", Avg(x => x.zohor)));
            ActivityProgress.Add(new ActivityItem("Asar", Avg(x => x.asar)));
            ActivityProgress.Add(new ActivityItem("Maghrib", Avg(x => x.maghrib)));
            ActivityProgress.Add(new ActivityItem("Isya’", Avg(x => x.isya)));

            ActivityProgress.Add(new ActivityItem("Quran", range.Average(x => x.QuranPercent())));
            ActivityProgress.Add(new ActivityItem("Zikr", range.Average(x => x.ZikrPercent())));
        }


        // --------------------------------------------------------------
        // WEEKLY HISTORY
        // --------------------------------------------------------------
        private async Task LoadWeeklyHistory()
        {
            WeekHistoryContainer.Children.Clear();

            var all = await FirebaseService.GetAllProgressAsync();
            if (all == null || all.Count == 0) return;

            SelectedWeekLabel.Text = $"Week {selectedWeekNumber}";

            var parsed = all
                .Where(kv => DateTime.TryParse(kv.Key, out _))
                .Select(kv => (date: DateTime.Parse(kv.Key), p: kv.Value))
                .OrderBy(x => x.date)
                .ToList();

            int year = DateTime.Today.Year;

            DateTime weekStart = ISOWeek.ToDateTime(year, selectedWeekNumber, DayOfWeek.Monday);
            DateTime weekEnd = weekStart.AddDays(6);

            var thisWeek = parsed
                .Where(x => x.date >= weekStart && x.date <= weekEnd)
                .ToList();


            if (!thisWeek.Any())
            {
                WeekHistoryContainer.Children.Add(
                    new Label
                    {
                        Text = "No data for this week.",
                        TextColor = Colors.Black,
                        HorizontalTextAlignment = TextAlignment.Center
                    });
                return;
            }

            // Weekly averages
            double avg(Func<DailyProgress, bool> fn) =>
                thisWeek.Average(x => fn(x.p) ? 1 : 0) * 100;

            double subuh = avg(x => x.subuh);
            double zohor = avg(x => x.zohor);
            double asar = avg(x => x.asar);
            double maghrib = avg(x => x.maghrib);
            double isya = avg(x => x.isya);

            double quran = thisWeek.Average(x => x.p.QuranPercent());
            double zikr = thisWeek.Average(x => x.p.ZikrPercent());

            AddHistoryCard("Subuh", subuh);
            AddHistoryCard("Zohor", zohor);
            AddHistoryCard("Asar", asar);
            AddHistoryCard("Maghrib", maghrib);
            AddHistoryCard("Isya’", isya);

            AddHistoryCard("Quran Recitation", quran);
            AddHistoryCard("Zikr", zikr);
        }


        private void AddHistoryCard(string title, double percent)
        {
            WeekHistoryContainer.Children.Add(
                new Frame
                {
                    BackgroundColor = Color.FromArgb("#F5F5F5"),
                    CornerRadius = 16,
                    Padding = 14,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = Colors.Black },
                            new Label { Text = $"{percent:0}%", FontSize = 14, TextColor = Colors.Black },
                            new ProgressBar { Progress = percent / 100.0, HeightRequest = 8, ProgressColor = Colors.Green }
                        }
                    }
                }
            );
        }


        // --------------------------------------------------------------
        // WEEK NAVIGATION
        // --------------------------------------------------------------
        private async void OnPreviousWeekTapped(object sender, TappedEventArgs e)
        {
            selectedWeekNumber--;
            if (selectedWeekNumber < 1)
                selectedWeekNumber = 1;

            await LoadWeeklyHistory();
        }

        private async void OnNextWeekTapped(object sender, TappedEventArgs e)
        {
            selectedWeekNumber++;
            await LoadWeeklyHistory();
        }
    }


    // --------------------------------------------------------------
    // UI MODEL
    // --------------------------------------------------------------
    public class ActivityItem
    {
        public string Name { get; set; }
        public string PercentText { get; set; }
        public double Fraction { get; set; }

        public ActivityItem(string name, double percent)
        {
            Name = name;
            PercentText = $"{percent:0}%";
            Fraction = percent / 100.0;
        }
    }
}
