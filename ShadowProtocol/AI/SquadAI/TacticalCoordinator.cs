using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;
using ShadowProtocol.AI.Perception;

namespace ShadowProtocol.AI.SquadAI
{
    public enum TacticalManeuver
    {
        DirectAssault,
        FlankLeft,
        FlankRight,
        BoundingOverwatch,
        DefensivePerimeter,
        TacticalRetreat,
        SuppressiveBarrage,
        AmbushHold
    }

    public enum SquadRole
    {
        PointMan,
        AssaultLead,
        SuppressionGunner,
        MarksmanSniper,
        CombatMedic,
        BreacherTech
    }

    public class SquadMemberContext
    {
        public string EntityId { get; set; }
        public SquadRole Role { get; set; }
        public Vector3F CurrentPosition { get; set; }
        public Vector3F AssignedWaypoint { get; set; }
        public float HealthPercent { get; set; } = 1.0f;
        public float AmmoPercent { get; set; } = 1.0f;
        public bool IsInCover { get; set; }
        public bool IsSuppressed { get; set; }
        public string TargetEntityId { get; set; }
    }

    public class TacticalCoordinator
    {
        private readonly List<SquadMemberContext> _members = new List<SquadMemberContext>();
        private TacticalManeuver _currentManeuver = TacticalManeuver.DefensivePerimeter;
        private Vector3F _primaryThreatPosition = Vector3F.Zero;
        private float _tacticalDecisionTimer = 0f;
        private const float DecisionInterval = 1.5f;

        public IReadOnlyList<SquadMemberContext> Members => _members;
        public TacticalManeuver CurrentManeuver => _currentManeuver;
        public Vector3F PrimaryThreatPosition => _primaryThreatPosition;

        public void RegisterMember(string entityId, SquadRole role)
        {
            _members.Add(new SquadMemberContext
            {
                EntityId = entityId,
                Role = role
            });
        }

        public void UpdateThreat(Vector3F threatPos)
        {
            _primaryThreatPosition = threatPos;
        }

        public void EvaluateTactics(float deltaTime)
        {
            _tacticalDecisionTimer += deltaTime;
            if (_tacticalDecisionTimer < DecisionInterval) return;
            _tacticalDecisionTimer = 0f;

            if (_members.Count == 0) return;

            float avgHealth = CalculateSquadHealthAverage();
            int suppressedCount = CountSuppressedMembers();

            if (avgHealth < 0.35f || suppressedCount > (_members.Count / 2))
            {
                _currentManeuver = TacticalManeuver.TacticalRetreat;
            }
            else if (avgHealth > 0.8f && _members.Count >= 3)
            {
                _currentManeuver = TacticalManeuver.BoundingOverwatch;
            }
            else
            {
                _currentManeuver = TacticalManeuver.DefensivePerimeter;
            }

            AssignOrdersForManeuver(_currentManeuver);
        }

        private float CalculateSquadHealthAverage()
        {
            if (_members.Count == 0) return 0f;
            float sum = 0f;
            foreach (var m in _members) sum += m.HealthPercent;
            return sum / _members.Count;
        }

        private int CountSuppressedMembers()
        {
            int count = 0;
            foreach (var m in _members) if (m.IsSuppressed) count++;
            return count;
        }

        private void AssignOrdersForManeuver(TacticalManeuver maneuver)
        {
            Vector3F squadCenter = Vector3F.Zero;
            foreach (var m in _members) squadCenter += m.CurrentPosition;
            squadCenter *= (1.0f / Math.Max(1, _members.Count));

            Vector3F threatDir = (_primaryThreatPosition - squadCenter).Normalized();
            Vector3F flankRight = new Vector3F(-threatDir.Z, 0f, threatDir.X);
            Vector3F flankLeft = -flankRight;

            foreach (var member in _members)
            {
                switch (maneuver)
                {
                    case TacticalManeuver.TacticalRetreat:
                        member.AssignedWaypoint = member.CurrentPosition - (threatDir * 25f);
                        break;
                    case TacticalManeuver.BoundingOverwatch:
                        if (member.Role == SquadRole.PointMan || member.Role == SquadRole.BreacherTech)
                            member.AssignedWaypoint = member.CurrentPosition + (flankRight * 15f) + (threatDir * 10f);
                        else if (member.Role == SquadRole.SuppressionGunner)
                            member.AssignedWaypoint = squadCenter + (threatDir * 5f);
                        else
                            member.AssignedWaypoint = member.CurrentPosition + (flankLeft * 12f);
                        break;
                    case TacticalManeuver.DefensivePerimeter:
                    default:
                        member.AssignedWaypoint = squadCenter + (flankRight * ((_members.IndexOf(member) % 2 == 0) ? 8f : -8f));
                        break;
                }
            }
        }
    }
}
