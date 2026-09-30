using System;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;


public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IMemoryCache _cache;

    public WeatherService(HttpClient httpClient, IConfiguration config, IMemoryCache cache)
    {
        _httpClient = httpClient;
           _cache = cache;
        _apiKey = config["OpenWeatherMap:ApiKey"]
            ?? throw new Exception("Missing OpenWeatherMap API key in appsettings.Development.json");
    }

    // ---- Records for cities.json ----

    public record CityInfo
    (
        string CityCode,
        string CityName,
        string Temp,
        string Status
    );

    public record CityListWrapper
    (
        List<CityInfo> List
    );

    // ---- Records for OpenWeatherMap's response ----

    public record WeatherEntry(string Main, string Description);
    public record MainInfo(double Temp, int Humidity);
    public record WindInfo(double Speed);
    public record CloudInfo(int All);

    public record OpenWeatherResponse
    (
        List<WeatherEntry> Weather,
        MainInfo Main,
        WindInfo Wind,
        CloudInfo Clouds
    );

    // ---- Load cities from the local JSON file ----

    public List<CityInfo> LoadCities()
    {
        string filePath = "cities.json";
        string jsonText = File.ReadAllText(filePath);

        CityListWrapper? wrapper = JsonSerializer.Deserialize<CityListWrapper>(jsonText);

        if (wrapper == null || wrapper.List == null)
        {
            throw new Exception("Failed to load or parse cities.json");
        }

        return wrapper.List;
    }

    // ---- Call OpenWeatherMap for one city ----

    public async Task<OpenWeatherResponse> GetWeatherAsync(string cityCode)
    {
        string cacheKey = $"weather_{cityCode}";

    if (_cache.TryGetValue(cacheKey, out OpenWeatherResponse? cached) && cached != null)
    {
        return cached;
    }

    string url = $"https://api.openweathermap.org/data/2.5/weather?id={cityCode}&appid={_apiKey}&units=metric";
    string jsonText = await _httpClient.GetStringAsync(url);

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    OpenWeatherResponse? result = JsonSerializer.Deserialize<OpenWeatherResponse>(jsonText, options);

    if (result == null)
    {
        throw new Exception($"Failed to parse weather data for city {cityCode}");
    }

    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }


    public static double CalculateComfortIndex(double tempC, int humidity, double windSpeedMs)
{
    double windKmh = windSpeedMs * 3.6;

    double tempScore = ScoreWithinRange(tempC, 22, 27, penaltyPerUnit: 4);
    double humidityScore = ScoreWithinRange(humidity, 40, 60, penaltyPerUnit: 2);
    double windScore = ScoreWithinRange(windKmh, 5, 15, penaltyPerUnit: 3);

    double comfortIndex = (tempScore * 0.4) + (humidityScore * 0.3) + (windScore * 0.3);

    return Math.Round(comfortIndex, 1);
}

private static double ScoreWithinRange(double value, double idealMin, double idealMax, double penaltyPerUnit)
{
    if (value >= idealMin && value <= idealMax)
    {
        return 100;
    }

    double distanceOutside = value < idealMin ? idealMin - value : value - idealMax;
    double score = 100 - (distanceOutside * penaltyPerUnit);

    return Math.Max(0, score);
}

public record ComfortResult(string CityName, string Description, double TempCelsius, double ComfortScore, int Rank);

public async Task<List<ComfortResult>> GetRankedCitiesAsync()
{
    List<CityInfo> cities = LoadCities();
    var results = new List<ComfortResult>();

    foreach (var city in cities)
    {
        var weather = await GetWeatherAsync(city.CityCode);
        double score = CalculateComfortIndex(weather.Main.Temp, weather.Main.Humidity, weather.Wind.Speed);

        results.Add(new ComfortResult(
            city.CityName,
            weather.Weather[0].Description,
            weather.Main.Temp,
            score,
            0 // rank assigned below
        ));
    }

    var ranked = results
        .OrderByDescending(r => r.ComfortScore)
        .Select((r, index) => r with { Rank = index + 1 })
        .ToList();

    return ranked;
}
}