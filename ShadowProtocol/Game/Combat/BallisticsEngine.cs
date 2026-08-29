using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Combat
{
    /// <summary>
    /// Physical properties of a fired ballistic projectile.
    /// </summary>
    public class BallisticProfile
    {
        public float CaliberMm { get; set; } = 7.62f;
        public float BulletMassGrams { get; set; } = 9.7f;
        public float MuzzleVelocityMs { get; set; } = 820f;
        public float DragCoefficientG1 { get; set; } = 0.295f;
        public float MaximumRangeMeters { get; set; } = 1200f;
        public float PenetrationRatingMm { get; set; } = 18f;
        public float RicochetThresholdDegrees { get; set; } = 68f;
        public bool HasTracer { get; set; } = true;
        public Vector3F TracerColorHex { get; set; } = new Vector3F(1.0f, 0.35f, 0.1f);
    }

    /// <summary>
    /// Represents an in-flight projectile being simulated frame-by-frame.
    /// </summary>
    public class ActiveProjectile
    {
        public uint ProjectileId { get; set; }
        public string FiredByEntityId { get; set; }
        public Vector3F Position { get; set; }
        public Vector3F Velocity { get; set; }
        public Vector3F Acceleration { get; set; }
        public BallisticProfile Profile { get; set; }
        public float FlightTimeSeconds { get; set; }
        public float TraveledDistanceMeters { get; set; }
        public bool IsAlive { get; set; } = true;
        public List<Vector3F> TrajectoryHistory { get; } = new List<Vector3F>();

        public ActiveProjectile(uint id, string shooter, Vector3F origin, Vector3F direction, BallisticProfile profile)
        {
            ProjectileId = id;
            FiredByEntityId = shooter;
            Position = origin;
            Profile = profile;
            Velocity = direction.Normalized() * profile.MuzzleVelocityMs;
            Acceleration = new Vector3F(0f, -9.80665f, 0f); // Standard Earth Gravity
            TrajectoryHistory.Add(origin);
        }
    }

    /// <summary>
    /// Environmental air properties impacting ballistics.
    /// </summary>
    public class BallisticAtmosphere
    {
        public float AirTemperatureCelsius { get; set; } = 15f;
        public float BarometricPressureHpa { get; set; } = 1013.25f;
        public float RelativeHumidity { get; set; } = 0.5f;
        public Vector3F WindVectorMs { get; set; } = new Vector3F(2.5f, 0f, 1.2f);

        public float CalculateAirDensityKgM3()
        {
            float tempKelvin = AirTemperatureCelsius + 273.15f;
            float dryPressure = BarometricPressureHpa * 100f;
            const float specificGasConstant = 287.058f;
            return dryPressure / (specificGasConstant * tempKelvin);
        }
    }

    /// <summary>
    /// High-precision ballistics simulation engine supporting supersonic drag, wind drift, and trajectory integration.
    /// </summary>
    public class BallisticsEngine
    {
        private readonly List<ActiveProjectile> _activeProjectiles = new List<ActiveProjectile>();
        private readonly BallisticAtmosphere _atmosphere = new BallisticAtmosphere();
        private uint _nextProjectileId = 1;

        public BallisticsAtmosphere Atmosphere => _atmosphere;
        public int ActiveCount => _activeProjectiles.Count;

        public ActiveProjectile FireProjectile(string shooterId, Vector3F origin, Vector3F direction, BallisticProfile profile)
        {
            var p = new ActiveProjectile(_nextProjectileId++, shooterId, origin, direction, profile);
            _activeProjectiles.Add(p);
            return p;
        }

        public void StepSimulation(float deltaTimeSeconds)
        {
            float airDensity = _atmosphere.CalculateAirDensityKgM3();
            float standardAirDensity = 1.225f;
            float densityFactor = airDensity / standardAirDensity;

            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var p = _activeProjectiles[i];
                if (!p.IsAlive)
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                // Compute relative velocity against wind
                Vector3F relVelocity = p.Velocity - _atmosphere.WindVectorMs;
                float speed = relVelocity.Magnitude();

                if (speed < 1f || p.TraveledDistanceMeters >= p.Profile.MaximumRangeMeters || p.Position.Y < -50f)
                {
                    p.IsAlive = false;
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                // Aerodynamic Drag Model: F_drag = 0.5 * rho * v^2 * Cd * A
                float caliberMeters = p.Profile.CaliberMm / 1000f;
                float crossSectionalArea = (float)(Math.PI * Math.Pow(caliberMeters * 0.5f, 2));
                float bulletMassKg = p.Profile.BulletMassGrams / 1000f;

                float machNumber = speed / 340.29f;
                float cd = ComputeMachDragCoefficient(machNumber, p.Profile.DragCoefficientG1);

                float dragForceMagnitude = 0.5f * airDensity * (speed * speed) * cd * crossSectionalArea;
                Vector3F dragAcceleration = (-relVelocity.Normalized()) * (dragForceMagnitude / bulletMassKg);

                // Integrate Gravity and Drag (Euler integration with sub-stepping for supersonic stability)
                int subSteps = (speed > 500f) ? 4 : 2;
                float subDt = deltaTimeSeconds / subSteps;

                for (int step = 0; step < subSteps; step++)
                {
                    Vector3F totalAccel = new Vector3F(0f, -9.80665f, 0f) + dragAcceleration;
                    p.Velocity += totalAccel * subDt;
                    Vector3F displacement = p.Velocity * subDt;
                    p.Position += displacement;
                    p.TraveledDistanceMeters += displacement.Magnitude();
                }

                p.FlightTimeSeconds += deltaTimeSeconds;
                p.TrajectoryHistory.Add(p.Position);

                // Cap trajectory points
                if (p.TrajectoryHistory.Count > 100)
                {
                    p.TrajectoryHistory.RemoveAt(0);
                }
            }
        }

        private float ComputeMachDragCoefficient(float mach, float baseCd)
        {
            if (mach < 0.8f) return baseCd * 0.75f;
            if (mach >= 0.8f && mach < 1.2f)
            {
                // Transonic Drag Rise Peak
                float t = (mach - 0.8f) / 0.4f;
                return MathUtils.Lerp(baseCd * 0.75f, baseCd * 1.65f, t);
            }
            if (mach >= 1.2f && mach < 2.5f)
            {
                // Supersonic Decay
                float t = (mach - 1.2f) / 1.3f;
                return MathUtils.Lerp(baseCd * 1.65f, baseCd * 1.10f, t);
            }
            return baseCd * 0.95f;
        }

        public bool CheckRicochet(Vector3F projectileDir, Vector3F surfaceNormal, float ricochetThreshold)
        {
            float dot = Vector3F.Dot(-projectileDir.Normalized(), surfaceNormal.Normalized());
            float impactAngleDeg = (float)(Math.Acos(Math.Clamp(dot, -1f, 1f)) * (180.0 / Math.PI));
            float glanceAngle = 90f - impactAngleDeg;
            return glanceAngle >= ricochetThreshold;
        }

        public Vector3F ComputeReflectionVector(Vector3F incomingVelocity, Vector3F surfaceNormal, float restitution = 0.55f)
        {
            Vector3F n = surfaceNormal.Normalized();
            Vector3F reflected = incomingVelocity - (n * (2.0f * Vector3F.Dot(incomingVelocity, n)));
            return reflected * restitution;
        }
    }
}
