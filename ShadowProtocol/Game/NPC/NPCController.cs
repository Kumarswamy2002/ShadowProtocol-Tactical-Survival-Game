using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.NPC;

public class NPCController
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Survivor";
    public NPCType Type { get; set; } = NPCType.Survivor;
    public string FactionId { get; set; } = "Settlers";

    public Vector3F Position { get; set; } = Vector3F.Zero;
    public Vector3F Forward { get; set; } = Vector3F.Forward;
    public float InteractionRadius { get; set; } = 2.5f;

    public NPCSchedule Schedule { get; set; }
    public ScheduleSlot CurrentScheduleSlot { get; private set; }

    // Relationship score with player: -100 (Hostile) to +100 (Revered)
    public int RelationshipScore { get; private set; } = 0;
    public bool IsHostile => RelationshipScore < -30;
    public bool WillTrade => !IsHostile && (Type == NPCType.Trader || Type == NPCType.Medic || Type == NPCType.Engineer);

    public NPCController(string name, NPCType type, Vector3F position, NPCSchedule? schedule = null)
    {
        Name = name;
        Type = type;
        Position = position;
        Schedule = schedule ?? NPCSchedule.CreateStandardDailyRoutine(position, position + new Vector3F(10, 0, 0), position + new Vector3F(0, 0, 10));
    }

    public void ModifyRelationship(int delta)
    {
        RelationshipScore = Math.Clamp(RelationshipScore + delta, -100, 100);
    }

    public bool CanInteractWithPlayer(Vector3F playerPos)
    {
        return Vector3F.Distance(Position, playerPos) <= InteractionRadius && !IsHostile;
    }

    public void UpdateSchedule(TimeSpan worldTimeOfDay, float deltaTime)
    {
        CurrentScheduleSlot = Schedule.GetCurrentActivity(worldTimeOfDay);

        // Path towards schedule location
        Vector3F destination = CurrentScheduleSlot.TargetLocation;
        if (Vector3F.Distance(Position, destination) > 0.5f)
        {
            Vector3F dir = (destination - Position).Normalized;
            Forward = dir;
            Position += dir * (2.0f * deltaTime);
        }
    }
}
