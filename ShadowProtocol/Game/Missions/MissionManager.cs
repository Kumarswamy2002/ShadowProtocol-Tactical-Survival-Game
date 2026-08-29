using ShadowProtocol.Core.Events;
using ShadowProtocol.Game.Player;

namespace ShadowProtocol.Game.Missions;

public class MissionManager
{
    private readonly Dictionary<string, Mission> _missions = new();
    private readonly IEventBus? _eventBus;

    public IReadOnlyCollection<Mission> AllMissions => _missions.Values;
    public IEnumerable<Mission> ActiveMissions => _missions.Values.Where(m => m.Status == MissionStatus.Active);
    public IEnumerable<Mission> CompletedMissions => _missions.Values.Where(m => m.Status == MissionStatus.Completed);

    public MissionManager(IEventBus? eventBus = null)
    {
        _eventBus = eventBus;
    }

    public void RegisterMission(Mission mission)
    {
        ArgumentNullException.ThrowIfNull(mission);
        _missions[mission.Id] = mission;
    }

    public bool StartMission(string missionId)
    {
        if (_missions.TryGetValue(missionId, out var mission) && mission.Status == MissionStatus.Inactive)
        {
            mission.Start();
            _eventBus?.Publish(new MissionStatusChangedEvent(mission.Id, mission.Title, mission.Status));
            return true;
        }
        return false;
    }

    public bool ProgressObjective(string missionId, string objectiveId, int amount = 1)
    {
        if (_missions.TryGetValue(missionId, out var mission) && mission.Status == MissionStatus.Active)
        {
            var obj = mission.Objectives.FirstOrDefault(o => o.Id == objectiveId);
            if (obj != null && !obj.IsCompleted)
            {
                obj.AddProgress(amount);
                _eventBus?.Publish(new ObjectiveProgressEvent(missionId, objectiveId, obj.CurrentAmount, obj.RequiredAmount));

                if (mission.AreMandatoryObjectivesCompleted)
                {
                    CompleteMission(missionId);
                }
                return true;
            }
        }
        return false;
    }

    public bool CompleteMission(string missionId, PlayerAttributes? playerAttributes = null)
    {
        if (_missions.TryGetValue(missionId, out var mission) && mission.Status == MissionStatus.Active)
        {
            if (mission.AreMandatoryObjectivesCompleted)
            {
                mission.Complete();

                if (playerAttributes != null)
                {
                    playerAttributes.AddExperience(mission.Reward.Experience);
                    playerAttributes.AddCurrency(mission.Reward.Currency);
                }

                _eventBus?.Publish(new MissionStatusChangedEvent(mission.Id, mission.Title, mission.Status));
                return true;
            }
        }
        return false;
    }

    public bool FailMission(string missionId)
    {
        if (_missions.TryGetValue(missionId, out var mission) && mission.Status == MissionStatus.Active)
        {
            mission.Fail();
            _eventBus?.Publish(new MissionStatusChangedEvent(mission.Id, mission.Title, mission.Status));
            return true;
        }
        return false;
    }

    public void UpdateDynamicEvents(float deltaTime)
    {
        foreach (var mission in _missions.Values)
        {
            if (mission is DynamicEvent dynamicEvent && dynamicEvent.Status == MissionStatus.Active)
            {
                dynamicEvent.UpdateTimer(deltaTime);
            }
        }
    }
}
