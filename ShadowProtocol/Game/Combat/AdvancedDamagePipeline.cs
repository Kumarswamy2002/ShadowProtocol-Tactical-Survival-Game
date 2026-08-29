using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Core.Logging;

namespace ShadowProtocol.Game.Combat
{
    /// <summary>
    /// Supported elemental and tactical damage archetypes in Shadow Protocol.
    /// </summary>
    public enum DamageType
    {
        Kinetic,
        Thermal,
        Cryo,
        Electromagnetic,
        Toxic,
        Corrosive,
        Blast,
        DirectEnergy,
        TrueDamage
    }

    /// <summary>
    /// Hitzone categories on human and mechanical targets.
    /// </summary>
    public enum HitZone
    {
        Head,
        Neck,
        UpperTorso,
        LowerTorso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
        WeakpointEngine,
        ArmorPlateFront,
        ArmorPlateRear
    }

    /// <summary>
    /// Encapsulates initial damage payload before mitigation calculations.
    /// </summary>
    public class DamagePayload
    {
        public string SourceEntityId { get; set; }
        public string TargetEntityId { get; set; }
        public string WeaponId { get; set; }
        public DamageType Type { get; set; }
        public HitZone Zone { get; set; }
        public float RawDamage { get; set; }
        public float ArmorPenetrationFactor { get; set; }
        public float CriticalMultiplier { get; set; }
        public bool IsCriticalHit { get; set; }
        public float ImpulseForce { get; set; }
        public Vector3F HitLocation { get; set; }
        public Vector3F HitNormal { get; set; }
        public float DistanceMeters { get; set; }
        public float StatusApplicationChance { get; set; }
        public float StatusDurationSeconds { get; set; }

        public DamagePayload(string sourceId, string targetId, float damage, DamageType type, HitZone zone)
        {
            SourceEntityId = sourceId;
            TargetEntityId = targetId;
            RawDamage = damage;
            Type = type;
            Zone = zone;
            ArmorPenetrationFactor = 0.25f;
            CriticalMultiplier = 1.5f;
            IsCriticalHit = false;
            ImpulseForce = 10f;
            HitLocation = Vector3F.Zero;
            HitNormal = Vector3F.Up;
            DistanceMeters = 10f;
            StatusApplicationChance = 0.2f;
            StatusDurationSeconds = 5f;
        }
    }

    /// <summary>
    /// Resulting state after running damage calculations through all mitigation stages.
    /// </summary>
    public class DamageMitigationResult
    {
        public DamagePayload OriginalPayload { get; set; }
        public float FinalHealthDamage { get; set; }
        public float FinalShieldDamage { get; set; }
        public float ArmorDamageAbsorbed { get; set; }
        public float ArmorDurabilityDamage { get; set; }
        public bool WasArmorBroken { get; set; }
        public bool TargetWasKilled { get; set; }
        public bool AppliedStatusEffect { get; set; }
        public string StatusEffectName { get; set; }
        public float OverkillDamage { get; set; }
        public List<string> MitigationLog { get; } = new List<string>();

        public void LogStep(string message)
        {
            MitigationLog.Add(message);
        }
    }

    /// <summary>
    /// Defense statistics and resistances of a combatant.
    /// </summary>
    public class TargetDefenseProfile
    {
        public float CurrentHealth { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public float CurrentShield { get; set; } = 50f;
        public float MaxShield { get; set; } = 50f;
        public float ShieldRechargeDelay { get; set; } = 3.5f;
        public float ShieldRechargeRate { get; set; } = 15f;
        public float ArmorPlatingRating { get; set; } = 40f;
        public float ArmorDurability { get; set; } = 100f;
        public float MaxArmorDurability { get; set; } = 100f;

        private readonly Dictionary<DamageType, float> _resistances = new Dictionary<DamageType, float>();
        private readonly Dictionary<HitZone, float> _zoneMultipliers = new Dictionary<HitZone, float>();

        public TargetDefenseProfile()
        {
            InitializeResistances();
            InitializeZoneMultipliers();
        }

        private void InitializeResistances()
        {
            _resistances[DamageType.Kinetic] = 0.15f;
            _resistances[DamageType.Thermal] = 0.10f;
            _resistances[DamageType.Cryo] = 0.05f;
            _resistances[DamageType.Electromagnetic] = -0.25f; // Vulnerable to EMP
            _resistances[DamageType.Toxic] = 0.20f;
            _resistances[DamageType.Corrosive] = -0.10f;
            _resistances[DamageType.Blast] = 0.30f;
            _resistances[DamageType.DirectEnergy] = 0.0f;
            _resistances[DamageType.TrueDamage] = 0.0f;
        }

        private void InitializeZoneMultipliers()
        {
            _zoneMultipliers[HitZone.Head] = 2.5f;
            _zoneMultipliers[HitZone.Neck] = 1.8f;
            _zoneMultipliers[HitZone.UpperTorso] = 1.0f;
            _zoneMultipliers[HitZone.LowerTorso] = 0.9f;
            _zoneMultipliers[HitZone.LeftArm] = 0.7f;
            _zoneMultipliers[HitZone.RightArm] = 0.7f;
            _zoneMultipliers[HitZone.LeftLeg] = 0.65f;
            _zoneMultipliers[HitZone.RightLeg] = 0.65f;
            _zoneMultipliers[HitZone.WeakpointEngine] = 3.5f;
            _zoneMultipliers[HitZone.ArmorPlateFront] = 0.4f;
            _zoneMultipliers[HitZone.ArmorPlateRear] = 0.5f;
        }

        public float GetResistance(DamageType type)
        {
            return _resistances.TryGetValue(type, out float res) ? Math.Clamp(res, -1.0f, 0.95f) : 0.0f;
        }

        public void SetResistance(DamageType type, float value)
        {
            _resistances[type] = Math.Clamp(value, -1.0f, 0.95f);
        }

        public float GetZoneMultiplier(HitZone zone)
        {
            return _zoneMultipliers.TryGetValue(zone, out float mult) ? mult : 1.0f;
        }
    }

    /// <summary>
    /// Pipeline orchestrator for calculating realistic, deterministic damage mitigation.
    /// </summary>
    public class AdvancedDamagePipeline
    {
        private static readonly Random _rng = new Random(42);

        public static DamageMitigationResult ExecuteDamagePipeline(DamagePayload payload, TargetDefenseProfile target)
        {
            var result = new DamageMitigationResult { OriginalPayload = payload };

            float currentDamage = payload.RawDamage;
            result.LogStep($"[Pipeline] Initial raw damage: {currentDamage:F2} ({payload.Type}) on {payload.Zone}");

            // 1. Distance Falloff
            currentDamage = ApplyDistanceFalloff(currentDamage, payload.DistanceMeters);
            result.LogStep($"[Pipeline] After distance falloff ({payload.DistanceMeters:F1}m): {currentDamage:F2}");

            // 2. Hitzone and Critical Hit Processing
            float zoneMult = target.GetZoneMultiplier(payload.Zone);
            if (payload.IsCriticalHit)
            {
                zoneMult *= payload.CriticalMultiplier;
                result.LogStep($"[Pipeline] Critical Hit triggered! Multiplier: {payload.CriticalMultiplier:F2}");
            }
            currentDamage *= zoneMult;
            result.LogStep($"[Pipeline] After zone ({payload.Zone} x{zoneMult:F2}): {currentDamage:F2}");

            // 3. Shield Absorption
            if (target.CurrentShield > 0f && payload.Type != DamageType.Toxic && payload.Type != DamageType.TrueDamage)
            {
                float shieldMod = (payload.Type == DamageType.Electromagnetic) ? 2.0f : 1.0f;
                float effectiveShieldDamage = currentDamage * shieldMod;

                if (target.CurrentShield >= effectiveShieldDamage)
                {
                    target.CurrentShield -= effectiveShieldDamage;
                    result.FinalShieldDamage = effectiveShieldDamage;
                    currentDamage = 0f;
                    result.LogStep($"[Pipeline] Shield absorbed all damage. Remaining shield: {target.CurrentShield:F2}");
                }
                else
                {
                    float absorbed = target.CurrentShield;
                    float excessRatio = (effectiveShieldDamage - absorbed) / effectiveShieldDamage;
                    result.FinalShieldDamage = absorbed;
                    target.CurrentShield = 0f;
                    currentDamage *= excessRatio;
                    result.LogStep($"[Pipeline] Shield broke! Spillover damage remaining: {currentDamage:F2}");
                }
            }

            if (currentDamage <= 0f)
            {
                return result;
            }

            // 4. Resistance Mitigation
            float resistance = target.GetResistance(payload.Type);
            float mitigatedByResistance = currentDamage * (1.0f - resistance);
            result.LogStep($"[Pipeline] Resistance ({resistance * 100:F1}%): {currentDamage:F2} -> {mitigatedByResistance:F2}");
            currentDamage = mitigatedByResistance;

            // 5. Armor Plating Absorption
            if (target.ArmorDurability > 0f && payload.Type != DamageType.TrueDamage)
            {
                float effectiveArmor = target.ArmorPlatingRating * (1.0f - payload.ArmorPenetrationFactor);
                effectiveArmor = Math.Max(0f, effectiveArmor);

                float armorAbsorptionFraction = effectiveArmor / (effectiveArmor + 100f);
                float damageAbsorbed = currentDamage * armorAbsorptionFraction;
                float damagePassingThrough = currentDamage - damageAbsorbed;

                // Degrade armor durability
                float armorDamage = damageAbsorbed * 0.5f;
                if (payload.Type == DamageType.Corrosive) armorDamage *= 2.5f;
                target.ArmorDurability = Math.Max(0f, target.ArmorDurability - armorDamage);
                result.ArmorDurabilityDamage = armorDamage;
                result.ArmorDamageAbsorbed = damageAbsorbed;

                if (target.ArmorDurability <= 0f)
                {
                    result.WasArmorBroken = true;
                    result.LogStep("[Pipeline] Target armor plating has been shattered!");
                }

                currentDamage = damagePassingThrough;
                result.LogStep($"[Pipeline] Armor absorbed: {damageAbsorbed:F2}, Pass-through: {damagePassingThrough:F2}");
            }

            // 6. Final Health Application
            result.FinalHealthDamage = currentDamage;
            if (target.CurrentHealth <= currentDamage)
            {
                result.OverkillDamage = currentDamage - target.CurrentHealth;
                target.CurrentHealth = 0f;
                result.TargetWasKilled = true;
                result.LogStep($"[Pipeline] Fatal blow dealt! Target eliminated (Overkill: {result.OverkillDamage:F2})");
            }
            else
            {
                target.CurrentHealth -= currentDamage;
                result.LogStep($"[Pipeline] Health reduced to: {target.CurrentHealth:F2} / {target.MaxHealth:F2}");
            }

            // 7. Status Effect Evaluation
            if (!result.TargetWasKilled && payload.StatusApplicationChance > 0f)
            {
                double roll = _rng.NextDouble();
                if (roll <= payload.StatusApplicationChance)
                {
                    result.AppliedStatusEffect = true;
                    result.StatusEffectName = DetermineStatusEffect(payload.Type);
                    result.LogStep($"[Pipeline] Applied status effect: {result.StatusEffectName} for {payload.StatusDurationSeconds:F1}s");
                }
            }

            return result;
        }

        private static float ApplyDistanceFalloff(float baseDamage, float distanceMeters)
        {
            if (distanceMeters <= 15f) return baseDamage;
            if (distanceMeters >= 100f) return baseDamage * 0.45f;
            float t = (distanceMeters - 15f) / 85f;
            return baseDamage * MathUtils.Lerp(1.0f, 0.45f, t);
        }

        private static string DetermineStatusEffect(DamageType type)
        {
            return type switch
            {
                DamageType.Thermal => "Ignited",
                DamageType.Cryo => "FrozenStasis",
                DamageType.Electromagnetic => "SystemDisrupted",
                DamageType.Toxic => "NeurotoxinPoison",
                DamageType.Corrosive => "ArmorDegradation",
                DamageType.Blast => "ConcussiveStun",
                _ => "Suppressed"
            };
        }
    }
}
