using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MonarchCalculator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Logger.LogInfo(LogMessages.AppStarting);

            // Simple DI
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(AppSettings.HttpTimeout) };
            IMonarchRepository repository = new MonarchRepository(httpClient);
            IMonarchService service = new MonarchService(repository, new MonarchCacheService());

            try
            {
                string dataUrl = AppSettings.DataUrl;

                // Fetch data
                var response = await service.GetMonarchsAsync(dataUrl);
                if (!response.Status || response.Data == null)
                {
                    Logger.LogError(LogMessages.DataFetchFailedPrefix + response.Message);
                    return;
                }

                var monarchs = response.Data;

                int totalCount = service.GetTotalMonarchCount(monarchs);
                var (longestName, longestYears) = service.GetLongestRulingMonarch(monarchs);
                var (houseName, houseDuration) = service.GetLongestRulingHouse(monarchs);
                var mostCommonFirstName = service.GetMostCommonFirstName(monarchs);

                // Print results
                Logger.LogInfo($"1) Total monarch count   : {totalCount}");
                Logger.LogInfo($"2) Longest ruling monarch: {longestName} ({longestYears} years)");
                Logger.LogInfo($"3) Longest ruling house  : {houseName} ({houseDuration} years)");
                Logger.LogInfo($"4) Most common first name: {mostCommonFirstName}");

                Logger.LogInfo(LogMessages.AppCompleted);
            }
            catch (Exception ex)
            {
                Logger.LogError(LogMessages.CriticalError, ex);
            }
        }
    }

    #region Const Message Classes
    public static class LogMessages
    {
        public const string AppStarting = "Application is starting...";
        public const string AppCompleted = "Application completed successfully.";
        public const string CriticalError = "A critical error occurred.";
        public const string DataFetchFailedPrefix = "Data fetch failed: ";

        public const string DataRetrievedFromCache = "Data retrieved from cache.";
        public const string FetchingDataFromRemote = "Fetching data from remote...";
        public const string ErrorWhileFetching = "Error while fetching/parsing data.";
    }

    public static class ResponseMessages
    {
        public const string DataFromCache = "Data from cache.";
        public const string DataFetchedSuccessfully = "Data fetched successfully.";
        public const string ParsingError = "Parsing error";
    }
    #endregion

    #region AppSettings
    public static class AppSettings
    {
        public static int ParallelThreshold { get; private set; } = 10000;
        public static double CacheDurationMinutes { get; private set; } = 5;
        public static double HttpTimeout { get; private set; } = 30;

        public static string DataUrl { get; private set; }
            = "https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048/kings";
    }
    #endregion

    #region Models
    public class BaseResponse<T>
    {
        public bool Status { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }

    public class Monarch
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("nm")]
        public string Name { get; set; }

        [JsonProperty("cty")]
        public string Country { get; set; }

        [JsonProperty("hse")]
        public string House { get; set; }

        [JsonProperty("yrs")]
        public string YrsRaw { get; set; }

        // Calculated fields
        public int StartYear { get; set; }
        public int EndYear { get; set; }
    }
    #endregion

    #region Logger
    public static class Logger
    {
        public static void LogInfo(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[INFO] {msg}");
            Console.ResetColor();
        }

        public static void LogWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {msg}");
            Console.ResetColor();
        }

        public static void LogError(string msg, Exception ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {msg}");
            if (ex != null)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            Console.ResetColor();
        }
    }
    #endregion

    #region Caching Interface + Implementation

    /// <summary>
    /// A simple interface that defines caching operations for monarchs.
    /// </summary>
    public interface IMonarchCacheService
    {
        bool TryGetCache(out List<Monarch> monarchs);
        void StoreCache(List<Monarch> monarchs);
    }

    /// <summary>
    /// Implementation using an in-memory static collection.
    /// </summary>
    public class MonarchCacheService : IMonarchCacheService
    {
        private static List<Monarch> _cache;
        private static DateTime _lastFetch;
        private static TimeSpan CacheDuration => TimeSpan.FromMinutes(AppSettings.CacheDurationMinutes);

        public bool TryGetCache(out List<Monarch> monarchs)
        {
            if (_cache != null && (DateTime.Now - _lastFetch) < CacheDuration)
            {
                monarchs = _cache;
                return true;
            }
            monarchs = null;
            return false;
        }

        public void StoreCache(List<Monarch> monarchs)
        {
            _cache = monarchs;
            _lastFetch = DateTime.Now;
        }
    }

    #endregion

    #region Repository & Service Interfaces
    public interface IMonarchRepository
    {
        Task<BaseResponse<List<Monarch>>> FetchMonarchsAsync(string url);
    }

    public interface IMonarchService
    {
        Task<BaseResponse<List<Monarch>>> GetMonarchsAsync(string url);
        int GetTotalMonarchCount(List<Monarch> monarchs);
        (string MonarchName, int Duration) GetLongestRulingMonarch(List<Monarch> monarchs);
        (string House, int Duration) GetLongestRulingHouse(List<Monarch> monarchs);
        string GetMostCommonFirstName(List<Monarch> monarchs);
    }
    #endregion

    #region Repository Implementation
    public class MonarchRepository : IMonarchRepository
    {
        private readonly HttpClient _httpClient;

        public MonarchRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BaseResponse<List<Monarch>>> FetchMonarchsAsync(string url)
        {
            Logger.LogInfo(LogMessages.FetchingDataFromRemote);
            try
            {
                using var responseStream = await _httpClient.GetStreamAsync(url);
                var allMonarchs = ReadMonarchsFromStream(responseStream).ToList();

                return new BaseResponse<List<Monarch>>
                {
                    Status = true,
                    Data = allMonarchs,
                    Message = ResponseMessages.DataFetchedSuccessfully
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(LogMessages.ErrorWhileFetching, ex);
                return new BaseResponse<List<Monarch>>
                {
                    Status = false,
                    Data = null,
                    Message = ResponseMessages.ParsingError
                };
            }
        }

        private IEnumerable<Monarch> ReadMonarchsFromStream(Stream stream)
        {
            using var sr = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(sr);
            var serializer = new Newtonsoft.Json.JsonSerializer();

            if (jsonReader.Read() && jsonReader.TokenType == JsonToken.StartArray)
            {
                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var monarch = serializer.Deserialize<Monarch>(jsonReader);
                        if (monarch != null)
                        {
                            monarch.ParseYears();
                            yield return monarch;
                        }
                    }
                }
            }
            else
            {
                Logger.LogWarning("Fetched data is not in the expected JSON array format.");
            }
        }
    }
    #endregion

    #region Service Implementation
    public class MonarchService : IMonarchService
    {
        private readonly IMonarchRepository _repo;
        private readonly IMonarchCacheService _cacheService;

        public MonarchService(IMonarchRepository repo, IMonarchCacheService cacheService)
        {
            _repo = repo;
            _cacheService = cacheService;
        }

        public async Task<BaseResponse<List<Monarch>>> GetMonarchsAsync(string url)
        {
            // 1) Check cache first
            if (_cacheService.TryGetCache(out var cachedMonarchs))
            {
                Logger.LogInfo(LogMessages.DataRetrievedFromCache);
                return new BaseResponse<List<Monarch>>
                {
                    Status = true,
                    Data = cachedMonarchs,
                    Message = ResponseMessages.DataFromCache
                };
            }

            // 2) If not in cache, fetch from repository
            var repoResponse = await _repo.FetchMonarchsAsync(url);
            repoResponse.Data = repoResponse.Data.Where(x => x.House != "Commonwealth").ToList();
            if (repoResponse.Status && repoResponse.Data != null)
            {
                _cacheService.StoreCache(repoResponse.Data);
            }

            return repoResponse;
        }

        public int GetTotalMonarchCount(List<Monarch> monarchs)
        {
            if (monarchs.Count > AppSettings.ParallelThreshold)
                return monarchs.AsParallel().Count();
            else
                return monarchs.Count();
        }

        public (string MonarchName, int Duration) GetLongestRulingMonarch(List<Monarch> monarchs)
        {
            IEnumerable<Monarch> query = (monarchs.Count > AppSettings.ParallelThreshold)
                ? monarchs.AsParallel()
                : monarchs;

            var longest = query
                .OrderByDescending(m => (m.EndYear - m.StartYear))
                .FirstOrDefault();

            if (longest == null) return ("None", 0);

            int duration = longest.EndYear - longest.StartYear;
            return (longest.Name, duration);
        }

        public (string House, int Duration) GetLongestRulingHouse(List<Monarch> monarchs)
        {
            IEnumerable<Monarch> query = (monarchs.Count > AppSettings.ParallelThreshold)
                ? monarchs.AsParallel()
                : monarchs;

            var houseResult = query
                .GroupBy(m => m.House)
                .Select(g => new
                {
                    House = g.Key,
                    Duration = g.Sum(x => x.EndYear - x.StartYear)
                })
                .OrderByDescending(x => x.Duration)
                .FirstOrDefault();

            if (houseResult == null)
                return ("None", 0);

            return (houseResult.House, houseResult.Duration);
        }

        public string GetMostCommonFirstName(List<Monarch> monarchs)
        {
            IEnumerable<Monarch> query = (monarchs.Count > AppSettings.ParallelThreshold)
                ? monarchs.AsParallel()
                : monarchs;

            return query
                .Select(m => m.Name.Split(' ')[0])
                .GroupBy(first => first)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()
                ?.Key;
        }
    }
    #endregion

    #region Extensions
    public static class MonarchExtensions
    {
        public static void ParseYears(this Monarch monarch)
        {
            if (string.IsNullOrWhiteSpace(monarch.YrsRaw))
            {
                monarch.StartYear = 0;
                monarch.EndYear = 0;
                return;
            }

            var parts = monarch.YrsRaw.Split('-');
            if (parts.Length == 1)
            {
                if (int.TryParse(parts[0], out var singleYear))
                {
                    monarch.StartYear = singleYear;
                    monarch.EndYear = singleYear;
                }
                else
                {
                    monarch.StartYear = 0;
                    monarch.EndYear = 0;
                }
            }
            else
            {
                monarch.StartYear = parts[0].TryParseOrZero();
                monarch.EndYear = string.IsNullOrWhiteSpace(parts[1])
                    ? DateTime.Now.Year
                    : parts[1].TryParseOrZero();
            }
        }

        private static int TryParseOrZero(this string input)
        {
            return int.TryParse(input, out var val) ? val : 0;
        }
    }
    #endregion
}
