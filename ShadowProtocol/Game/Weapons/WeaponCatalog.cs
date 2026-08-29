using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Weapons
{
    public enum WeaponCategory
    {
        Sidearm,
        SubmachineGun,
        AssaultRifle,
        DesignatedMarksmanRifle,
        SniperRifle,
        CombatShotgun,
        HeavyMachineGun,
        AntiMaterialRifle,
        TacticalOrdnance
    }

    public class WeaponDefinition
    {
        public string WeaponId { get; set; }
        public string DisplayName { get; set; }
        public WeaponCategory Category { get; set; }
        public float BaseDamage { get; set; }
        public float FireRateRoundsPerMinute { get; set; }
        public int MagazineCapacity { get; set; }
        public float ReloadTimeSeconds { get; set; }
        public float TacticalReloadTimeSeconds { get; set; }
        public float MuzzleVelocityMs { get; set; }
        public float EffectiveRangeMeters { get; set; }
        public float BaseAccuracySpreadMoa { get; set; }
        public float ErgonomicsScore { get; set; }
        public float BaseWeightKg { get; set; }
        public string DefaultCaliber { get; set; }
    }

    public static class WeaponCatalog
    {
        private static readonly Dictionary<string, WeaponDefinition> _weapons = new Dictionary<string, WeaponDefinition>();

        static WeaponCatalog()
        {
            Register(new WeaponDefinition
            {
                WeaponId = "wpn_akm_tactical",
                DisplayName = "Veyra AR-76 Tactical",
                Category = WeaponCategory.AssaultRifle,
                BaseDamage = 44f,
                FireRateRoundsPerMinute = 625f,
                MagazineCapacity = 30,
                ReloadTimeSeconds = 2.8f,
                TacticalReloadTimeSeconds = 2.2f,
                MuzzleVelocityMs = 715f,
                EffectiveRangeMeters = 350f,
                BaseAccuracySpreadMoa = 2.1f,
                ErgonomicsScore = 62f,
                BaseWeightKg = 3.6f,
                DefaultCaliber = "7.62x39mm"
            });

            Register(new WeaponDefinition
            {
                WeaponId = "wpn_m4_specops",
                DisplayName = "Directorate Spectre M4",
                Category = WeaponCategory.AssaultRifle,
                BaseDamage = 38f,
                FireRateRoundsPerMinute = 800f,
                MagazineCapacity = 30,
                ReloadTimeSeconds = 2.4f,
                TacticalReloadTimeSeconds = 1.9f,
                MuzzleVelocityMs = 880f,
                EffectiveRangeMeters = 400f,
                BaseAccuracySpreadMoa = 1.4f,
                ErgonomicsScore = 78f,
                BaseWeightKg = 3.1f,
                DefaultCaliber = "5.56x45mm NATO"
            });

            Register(new WeaponDefinition
            {
                WeaponId = "wpn_vector_smg",
                DisplayName = "Apex Vector-9",
                Category = WeaponCategory.SubmachineGun,
                BaseDamage = 29f,
                FireRateRoundsPerMinute = 1100f,
                MagazineCapacity = 33,
                ReloadTimeSeconds = 2.1f,
                TacticalReloadTimeSeconds = 1.6f,
                MuzzleVelocityMs = 380f,
                EffectiveRangeMeters = 150f,
                BaseAccuracySpreadMoa = 3.2f,
                ErgonomicsScore = 88f,
                BaseWeightKg = 2.7f,
                DefaultCaliber = "9x19mm Parabellum"
            });

            Register(new WeaponDefinition
            {
                WeaponId = "wpn_svd_marksman",
                DisplayName = "Vanguard SVD-EBR",
                Category = WeaponCategory.DesignatedMarksmanRifle,
                BaseDamage = 78f,
                FireRateRoundsPerMinute = 280f,
                MagazineCapacity = 10,
                ReloadTimeSeconds = 3.2f,
                TacticalReloadTimeSeconds = 2.7f,
                MuzzleVelocityMs = 830f,
                EffectiveRangeMeters = 800f,
                BaseAccuracySpreadMoa = 0.95f,
                ErgonomicsScore = 45f,
                BaseWeightKg = 4.8f,
                DefaultCaliber = "7.62x54mmR"
            });

            Register(new WeaponDefinition
            {
                WeaponId = "wpn_cheytac_m200",
                DisplayName = "Shadow Interceptor .408",
                Category = WeaponCategory.AntiMaterialRifle,
                BaseDamage = 195f,
                FireRateRoundsPerMinute = 42f,
                MagazineCapacity = 7,
                ReloadTimeSeconds = 4.2f,
                TacticalReloadTimeSeconds = 3.8f,
                MuzzleVelocityMs = 1020f,
                EffectiveRangeMeters = 1800f,
                BaseAccuracySpreadMoa = 0.35f,
                ErgonomicsScore = 25f,
                BaseWeightKg = 12.3f,
                DefaultCaliber = ".408 CheyTac"
            });

            Register(new WeaponDefinition
            {
                WeaponId = "wpn_spas12_tactical",
                DisplayName = "Breacher-12 Combat Shotgun",
                Category = WeaponCategory.CombatShotgun,
                BaseDamage = 112f,
                FireRateRoundsPerMinute = 240f,
                MagazineCapacity = 8,
                ReloadTimeSeconds = 4.5f,
                TacticalReloadTimeSeconds = 3.9f,
                MuzzleVelocityMs = 410f,
                EffectiveRangeMeters = 45f,
                BaseAccuracySpreadMoa = 8.5f,
                ErgonomicsScore = 55f,
                BaseWeightKg = 4.2f,
                DefaultCaliber = "12 Gauge 00 Buck"
            });
        }

        private static void Register(WeaponDefinition def)
        {
            _weapons[def.WeaponId] = def;
        }

        public static WeaponDefinition Get(string weaponId)
        {
            return _weapons.TryGetValue(weaponId, out var def) ? def : null;
        }

        public static IEnumerable<WeaponDefinition> GetAll() => _weapons.Values;
    }
}
