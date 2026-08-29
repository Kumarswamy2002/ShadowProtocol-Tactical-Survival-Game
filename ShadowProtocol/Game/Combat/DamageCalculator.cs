using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Combat;

public enum HitZone
{
    Head,
    Torso,
    Limbs
}

public readonly record struct HitInfo(
    Vector3F Point,
    Vector3F Normal,
    HitZone Zone,
    float Distance,
    object? HitTarget
);

public static class DamageCalculator
{
    public static float CalculateDamage(
        float baseDamage,
        float maxRange,
        float actualDistance,
        HitZone hitZone,
        float armorPenetration,
        float targetArmor,
        float critChance = 0.1f,
        float critMultiplier = 1.5f,
        bool forceCrit = false
    )
    {
        // 1. Distance Falloff (Linear beyond 60% of max range)
        float falloffFactor = 1.0f;
        float falloffStart = maxRange * 0.6f;
        if (actualDistance > falloffStart)
        {
            float t = Math.Clamp((actualDistance - falloffStart) / Math.Max(1f, maxRange - falloffStart), 0f, 1f);
            falloffFactor = Math.Max(0.2f, 1.0f - (t * 0.7f));
        }

        float damage = baseDamage * falloffFactor;

        // 2. Hit Zone Multipliers
        float zoneMultiplier = hitZone switch
        {
            HitZone.Head => 2.5f,
            HitZone.Torso => 1.0f,
            HitZone.Limbs => 0.75f,
            _ => 1.0f
        };
        damage *= zoneMultiplier;

        // 3. Critical Damage
        bool isCritical = forceCrit || (Random.Shared.NextDouble() < critChance);
        if (isCritical)
        {
            damage *= critMultiplier;
        }

        // 4. Armor Mitigation vs Armor Penetration
        float effectiveArmor = Math.Max(0f, targetArmor * (1.0f - Math.Clamp(armorPenetration, 0f, 1f)));
        float mitigationRatio = effectiveArmor / (effectiveArmor + 100f);
        damage *= (1.0f - mitigationRatio);

        return Math.Max(1.0f, damage);
    }
}
