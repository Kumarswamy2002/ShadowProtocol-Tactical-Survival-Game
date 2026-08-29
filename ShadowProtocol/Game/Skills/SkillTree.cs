using ShadowProtocol.Core.Events;
using ShadowProtocol.Game.Player;

namespace ShadowProtocol.Game.Skills;

public enum SkillTreeBranch
{
    Combat,
    Survival,
    Stealth
}

public class SkillNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SkillTreeBranch Branch { get; set; }
    public int Tier { get; set; } = 1;
    public int PointCost { get; set; } = 1;
    public List<string> PrerequisiteSkillIds { get; } = new();

    public int MaxLevel { get; set; } = 3;
    public int CurrentLevel { get; set; } = 0;
    public bool IsUnlocked => CurrentLevel > 0;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public Action<PlayerAttributes, PlayerVitals, int>? ApplySkillBonus { get; set; }
}

public readonly record struct SkillUpgradedEvent(string SkillId, string SkillName, int NewLevel) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class SkillTreeManager
{
    private readonly Dictionary<string, SkillNode> _skills = new();
    private readonly IEventBus? _eventBus;

    public IReadOnlyCollection<SkillNode> AllSkills => _skills.Values;

    public SkillTreeManager(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
        InitializeStandardSkillTrees();
    }

    public void RegisterSkill(SkillNode skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        _skills[skill.Id] = skill;
    }

    public SkillNode? GetSkill(string id) => _skills.GetValueOrDefault(id);

    public bool CanUpgradeSkill(string skillId, PlayerAttributes attributes)
    {
        if (!_skills.TryGetValue(skillId, out var skill)) return false;
        if (skill.IsMaxLevel) return false;
        if (attributes.AvailableSkillPoints < skill.PointCost) return false;

        // Check prerequisites
        foreach (var prereqId in skill.PrerequisiteSkillIds)
        {
            if (!_skills.TryGetValue(prereqId, out var prereq) || !prereq.IsUnlocked)
            {
                return false;
            }
        }

        return true;
    }

    public bool UpgradeSkill(string skillId, PlayerAttributes attributes, PlayerVitals vitals)
    {
        if (!CanUpgradeSkill(skillId, attributes)) return false;

        var skill = _skills[skillId];
        if (attributes.SpendSkillPoints(skill.PointCost))
        {
            skill.CurrentLevel++;
            skill.ApplySkillBonus?.Invoke(attributes, vitals, skill.CurrentLevel);
            _eventBus?.Publish(new SkillUpgradedEvent(skill.Id, skill.Name, skill.CurrentLevel));
            return true;
        }

        return false;
    }

    private void InitializeStandardSkillTrees()
    {
        // === Combat Branch ===
        RegisterSkill(new SkillNode
        {
            Id = "combat_weapon_damage",
            Name = "Ballistic Mastery",
            Description = "+10% Weapon Damage per rank",
            Branch = SkillTreeBranch.Combat,
            Tier = 1,
            PointCost = 1,
            MaxLevel = 3,
            ApplySkillBonus = (attr, vit, rank) => attr.DamageMultiplier = 1.0f + (0.10f * rank)
        });

        RegisterSkill(new SkillNode
        {
            Id = "combat_reload_speed",
            Name = "Tactical Reload",
            Description = "+15% Faster Reload Speed per rank",
            Branch = SkillTreeBranch.Combat,
            Tier = 1,
            PointCost = 1,
            MaxLevel = 3,
            ApplySkillBonus = (attr, vit, rank) => attr.ReloadSpeedMultiplier = 1.0f + (0.15f * rank)
        });

        var armorPenSkill = new SkillNode
        {
            Id = "combat_armor_penetration",
            Name = "Armor Piercing Expertise",
            Description = "Enables armor penetration boosts",
            Branch = SkillTreeBranch.Combat,
            Tier = 2,
            PointCost = 2,
            MaxLevel = 3
        };
        armorPenSkill.PrerequisiteSkillIds.Add("combat_weapon_damage");
        RegisterSkill(armorPenSkill);

        // === Survival Branch ===
        RegisterSkill(new SkillNode
        {
            Id = "survival_endurance",
            Name = "Hardened Constitution",
            Description = "+25 Max Health per rank",
            Branch = SkillTreeBranch.Survival,
            Tier = 1,
            PointCost = 1,
            MaxLevel = 3,
            ApplySkillBonus = (attr, vit, rank) => vit.SetMaxHealth(100f + (25f * rank))
        });

        RegisterSkill(new SkillNode
        {
            Id = "survival_stamina",
            Name = "Iron Lungs",
            Description = "+25 Max Stamina & faster regen",
            Branch = SkillTreeBranch.Survival,
            Tier = 1,
            PointCost = 1,
            MaxLevel = 3,
            ApplySkillBonus = (attr, vit, rank) =>
            {
                vit.SetMaxStamina(100f + (25f * rank));
                vit.StaminaRecoveryRate = 15f + (5f * rank);
            }
        });

        var packMuleSkill = new SkillNode
        {
            Id = "survival_pack_mule",
            Name = "Pack Mule",
            Description = "+15kg Carry Weight capacity",
            Branch = SkillTreeBranch.Survival,
            Tier = 2,
            PointCost = 2,
            MaxLevel = 2,
            ApplySkillBonus = (attr, vit, rank) => attr.BonusCarryWeight = 15.0f * rank
        };
        packMuleSkill.PrerequisiteSkillIds.Add("survival_endurance");
        RegisterSkill(packMuleSkill);

        // === Stealth Branch ===
        RegisterSkill(new SkillNode
        {
            Id = "stealth_shadow_step",
            Name = "Shadow Step",
            Description = "-20% Movement Noise per rank",
            Branch = SkillTreeBranch.Stealth,
            Tier = 1,
            PointCost = 1,
            MaxLevel = 3,
            ApplySkillBonus = (attr, vit, rank) => attr.NoiseMultiplier = Math.Max(0.1f, 1.0f - (0.20f * rank))
        });

        var ghostStride = new SkillNode
        {
            Id = "stealth_silent_stalker",
            Name = "Silent Stalker",
            Description = "Enables full stealth movement bonus",
            Branch = SkillTreeBranch.Stealth,
            Tier = 2,
            PointCost = 2,
            MaxLevel = 2
        };
        ghostStride.PrerequisiteSkillIds.Add("stealth_shadow_step");
        RegisterSkill(ghostStride);
    }
}
