using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.CoverSystem
{
    public enum CoverHeight
    {
        None,
        LowCrouch,
        FullStand
    }

    public enum CoverMaterialType
    {
        Concrete,
        SteelReinforced,
        Wood,
        Sandbags,
        VehicleChassis
    }

    public class CoverPoint
    {
        public string CoverId { get; set; }
        public Vector3F Position { get; set; }
        public Vector3F ForwardNormal { get; set; }
        public CoverHeight Height { get; set; }
        public CoverMaterialType Material { get; set; }
        public float DurabilityHp { get; set; } = 500f;
        public bool IsOccupied { get; set; }
        public string OccupiedByEntityId { get; set; }
    }

    public class CoverEvaluator
    {
        private readonly List<CoverPoint> _registeredCoverPoints = new List<CoverPoint>();

        public void RegisterCoverPoint(CoverPoint point)
        {
            _registeredCoverPoints.Add(point);
        }

        public CoverPoint FindBestCover(Vector3F seekerPosition, Vector3F threatPosition, float maxSearchRadius = 30f)
        {
            CoverPoint bestPoint = null;
            float highestScore = float.MinValue;

            foreach (var point in _registeredCoverPoints)
            {
                if (point.IsOccupied || point.DurabilityHp <= 0f) continue;

                float distToSeeker = (point.Position - seekerPosition).Magnitude();
                if (distToSeeker > maxSearchRadius) continue;

                // Evaluate angle between cover normal and threat
                Vector3F toThreat = (threatPosition - point.Position).Normalized();
                float dot = Vector3F.Dot(point.ForwardNormal.Normalized(), toThreat);

                // Good cover faces the threat (dot near 1.0)
                if (dot < 0.2f) continue; // Behind or perpendicular

                float angleScore = dot * 50f;
                float distanceScore = (maxSearchRadius - distToSeeker) * 1.5f;
                float heightScore = (point.Height == CoverHeight.FullStand) ? 20f : 10f;
                float materialScore = (point.Material == CoverMaterialType.SteelReinforced || point.Material == CoverMaterialType.Concrete) ? 15f : 5f;

                float totalScore = angleScore + distanceScore + heightScore + materialScore;

                if (totalScore > highestScore)
                {
                    highestScore = totalScore;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        public void DamageCover(string coverId, float damage)
        {
            var point = _registeredCoverPoints.Find(p => p.CoverId == coverId);
            if (point != null)
            {
                point.DurabilityHp = Math.Max(0f, point.DurabilityHp - damage);
                if (point.DurabilityHp <= 0f)
                {
                    point.Height = CoverHeight.None;
                }
            }
        }
    }
}
