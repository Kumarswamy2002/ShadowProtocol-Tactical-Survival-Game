using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.World;

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    HeavyRain,
    Fog,
    Storm
}

public enum WorldRegion
{
    OldCity,
    IndustrialDistrict,
    AbandonedHighway,
    SurvivorSettlement,
    ForestRegion,
    MilitaryBase,
    UndergroundFacility,
    ResearchLaboratory,
    EnemyFortress,
    RestrictedZone
}

public readonly record struct WeatherChangedEvent(WeatherType NewWeather, float VisibilityMultiplier) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct TimeOfDayChangedEvent(TimeSpan CurrentTime, string PeriodName) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class WeatherSystem
{
    public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
    public float VisibilityMultiplier { get; private set; } = 1.0f;
    public float SoundPropagationMultiplier { get; private set; } = 1.0f;
    public float RoadGripMultiplier { get; private set; } = 1.0f;

    private readonly IEventBus? _eventBus;

    public WeatherSystem(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
        SetWeather(WeatherType.Clear);
    }

    public void SetWeather(WeatherType weather)
    {
        CurrentWeather = weather;
        switch (weather)
        {
            case WeatherType.Clear:
                VisibilityMultiplier = 1.0f;
                SoundPropagationMultiplier = 1.0f;
                RoadGripMultiplier = 1.0f;
                break;
            case WeatherType.Cloudy:
                VisibilityMultiplier = 0.9f;
                SoundPropagationMultiplier = 1.0f;
                RoadGripMultiplier = 1.0f;
                break;
            case WeatherType.Rain:
                VisibilityMultiplier = 0.75f;
                SoundPropagationMultiplier = 0.7f;
                RoadGripMultiplier = 0.8f;
                break;
            case WeatherType.HeavyRain:
                VisibilityMultiplier = 0.5f;
                SoundPropagationMultiplier = 0.4f;
                RoadGripMultiplier = 0.6f;
                break;
            case WeatherType.Fog:
                VisibilityMultiplier = 0.3f;
                SoundPropagationMultiplier = 0.85f;
                RoadGripMultiplier = 0.9f;
                break;
            case WeatherType.Storm:
                VisibilityMultiplier = 0.35f;
                SoundPropagationMultiplier = 0.3f;
                RoadGripMultiplier = 0.55f;
                break;
        }

        _eventBus?.Publish(new WeatherChangedEvent(CurrentWeather, VisibilityMultiplier));
    }
}

public class DayNightCycle
{
    public float TimeSpeedMultiplier { get; set; } = 60.0f; // 1 real sec = 1 in-game minute
    public TimeSpan CurrentTime { get; private set; } = TimeSpan.FromHours(8); // Start at 08:00 AM

    public string CurrentPeriodName => GetPeriodName(CurrentTime.Hours);
    public bool IsNight => CurrentTime.Hours is >= 20 or < 6;
    public float AmbientLightIntensity => CalculateAmbientLight(CurrentTime.Hours + (CurrentTime.Minutes / 60f));

    private readonly IEventBus? _eventBus;
    private int _lastReportedHour = -1;

    public DayNightCycle(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
    }

    public void SetTime(TimeSpan time)
    {
        CurrentTime = time;
    }

    public void Update(float deltaTime)
    {
        double secondsToAdd = deltaTime * TimeSpeedMultiplier;
        CurrentTime = CurrentTime.Add(TimeSpan.FromSeconds(secondsToAdd));
        if (CurrentTime.TotalDays >= 1)
        {
            CurrentTime = TimeSpan.FromSeconds(CurrentTime.TotalSeconds % 86400);
        }

        if (CurrentTime.Hours != _lastReportedHour)
        {
            _lastReportedHour = CurrentTime.Hours;
            _eventBus?.Publish(new TimeOfDayChangedEvent(CurrentTime, CurrentPeriodName));
        }
    }

    private static string GetPeriodName(int hour) => hour switch
    {
        >= 4 and < 6 => "Dawn",
        >= 6 and < 8 => "Sunrise",
        >= 8 and < 12 => "Morning",
        >= 12 and < 18 => "Afternoon",
        >= 18 and < 20 => "Sunset",
        >= 20 and < 24 => "Night",
        _ => "Midnight"
    };

    private static float CalculateAmbientLight(float hourFloat)
    {
        // Smooth sine-based lighting from 0.05 (night) to 1.0 (noon)
        if (hourFloat is < 5f or > 21f) return 0.08f;
        if (hourFloat is >= 10f and <= 15f) return 1.0f;
        if (hourFloat >= 5f && hourFloat < 10f) return 0.08f + ((hourFloat - 5f) / 5f * 0.92f);
        return 1.0f - ((hourFloat - 15f) / 6f * 0.92f);
    }
}

public class RegionDescriptor
{
    public WorldRegion Region { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DangerRating { get; set; } = 1; // 1 to 10
    public Vector3F Center { get; set; }
    public float Radius { get; set; } = 500f;
    public bool IsDiscovered { get; set; } = false;
}

public class WorldManager
{
    public WeatherSystem Weather { get; }
    public DayNightCycle DayNight { get; }
    public Dictionary<WorldRegion, RegionDescriptor> Regions { get; } = new();

    public WorldManager(IEventBus? eventBus = null)
    {
        Weather = new WeatherSystem(eventBus);
        DayNight = new DayNightCycle(eventBus);
        InitializeRegions();
    }

    public void Update(float deltaTime)
    {
        DayNight.Update(deltaTime);
    }

    public WorldRegion GetCurrentRegion(Vector3F position)
    {
        foreach (var region in Regions.Values)
        {
            if (Vector3F.Distance(position, region.Center) <= region.Radius)
            {
                return region.Region;
            }
        }
        return WorldRegion.OldCity;
    }

    public void DiscoverRegion(WorldRegion region)
    {
        if (Regions.TryGetValue(region, out var desc))
        {
            desc.IsDiscovered = true;
        }
    }

    private void InitializeRegions()
    {
        AddRegion(WorldRegion.OldCity, "Old City", "Ruined commercial center with crumbling skyscrapers", 2, new Vector3F(0, 0, 0));
        AddRegion(WorldRegion.IndustrialDistrict, "Industrial District", "Abandoned manufacturing warehouses and toxic refineries", 4, new Vector3F(600, 0, 0));
        AddRegion(WorldRegion.AbandonedHighway, "Abandoned Highway", "Choked transportation corridor with vehicle graveyard", 3, new Vector3F(-600, 0, 0));
        AddRegion(WorldRegion.SurvivorSettlement, "Survivor Settlement", "Fortified sanctuary with neutral factions and traders", 1, new Vector3F(0, 0, 600));
        AddRegion(WorldRegion.ForestRegion, "Forest Region", "Dense overgrown woodland surrounding the city perimeter", 3, new Vector3F(0, 0, -600));
        AddRegion(WorldRegion.MilitaryBase, "Military Base", "Directorate forward operating base with heavy security", 7, new Vector3F(800, 0, 800));
        AddRegion(WorldRegion.UndergroundFacility, "Underground Facility", "Subterranean bunker network beneath the ruins", 6, new Vector3F(-800, -20, 0));
        AddRegion(WorldRegion.ResearchLaboratory, "Research Laboratory", "Classified Directorate bio-tech compound", 8, new Vector3F(-800, 0, -800));
        AddRegion(WorldRegion.EnemyFortress, "Enemy Fortress", "Heavily defended citadel of the Directorate command", 9, new Vector3F(1200, 0, 0));
        AddRegion(WorldRegion.RestrictedZone, "Restricted Zone", "Ground Zero of the Blackout event. Lethal hazards", 10, new Vector3F(0, 0, 1500));
    }

    private void AddRegion(WorldRegion region, string name, string desc, int danger, Vector3F center)
    {
        Regions[region] = new RegionDescriptor
        {
            Region = region,
            DisplayName = name,
            Description = desc,
            DangerRating = danger,
            Center = center,
            Radius = 400f,
            IsDiscovered = region == WorldRegion.SurvivorSettlement
        };
    }
}
