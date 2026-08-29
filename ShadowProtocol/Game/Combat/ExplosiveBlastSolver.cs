using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Combat
{
    public class ExplosiveChargeDefinition
    {
        public string ExplosiveId { get; set; }
        public string Name { get; set; }
        public float TntEquivalentKg { get; set; } = 1.2f;
        public float MaxDamage { get; set; } = 250f;
        public float InnerRadiusMeters { get; set; } = 3.0f;
        public float OuterRadiusMeters { get; set; } = 12.0f;
        public float ShrapnelCount { get; set; } = 64;
        public float ConcussiveImpulseNewtons { get; set; } = 8500f;
    }

    public class BlastResult
    {
        public string TargetEntityId { get; set; }
        public float DamageInflicted { get; set; }
        public float ConcussiveImpulseApplied { get; set; }
        public float ShrapnelHits { get; set; }
        public Vector3F ImpulseDirection { get; set; }
    }

    public class ExplosiveBlastSolver
    {
        public static List<BlastResult> ComputeBlastWave(
            ExplosiveChargeDefinition charge, 
            Vector3F blastOrigin, 
            IEnumerable<(string entityId, Vector3F position, float occlusionFactor)> targets)
        {
            var results = new List<BlastResult>();

            foreach (var target in targets)
            {
                Vector3F delta = target.position - blastOrigin;
                float dist = delta.Magnitude();

                if (dist > charge.OuterRadiusMeters) continue;

                // Overpressure shockwave decay: P(d) = P0 * (1 - d/R)^2
                float distanceFactor;
                if (dist <= charge.InnerRadiusMeters)
                {
                    distanceFactor = 1.0f;
                }
                else
                {
                    float normalized = (dist - charge.InnerRadiusMeters) / (charge.OuterRadiusMeters - charge.InnerRadiusMeters);
                    distanceFactor = (float)Math.Pow(1.0f - normalized, 2.0);
                }

                float effectiveOcclusion = Math.Clamp(1.0f - target.occlusionFactor, 0.05f, 1.0f);
                float finalDamage = charge.MaxDamage * distanceFactor * effectiveOcclusion;
                float impulse = charge.ConcussiveImpulseNewtons * distanceFactor * effectiveOcclusion;
                Vector3F impulseDir = dist > 0.001f ? delta.Normalized() : Vector3F.Up;

                // Shrapnel distribution
                float solidAngle = 1.0f / Math.Max(1.0f, dist * dist);
                float shrapnelHits = (float)Math.Round(charge.ShrapnelCount * solidAngle * effectiveOcclusion);

                results.Add(new BlastResult
                {
                    TargetEntityId = target.entityId,
                    DamageInflicted = finalDamage,
                    ConcussiveImpulseApplied = impulse,
                    ShrapnelHits = shrapnelHits,
                    ImpulseDirection = impulseDir
                });
            }

            return results;
        }
    }
}
