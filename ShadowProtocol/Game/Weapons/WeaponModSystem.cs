using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Weapons
{
    public enum ModSlotType
    {
        Optic,
        Muzzle,
        Barrel,
        Underbarrel,
        Magazine,
        Stock,
        InternalReceiver,
        LaserModule
    }

    public class WeaponAttachment
    {
        public string AttachmentId { get; set; }
        public string Name { get; set; }
        public ModSlotType Slot { get; set; }
        public float WeightKg { get; set; }
        public float RecoilReductionPercent { get; set; }
        public float MuzzleVelocityMultiplier { get; set; } = 1.0f;
        public float ErgonomicsModifier { get; set; }
        public float EffectiveRangeModifierPercent { get; set; }
        public float SoundSuppressionDecibels { get; set; }
        public float SpreadReductionPercent { get; set; }
        public int ExtraMagazineCapacity { get; set; }
        public float OpticalZoomFactor { get; set; } = 1.0f;
        public bool HasLaserPointer { get; set; }
        public bool HasThermalImaging { get; set; }
    }

    public class WeaponModSystem
    {
        private readonly Dictionary<ModSlotType, WeaponAttachment> _installedMods = new Dictionary<ModSlotType, WeaponAttachment>();

        public IReadOnlyDictionary<ModSlotType, WeaponAttachment> InstalledMods => _installedMods;

        public bool AttachMod(WeaponAttachment mod)
        {
            if (mod == null) return false;
            _installedMods[mod.Slot] = mod;
            return true;
        }

        public bool DetachMod(ModSlotType slot, out WeaponAttachment removedMod)
        {
            return _installedMods.Remove(slot, out removedMod);
        }

        public float CalculateTotalWeight()
        {
            float total = 0f;
            foreach (var mod in _installedMods.Values)
                total += mod.WeightKg;
            return total;
        }

        public float CalculateCombinedRecoilModifier()
        {
            float mod = 1.0f;
            foreach (var m in _installedMods.Values)
                mod *= (1.0f - (m.RecoilReductionPercent / 100f));
            return Math.Clamp(mod, 0.25f, 1.5f);
        }

        public float CalculateMuzzleVelocityMultiplier()
        {
            float mult = 1.0f;
            foreach (var m in _installedMods.Values)
                mult *= m.MuzzleVelocityMultiplier;
            return mult;
        }

        public float CalculateEffectiveRangeModifier()
        {
            float add = 0f;
            foreach (var m in _installedMods.Values)
                add += m.EffectiveRangeModifierPercent;
            return 1.0f + (add / 100f);
        }

        public bool IsSuppressed()
        {
            return _installedMods.TryGetValue(ModSlotType.Muzzle, out var muzzle) && muzzle.SoundSuppressionDecibels > 20f;
        }
    }
}
