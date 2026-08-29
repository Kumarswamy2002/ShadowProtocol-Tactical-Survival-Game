using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.NPC;

public enum NPCType
{
    Trader,
    Medic,
    Engineer,
    Guard,
    Survivor,
    QuestGiver,
    Civilian,
    Farmer,
    FactionLeader,
    EnemyNPC
}

public enum ScheduleActivity
{
    Sleep,
    WakeUp,
    Work,
    BreakTime,
    ReturnHome,
    SocialActivity
}

public readonly record struct ScheduleSlot(
    TimeSpan StartTime,
    TimeSpan EndTime,
    ScheduleActivity Activity,
    Vector3F TargetLocation,
    string LocationName
);

public class NPCSchedule
{
    private readonly List<ScheduleSlot> _slots = new();

    public IReadOnlyList<ScheduleSlot> Slots => _slots;

    public static NPCSchedule CreateStandardDailyRoutine(Vector3F homePos, Vector3F workPos, Vector3F tavernPos)
    {
        var schedule = new NPCSchedule();
        schedule.AddSlot(TimeSpan.FromHours(23), TimeSpan.FromHours(6), ScheduleActivity.Sleep, homePos, "Home / Bed");
        schedule.AddSlot(TimeSpan.FromHours(6), TimeSpan.FromHours(8), ScheduleActivity.WakeUp, homePos, "Home / Morning");
        schedule.AddSlot(TimeSpan.FromHours(8), TimeSpan.FromHours(12), ScheduleActivity.Work, workPos, "Workstation");
        schedule.AddSlot(TimeSpan.FromHours(12), TimeSpan.FromHours(14), ScheduleActivity.BreakTime, tavernPos, "Canteen / Break Area");
        schedule.AddSlot(TimeSpan.FromHours(14), TimeSpan.FromHours(18), ScheduleActivity.Work, workPos, "Workstation");
        schedule.AddSlot(TimeSpan.FromHours(18), TimeSpan.FromHours(21), ScheduleActivity.ReturnHome, homePos, "Home / Dinner");
        schedule.AddSlot(TimeSpan.FromHours(21), TimeSpan.FromHours(23), ScheduleActivity.SocialActivity, tavernPos, "Tavern / Plaza");
        return schedule;
    }

    public void AddSlot(TimeSpan start, TimeSpan end, ScheduleActivity activity, Vector3F targetLoc, string locName)
    {
        _slots.Add(new ScheduleSlot(start, end, activity, targetLoc, locName));
    }

    public ScheduleSlot GetCurrentActivity(TimeSpan timeOfDay)
    {
        foreach (var slot in _slots)
        {
            if (slot.StartTime <= slot.EndTime)
            {
                if (timeOfDay >= slot.StartTime && timeOfDay < slot.EndTime)
                {
                    return slot;
                }
            }
            else // Crosses midnight (e.g. 23:00 to 06:00)
            {
                if (timeOfDay >= slot.StartTime || timeOfDay < slot.EndTime)
                {
                    return slot;
                }
            }
        }

        // Fallback default
        return _slots.Count > 0
            ? _slots[0]
            : new ScheduleSlot(TimeSpan.Zero, TimeSpan.FromHours(24), ScheduleActivity.Work, Vector3F.Zero, "Default Zone");
    }
}
