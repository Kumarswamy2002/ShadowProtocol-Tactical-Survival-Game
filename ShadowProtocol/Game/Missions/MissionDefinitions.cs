using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Missions;

public enum MissionType
{
    MainStory,
    SideQuest,
    DynamicEvent,
    Bounty
}

public enum MissionCategory
{
    Investigation,
    Rescue,
    Extraction,
    Sabotage,
    Assault,
    Defense,
    Escort,
    Assassination,
    ResourceCollection,
    EnemyCampClearing,
    Delivery,
    Hunting,
    Recovery
}

public enum MissionStatus
{
    Inactive,
    Active,
    Completed,
    Failed
}

public class MissionObjective
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public int RequiredAmount { get; set; } = 1;
    public int CurrentAmount { get; private set; } = 0;
    public Vector3F? LocationMarker { get; set; }
    public bool IsOptional { get; set; } = false;

    public bool IsCompleted => CurrentAmount >= RequiredAmount;

    public void AddProgress(int amount = 1)
    {
        CurrentAmount = Math.Clamp(CurrentAmount + amount, 0, RequiredAmount);
    }

    public void Reset()
    {
        CurrentAmount = 0;
    }
}

public readonly record struct MissionStatusChangedEvent(
    string MissionId,
    string MissionTitle,
    MissionStatus Status
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct ObjectiveProgressEvent(
    string MissionId,
    string ObjectiveId,
    int Current,
    int Required
) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class MissionReward
{
    public long Experience { get; set; } = 500;
    public long Currency { get; set; } = 250;
    public Dictionary<string, int> ItemRewards { get; } = new();
    public Dictionary<string, int> FactionReputation { get; } = new();
}

public class Mission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Untitled Mission";
    public string Description { get; set; } = string.Empty;
    public MissionType Type { get; set; } = MissionType.SideQuest;
    public MissionCategory Category { get; set; } = MissionCategory.Investigation;
    public MissionStatus Status { get; private set; } = MissionStatus.Inactive;
    public int RequiredPlayerLevel { get; set; } = 1;

    public List<MissionObjective> Objectives { get; } = new();
    public MissionReward Reward { get; set; } = new();

    public bool AreMandatoryObjectivesCompleted => Objectives.Where(o => !o.IsOptional).All(o => o.IsCompleted);

    public void Start()
    {
        if (Status == MissionStatus.Inactive)
        {
            Status = MissionStatus.Active;
        }
    }

    public void Complete()
    {
        if (Status == MissionStatus.Active && AreMandatoryObjectivesCompleted)
        {
            Status = MissionStatus.Completed;
        }
    }

    public void Fail()
    {
        if (Status == MissionStatus.Active)
        {
            Status = MissionStatus.Failed;
        }
    }
}

public class DynamicEvent : Mission
{
    public float TimeRemainingSeconds { get; set; } = 300f; // 5 min default
    public Vector3F EventCenter { get; set; }
    public float EventRadius { get; set; } = 100f;

    public DynamicEvent()
    {
        Type = MissionType.DynamicEvent;
    }

    public void UpdateTimer(float deltaTime)
    {
        if (Status != MissionStatus.Active) return;

        TimeRemainingSeconds -= deltaTime;
        if (TimeRemainingSeconds <= 0f && !AreMandatoryObjectivesCompleted)
        {
            Fail();
        }
    }
}
