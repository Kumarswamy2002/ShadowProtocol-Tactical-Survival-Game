using System;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.Game.Vehicles
{
    public class VehiclePhysicsEngine
    {
        public float MassKg { get; set; } = 1850f;
        public float EnginePowerHorsepower { get; set; } = 320f;
        public float MaxSpeedKph { get; set; } = 160f;
        public float CurrentSpeedKph { get; set; } = 0f;
        public float SteeringAngleDeg { get; set; } = 0f;
        public float MaxSteeringAngleDeg { get; set; } = 38f;
        public float BrakeForceNewtons { get; set; } = 6500f;
        public float FuelCapacityLiters { get; set; } = 75f;
        public float CurrentFuelLiters { get; set; } = 75f;
        public float FuelConsumptionLitersPerHour { get; set; } = 14f;

        public Vector3F Position { get; set; } = Vector3F.Zero;
        public Vector3F Velocity { get; set; } = Vector3F.Zero;
        public Vector3F ForwardVector { get; set; } = new Vector3F(0f, 0f, 1f);

        public void ApplyThrottle(float throttleInput, float deltaTime)
        {
            if (CurrentFuelLiters <= 0f) return;

            throttleInput = Math.Clamp(throttleInput, -1f, 1f);
            float targetSpeed = throttleInput * MaxSpeedKph;

            float accelerationRate = (EnginePowerHorsepower * 745.7f) / MassKg; // a = P / m
            if (throttleInput > 0f)
            {
                CurrentSpeedKph = Math.Min(MaxSpeedKph, CurrentSpeedKph + (accelerationRate * 3.6f * deltaTime * throttleInput));
                CurrentFuelLiters = Math.Max(0f, CurrentFuelLiters - (FuelConsumptionLitersPerHour / 3600f * deltaTime));
            }
            else if (throttleInput < 0f)
            {
                CurrentSpeedKph = Math.Max(-35f, CurrentSpeedKph + (accelerationRate * 3.6f * deltaTime * throttleInput));
            }
        }

        public void ApplyBraking(float brakeIntensity, float deltaTime)
        {
            brakeIntensity = Math.Clamp(brakeIntensity, 0f, 1f);
            float decelKph = (BrakeForceNewtons / MassKg) * 3.6f * deltaTime * brakeIntensity;

            if (CurrentSpeedKph > 0f)
                CurrentSpeedKph = Math.Max(0f, CurrentSpeedKph - decelKph);
            else if (CurrentSpeedKph < 0f)
                CurrentSpeedKph = Math.Min(0f, CurrentSpeedKph + decelKph);
        }

        public void UpdateSteering(float steerInput, float deltaTime)
        {
            float targetSteer = Math.Clamp(steerInput, -1f, 1f) * MaxSteeringAngleDeg;
            SteeringAngleDeg = MathUtils.Lerp(SteeringAngleDeg, targetSteer, deltaTime * 8.0f);
        }

        public void IntegratePhysics(float deltaTime)
        {
            // Aerodynamic drag + rolling resistance
            float dragDecel = 0.0005f * (CurrentSpeedKph * CurrentSpeedKph) * deltaTime;
            if (CurrentSpeedKph > 0f) CurrentSpeedKph = Math.Max(0f, CurrentSpeedKph - dragDecel);

            // Compute displacement
            float speedMs = CurrentSpeedKph / 3.6f;
            Velocity = ForwardVector * speedMs;
            Position += Velocity * deltaTime;
        }
    }
}
