using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Missions
{
    public enum MissionObjectiveType
    {
        EliminateHighValueTarget,
        InfiltrateSecureFacility,
        EscortConvoy,
        RecoverEncryptedDrive,
        SabotagePowerGrid,
        DefendEnclaveOutpost
    }

    public class DynamicMission
    {
        public string MissionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TargetZoneId { get; set; }
        public MissionObjectiveType ObjectiveType { get; set; }
        public int DifficultyRating { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int RewardCredits { get; set; }
        public int RewardXp { get; set; }
        public string RewardItemBlueprintId { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DynamicMissionGenerator
    {
        private static readonly Random _rng = new Random(999);

        public DynamicMission GenerateProceduralMission(string targetZoneId, int playerLevel)
        {
            int difficulty = Math.Clamp(playerLevel + _rng.Next(-1, 3), 1, 10);
            Array objTypes = Enum.GetValues(typeof(MissionObjectiveType));
            var objType = (MissionObjectiveType)objTypes.GetValue(_rng.Next(objTypes.Length));

            string missionId = $"mis_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string title = GenerateTitle(objType, targetZoneId);
            string desc = $"Deploy into {targetZoneId} and execute operation: {title}. Intelligence indicates heavy enemy resistance.";

            int credits = difficulty * 250 + _rng.Next(100, 500);
            int xp = difficulty * 180 + _rng.Next(50, 200);

            return new DynamicMission
            {
                MissionId = missionId,
                Title = title,
                Description = desc,
                TargetZoneId = targetZoneId,
                ObjectiveType = objType,
                DifficultyRating = difficulty,
                TimeLimitSeconds = 1800,
                RewardCredits = credits,
                RewardXp = xp,
                RewardItemBlueprintId = difficulty >= 5 ? "blueprint_emp_cannon" : null
            };
        }

        private string GenerateTitle(MissionObjectiveType objType, string zoneId)
        {
            return objType switch
            {
                MissionObjectiveType.EliminateHighValueTarget => "Operation Silent Talon",
                MissionObjectiveType.InfiltrateSecureFacility => "Project Phantom Breach",
                MissionObjectiveType.EscortConvoy => "Iron Vanguard Caravan Escort",
                MissionObjectiveType.RecoverEncryptedDrive => "Data Extraction: Blackout Archives",
                MissionObjectiveType.SabotagePowerGrid => "Grid Overload Directive",
                _ => "Outpost Defense Protocol"
            };
        }
    }
}
