using System.Text.Json;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Inventory;
using ShadowProtocol.Game.Missions;
using ShadowProtocol.Game.NPC;
using ShadowProtocol.Game.Player;
using ShadowProtocol.Game.Skills;
using ShadowProtocol.Game.World;

namespace ShadowProtocol.Game.SaveSystem;

public class PlayerSaveData
{
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Stamina { get; set; }
    public float MaxStamina { get; set; }
    public float Armor { get; set; }
    public float Energy { get; set; }
    public float Hunger { get; set; }
    public float Hydration { get; set; }

    public int Level { get; set; }
    public long Experience { get; set; }
    public int AvailableSkillPoints { get; set; }
    public long Currency { get; set; }

    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

public class ItemSaveData
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public float Durability { get; set; }
}

public class MissionSaveData
{
    public string MissionId { get; set; } = string.Empty;
    public MissionStatus Status { get; set; }
    public Dictionary<string, int> ObjectiveProgress { get; set; } = new();
}

public class SaveDataSnapshot
{
    public int SlotIndex { get; set; } = 0;
    public string SaveName { get; set; } = "QuickSave";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string GameVersion { get; set; } = "1.0.0";
    public double PlaytimeSeconds { get; set; } = 0;

    public PlayerSaveData Player { get; set; } = new();
    public List<ItemSaveData> InventoryItems { get; set; } = new();
    public Dictionary<string, int> UnlockedSkills { get; set; } = new();
    public List<MissionSaveData> Missions { get; set; } = new();
    public Dictionary<string, int> NpcRelationships { get; set; } = new();
    public List<string> DiscoveredRegions { get; set; } = new();

    public double WorldTimeSeconds { get; set; }
    public WeatherType Weather { get; set; }
}

public class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SaveDataSnapshot CreateSnapshot(
        int slotIndex,
        string saveName,
        PlayerController player,
        Inventory.Inventory inventory,
        SkillTreeManager skills,
        MissionManager missions,
        WorldManager world,
        IEnumerable<NPCController>? npcs = null
    )
    {
        var snapshot = new SaveDataSnapshot
        {
            SlotIndex = slotIndex,
            SaveName = saveName,
            CreatedAt = DateTime.UtcNow,
            GameVersion = "1.0.0",
            Player = new PlayerSaveData
            {
                Health = player.Vitals.Health,
                MaxHealth = player.Vitals.MaxHealth,
                Stamina = player.Vitals.Stamina,
                MaxStamina = player.Vitals.MaxStamina,
                Armor = player.Vitals.Armor,
                Energy = player.Vitals.Energy,
                Hunger = player.Vitals.Hunger,
                Hydration = player.Vitals.Hydration,
                Level = player.Attributes.Level,
                Experience = player.Attributes.Experience,
                AvailableSkillPoints = player.Attributes.AvailableSkillPoints,
                Currency = player.Attributes.Currency,
                PosX = player.Position.X,
                PosY = player.Position.Y,
                PosZ = player.Position.Z
            },
            WorldTimeSeconds = world.DayNight.CurrentTime.TotalSeconds,
            Weather = world.Weather.CurrentWeather
        };

        // Inventory
        foreach (var slot in inventory.Slots)
        {
            if (slot != null)
            {
                snapshot.InventoryItems.Add(new ItemSaveData
                {
                    ItemId = slot.Data.Id,
                    Quantity = slot.Quantity,
                    Durability = slot.CurrentDurability
                });
            }
        }

        // Skills
        foreach (var skill in skills.AllSkills)
        {
            if (skill.IsUnlocked)
            {
                snapshot.UnlockedSkills[skill.Id] = skill.CurrentLevel;
            }
        }

        // Missions
        foreach (var mission in missions.AllMissions)
        {
            var mData = new MissionSaveData
            {
                MissionId = mission.Id,
                Status = mission.Status
            };
            foreach (var obj in mission.Objectives)
            {
                mData.ObjectiveProgress[obj.Id] = obj.CurrentAmount;
            }
            snapshot.Missions.Add(mData);
        }

        // Discovered Regions
        foreach (var region in world.Regions.Values)
        {
            if (region.IsDiscovered)
            {
                snapshot.DiscoveredRegions.Add(region.Region.ToString());
            }
        }

        // NPC Relationships
        if (npcs != null)
        {
            foreach (var npc in npcs)
            {
                snapshot.NpcRelationships[npc.Name] = npc.RelationshipScore;
            }
        }

        return snapshot;
    }

    public string SerializeSnapshot(SaveDataSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public SaveDataSnapshot? DeserializeSnapshot(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveDataSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
