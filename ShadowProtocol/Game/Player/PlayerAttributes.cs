using ShadowProtocol.Core.Events;

namespace ShadowProtocol.Game.Player;

public readonly record struct LevelUpEvent(int NewLevel, int SkillPointsAwarded) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct CurrencyChangedEvent(long NewBalance, long Delta) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class PlayerAttributes
{
    public int Level { get; private set; } = 1;
    public long Experience { get; private set; } = 0;
    public long ExperienceToNextLevel => CalculateExpRequirement(Level);

    public int AvailableSkillPoints { get; private set; } = 0;
    public long Currency { get; private set; } = 500; // Starting Credits

    public float BaseCarryWeight { get; set; } = 35.0f; // in kg
    public float BonusCarryWeight { get; set; } = 0.0f;
    public float MaxCarryWeight => BaseCarryWeight + BonusCarryWeight;

    public float MoveSpeedMultiplier { get; set; } = 1.0f;
    public float NoiseMultiplier { get; set; } = 1.0f;
    public float DamageMultiplier { get; set; } = 1.0f;
    public float ReloadSpeedMultiplier { get; set; } = 1.0f;

    private readonly IEventBus? _eventBus;

    public PlayerAttributes(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
    }

    public static long CalculateExpRequirement(int level)
    {
        return (long)(100 * MathF.Pow(level, 1.5f));
    }

    public void AddExperience(long amount)
    {
        if (amount <= 0) return;

        Experience += amount;
        while (Experience >= ExperienceToNextLevel)
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            AvailableSkillPoints += 2;
            _eventBus?.Publish(new LevelUpEvent(Level, 2));
        }
    }

    public bool SpendSkillPoints(int points)
    {
        if (points <= 0 || AvailableSkillPoints < points)
        {
            return false;
        }
        AvailableSkillPoints -= points;
        return true;
    }

    public void AddSkillPoints(int points)
    {
        if (points > 0)
        {
            AvailableSkillPoints += points;
        }
    }

    public void AddCurrency(long amount)
    {
        if (amount == 0) return;
        Currency = Math.Max(0, Currency + amount);
        _eventBus?.Publish(new CurrencyChangedEvent(Currency, amount));
    }

    public bool DeductCurrency(long amount)
    {
        if (amount <= 0 || Currency < amount)
        {
            return false;
        }
        Currency -= amount;
        _eventBus?.Publish(new CurrencyChangedEvent(Currency, -amount));
        return true;
    }
}
