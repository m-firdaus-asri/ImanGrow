using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ImanGrow
{
    public static class FirebaseService
    {
        // 🔥 IMPORTANT: Your Firebase URL must always end with a "/" and always use ".json"
        private const string BaseUrl =
            "https://imangrow-default-rtdb.asia-southeast1.firebasedatabase.app/";

        private static readonly HttpClient _client = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };


        // ---------------------------------------------------
        // URL builder
        // ---------------------------------------------------
        private static string ProgressUrl(string? dateKey = null)
        {
            return string.IsNullOrEmpty(dateKey)
                ? $"{BaseUrl}progress.json"
                : $"{BaseUrl}progress/{dateKey}.json";
        }


        // ---------------------------------------------------
        // SAVE DAILY PROGRESS
        // ---------------------------------------------------
        public static async Task SaveDailyProgressAsync(string dateKey, DailyProgress progress)
        {
            try
            {
                string json = JsonSerializer.Serialize(progress, _jsonOptions);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PutAsync(ProgressUrl(dateKey), content);

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Firebase Save Error: " + ex.Message);
                throw;
            }
        }


        // ---------------------------------------------------
        // GET DAILY PROGRESS
        // ---------------------------------------------------
        public static async Task<DailyProgress?> GetDailyProgressAsync(string dateKey)
        {
            try
            {
                var response = await _client.GetAsync(ProgressUrl(dateKey));
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return null;

                return JsonSerializer.Deserialize<DailyProgress>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Firebase Get Daily Progress Error: " + ex.Message);
                return null;
            }
        }


        // ---------------------------------------------------
        // GET ALL PROGRESS
        // ---------------------------------------------------
        public static async Task<Dictionary<string, DailyProgress>?> GetAllProgressAsync()
        {
            try
            {
                var response = await _client.GetAsync(ProgressUrl());
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return null;

                return JsonSerializer.Deserialize<Dictionary<string, DailyProgress>>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Firebase Get All Progress Error: " + ex.Message);
                return null;
            }
        }
    }
}
