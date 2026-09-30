# City Comfort Index

A full-stack weather analytics app that fetches live weather data for cities around the world, computes a custom "Comfort Index" score for each one, and displays them ranked from most to least comfortable.

Built with ASP.NET Core (C#) and a plain HTML/CSS/JavaScript frontend.

## Live Demo

*(Add your deployed link here if you host it, e.g. on Azure/Render/Railway)*

## Features

- Fetches live weather data for 8 cities from the [OpenWeatherMap API](https://openweathermap.org/api)
- Computes a custom Comfort Index score (0–100) per city, based on temperature, humidity, and wind speed
- Ranks cities from most to least comfortable
- Server-side caching (5-minute TTL) to reduce redundant API calls
- Debug endpoint to inspect cache HIT/MISS status per city
- Responsive dashboard (desktop + mobile)

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10, Minimal API) |
| Data source | OpenWeatherMap REST API |
| Caching | `IMemoryCache` (in-memory, server-side) |
| Frontend | Plain HTML, CSS (Grid/Flexbox), vanilla JavaScript |
| JSON handling | `System.Text.Json` |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A free [OpenWeatherMap API key](https://openweathermap.org/api)

### Setup

1. Clone the repository:
   ```bash
   git clone <your-repo-url>
   cd ComfortIndexApi
   ```

2. Add your API key. Create a file called `appsettings.Development.json` in the project root:
   ```json
   {
     "OpenWeatherMap": {
       "ApiKey": "your-api-key-here"
     }
   }
   ```
   > This file is gitignored — never commit your real API key.

3. Run the app:
   ```bash
   dotnet run
   ```

4. Open your browser to `http://localhost:5132` (check the terminal output for the exact port).

### API Endpoints

| Endpoint | Description |
|---|---|
| `GET /api/comfort-index` | Returns all cities ranked by Comfort Index score |
| `GET /api/cache/status` | Debug endpoint — shows HIT/MISS cache status per city |

## The Comfort Index Formula

Rather than a simple linear scale, the formula scores each factor against an **ideal range** — values inside the range score 100, and the score decreases the further a value strays outside it in either direction. This reflects how comfort actually works: it's rarely "more is always better," it's usually "there's a sweet spot."

**Ideal ranges chosen:**

| Factor | Ideal Range | Weight | Penalty per unit outside range |
|---|---|---|---|
| Temperature | 22–27°C | 40% | 4 points |
| Humidity | 40–60% | 30% | 2 points |
| Wind Speed | 5–15 km/h | 30% | 3 points |

**Final score:**
```
ComfortIndex = (TempScore × 0.4) + (HumidityScore × 0.3) + (WindScore × 0.3)
```

Each factor's score is calculated as:
```
if value is within ideal range:
    score = 100
else:
    distance = how far outside the range the value is
    score = max(0, 100 - (distance × penaltyPerUnit))
```

### Reasoning behind the weights

- **Temperature (40%)** is weighted highest because it's typically the single biggest driver of how weather actually *feels* — a pleasant temperature can offset less-than-ideal humidity or wind, but extreme heat or cold dominates the overall impression regardless of other conditions.
- **Humidity (30%)** meaningfully changes how temperature is perceived (high humidity makes heat feel worse, low humidity can make cold feel sharper), so it earns a substantial but secondary weight.
- **Wind speed (30%)** affects comfort similarly — a light breeze is pleasant, but too much wind (or none at all in hot conditions) detracts from comfort. Given equal secondary importance to humidity.

Wind speed from the API is provided in m/s and is converted to km/h (`× 3.6`) before scoring, to match the more intuitive km/h range most people think in.

## Trade-offs Considered

- **Ideal-range scoring vs. simple linear scoring**: A simpler formula (e.g., "higher temperature = lower score") was considered but rejected, since it doesn't reflect that both very hot *and* very cold are uncomfortable. The range-based approach is more realistic but requires more parameters (min, max, and penalty rate) to tune per factor.
- **Equal vs. unequal weights**: Weights were chosen based on subjective judgment of what most affects perceived comfort, not derived from any external dataset. A future improvement could validate these weights against real comfort survey data.
- **Caching entire responses vs. individual fields**: The full raw OpenWeatherMap response is cached per city, rather than pre-computed scores. This trades a small amount of recomputation (the formula re-runs on cache hits) for simplicity and flexibility (the formula can change without needing to invalidate cache).

## Cache Design

- Each city's raw weather response is cached under a key like `weather_{cityCode}`, with a 5-minute expiration (`IMemoryCache`, in-process).
- A 5-minute TTL balances data freshness against OpenWeatherMap's free-tier rate limits — weather conditions don't meaningfully change minute-to-minute, so this avoids redundant calls without serving significantly stale data.
- The `/api/cache/status` endpoint reports HIT/MISS per city for debugging and demonstration purposes.
- Cache is in-memory only — it resets whenever the server restarts. This is acceptable for a single-instance demo app but would need a distributed cache (e.g., Redis) for a multi-instance production deployment.

## Known Limitations

- **In-memory cache** doesn't persist across restarts or scale across multiple server instances.
- **Dew point** isn't used in the formula — OpenWeatherMap's free tier doesn't return it directly, and it would require an additional calculation from temperature and humidity that wasn't prioritized for this version.
- **Formula weights are subjective**, based on reasoned judgment rather than empirical comfort data.
- **No persistent storage** — city list is loaded fresh from `cities.json` on every request; there's no database layer.
- **No authentication** — the dashboard is currently open to anyone with the URL.

## Possible Future Improvements

- Add dark mode toggle
- Add a temperature trend chart per city
- Frontend sorting/filtering controls
- Unit tests for `CalculateComfortIndex`
- Persist cache with Redis for multi-instance deployments
