using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Player
{
    public enum PerkCategory
    {
        CombatTactics,
        SurvivalEndurance,
        EngineeringCraft,
        InfiltrationStealth,
        BallisticsExpertise,
        CyberneticAugment
    }

    public class PerkDefinition
    {
        public string PerkId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public PerkCategory Category { get; set; }
        public int TierRequirement { get; set; }
        public int SkillPointCost { get; set; } = 1;
        public Dictionary<string, float> StatModifiers { get; } = new Dictionary<string, float>();
        public List<string> PrerequisitePerkIds { get; } = new List<string>();

        public PerkDefinition AddModifier(string stat, float val)
        {
            StatModifiers[stat] = val;
            return this;
        }

        public PerkDefinition AddPrereq(string perkId)
        {
            PrerequisitePerkIds.Add(perkId);
            return this;
        }
    }

    public class PerkSystem
    {
        private readonly Dictionary<string, PerkDefinition> _catalog = new Dictionary<string, PerkDefinition>();
        private readonly HashSet<string> _unlockedPerks = new HashSet<string>();

        public IReadOnlySet<string> UnlockedPerks => _unlockedPerks;

        public PerkSystem()
        {
            InitializePerks();
        }

        private void InitializePerks()
        {
            Register(new PerkDefinition
            {
                PerkId = "perk_steady_hands",
                Name = "Steady Hands",
                Description = "Reduces weapon sway by 30% and recoil impulse by 15%",
                Category = PerkCategory.BallisticsExpertise,
                TierRequirement = 1
            }.AddModifier("RecoilMultiplier", -0.15f).AddModifier("SwayMultiplier", -0.30f));

            Register(new PerkDefinition
            {
                PerkId = "perk_adrenal_surge",
                Name = "Adrenal Surge",
                Description = "Increases sprint speed by 20% when health drops below 35%",
                Category = PerkCategory.SurvivalEndurance,
                TierRequirement = 2
            }.AddModifier("LowHealthSpeedBonus", 0.20f));

            Register(new PerkDefinition
            {
                PerkId = "perk_silent_step",
                Name = "Silent Footsteps",
                Description = "Reduces acoustic noise generated while moving by 45%",
                Category = PerkCategory.InfiltrationStealth,
                TierRequirement = 1
            }.AddModifier("NoiseGenerationFactor", -0.45f));

            Register(new PerkDefinition
            {
                PerkId = "perk_ghost_protocol",
                Name = "Ghost Protocol",
                Description = "Thermal and night vision detection signature reduced by 60%",
                Category = PerkCategory.InfiltrationStealth,
                TierRequirement = 3
            }.AddPrereq("perk_silent_step").AddModifier("ThermalVisibilityFactor", -0.60f));

            Register(new PerkDefinition
            {
                PerkId = "perk_field_medic",
                Name = "Field Surgeon",
                Description = "Consumable medical treatment speed increased by 40%",
                Category = PerkCategory.SurvivalEndurance,
                TierRequirement = 2
            }.AddModifier("MedicalSpeedFactor", 0.40f));

            Register(new PerkDefinition
            {
                PerkId = "perk_armor_smith",
                Name = "Hardened Plates",
                Description = "Increases armor plate durability absorption by 25%",
                Category = PerkCategory.EngineeringCraft,
                TierRequirement = 2
            }.AddModifier("ArmorAbsorptionBonus", 0.25f));

            Register(new PerkDefinition
            {
                PerkId = "perk_overclocked_cyberware",
                Name = "Neural Overclock",
                Description = "Increases reflex bullet time slow-motion window by 2.5 seconds",
                Category = PerkCategory.CyberneticAugment,
                TierRequirement = 4
            }.AddModifier("ReflexWindowSeconds", 2.5f));
        }

        public void Register(PerkDefinition perk)
        {
            _catalog[perk.PerkId] = perk;
        }

        public bool CanUnlock(string perkId)
        {
            if (!_catalog.TryGetValue(perkId, out var perk)) return false;
            if (_unlockedPerks.Contains(perkId)) return false;

            foreach (var prereq in perk.PrerequisitePerkIds)
            {
                if (!_unlockedPerks.Contains(prereq)) return false;
            }
            return true;
        }

        public bool Unlock(string perkId)
        {
            if (!CanUnlock(perkId)) return false;
            _unlockedPerks.Add(perkId);
            return true;
        }

        public float GetTotalStatModifier(string statName)
        {
            float total = 0f;
            foreach (var perkId in _unlockedPerks)
            {
                if (_catalog.TryGetValue(perkId, out var def) && def.StatModifiers.TryGetValue(statName, out float val))
                {
                    total += val;
                }
            }
            return total;
        }
    }
}
