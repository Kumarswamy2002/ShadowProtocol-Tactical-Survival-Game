using ShadowProtocol.Core.Events;

namespace ShadowProtocol.Game.Player;

public readonly record struct VitalsChangedEvent(
    float Health,
    float MaxHealth,
    float Stamina,
    float MaxStamina,
    float Armor,
    float MaxArmor,
    float Energy,
    float Hunger,
    float Hydration
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct PlayerDiedEvent(string Reason) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class PlayerVitals
{
    public float MaxHealth { get; private set; } = 100f;
    public float Health { get; private set; } = 100f;

    public float MaxStamina { get; private set; } = 100f;
    public float Stamina { get; private set; } = 100f;

    public float MaxArmor { get; private set; } = 100f;
    public float Armor { get; private set; } = 50f;

    public float MaxEnergy { get; private set; } = 100f;
    public float Energy { get; private set; } = 100f;

    public float MaxHunger { get; private set; } = 100f;
    public float Hunger { get; private set; } = 100f;

    public float MaxHydration { get; private set; } = 100f;
    public float Hydration { get; private set; } = 100f;

    public bool IsDead => Health <= 0f;
    public bool IsExhausted => Stamina <= 0.1f;
    public bool IsStarving => Hunger <= 0f;
    public bool IsDehydrated => Hydration <= 0f;

    // Rates per second
    public float StaminaRecoveryRate { get; set; } = 15f;
    public float HungerDepletionRate { get; set; } = 0.05f;
    public float HydrationDepletionRate { get; set; } = 0.1f;
    public float StarvationDamageRate { get; set; } = 1.0f;
    public float DehydrationDamageRate { get; set; } = 1.5f;

    private readonly IEventBus? _eventBus;

    public PlayerVitals(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
    }

    public void ModifyHealth(float delta, string reason = "Damage")
    {
        if (IsDead && delta < 0) return;

        Health = Math.Clamp(Health + delta, 0f, MaxHealth);
        NotifyVitalsChanged();

        if (IsDead)
        {
            _eventBus?.Publish(new PlayerDiedEvent(reason));
        }
    }

    public void ApplyDamage(float damage, float armorPenetration = 0f)
    {
        if (damage <= 0f || IsDead) return;

        float effectiveArmor = Math.Max(0f, Armor * (1f - Math.Clamp(armorPenetration, 0f, 1f)));
        float armorDamage = Math.Min(effectiveArmor, damage * 0.7f);
        float directDamage = damage - armorDamage;

        Armor = Math.Max(0f, Armor - (armorDamage * 0.5f));
        ModifyHealth(-directDamage, "Combat Damage");
    }

    public void ModifyArmor(float delta)
    {
        Armor = Math.Clamp(Armor + delta, 0f, MaxArmor);
        NotifyVitalsChanged();
    }

    public bool ConsumeStamina(float amount)
    {
        if (Stamina < amount)
        {
            return false;
        }
        Stamina = Math.Max(0f, Stamina - amount);
        NotifyVitalsChanged();
        return true;
    }

    public void RestoreStamina(float amount)
    {
        Stamina = Math.Clamp(Stamina + amount, 0f, MaxStamina);
        NotifyVitalsChanged();
    }

    public void ModifyHunger(float delta)
    {
        Hunger = Math.Clamp(Hunger + delta, 0f, MaxHunger);
        NotifyVitalsChanged();
    }

    public void ModifyHydration(float delta)
    {
        Hydration = Math.Clamp(Hydration + delta, 0f, MaxHydration);
        NotifyVitalsChanged();
    }

    public void ModifyEnergy(float delta)
    {
        Energy = Math.Clamp(Energy + delta, 0f, MaxEnergy);
        NotifyVitalsChanged();
    }

    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = Math.Max(1f, maxHealth);
        Health = Math.Min(Health, MaxHealth);
        NotifyVitalsChanged();
    }

    public void SetMaxStamina(float maxStamina)
    {
        MaxStamina = Math.Max(1f, maxStamina);
        Stamina = Math.Min(Stamina, MaxStamina);
        NotifyVitalsChanged();
    }

    public void UpdateSurvivalTick(float deltaTime, bool isExerting = false)
    {
        if (IsDead) return;

        // Deplete hunger & hydration
        ModifyHunger(-HungerDepletionRate * deltaTime * (isExerting ? 1.5f : 1.0f));
        ModifyHydration(-HydrationDepletionRate * deltaTime * (isExerting ? 2.0f : 1.0f));

        // Natural stamina regen when not exerting
        if (!isExerting && Stamina < MaxStamina)
        {
            float regenModifier = (Hunger > 50f && Hydration > 50f) ? 1.0f : 0.5f;
            RestoreStamina(StaminaRecoveryRate * regenModifier * deltaTime);
        }

        // Damage from extreme deprivation
        if (IsStarving)
        {
            ModifyHealth(-StarvationDamageRate * deltaTime, "Starvation");
        }
        if (IsDehydrated)
        {
            ModifyHealth(-DehydrationDamageRate * deltaTime, "Dehydration");
        }
    }

    private void NotifyVitalsChanged()
    {
        _eventBus?.Publish(new VitalsChangedEvent(
            Health, MaxHealth, Stamina, MaxStamina, Armor, MaxArmor, Energy, Hunger, Hydration
        ));
    }
}
