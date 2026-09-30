using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<WeatherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/comfort-index", async (WeatherService weatherService) =>
{
    var result = await weatherService.GetRankedCitiesAsync();
    return Results.Ok(result);
});

app.MapGet("/api/cache/status", (WeatherService weatherService, IMemoryCache cache) =>
{
    var cities = weatherService.LoadCities();
    var status = cities.Select(c => new
    {
        c.CityName,
        c.CityCode,
        Cached = cache.TryGetValue($"weather_{c.CityCode}", out _) ? "HIT" : "MISS"
    });
    return Results.Ok(status);
});

app.Run();