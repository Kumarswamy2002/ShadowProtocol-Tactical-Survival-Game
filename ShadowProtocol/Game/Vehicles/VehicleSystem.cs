using ShadowProtocol.Core.Events;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Game.Inventory;

namespace ShadowProtocol.Game.Vehicles;

public enum VehicleType
{
    Car,
    Truck,
    Motorcycle,
    ArmoredVehicle
}

public readonly record struct VehicleEnteredEvent(string VehicleId, VehicleType Type) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public readonly record struct VehicleExitedEvent(string VehicleId) : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class VehicleController
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public VehicleType Type { get; }

    public Vector3F Position { get; set; } = Vector3F.Zero;
    public Vector3F Forward { get; set; } = Vector3F.Forward;
    public Vector3F Velocity { get; set; } = Vector3F.Zero;

    public float MaxSpeed { get; set; } = 28f; // m/s
    public float Acceleration { get; set; } = 12f;
    public float BrakingForce { get; set; } = 20f;
    public float TurnSpeed { get; set; } = 60f; // deg/sec

    public float MaxHealth { get; set; } = 500f;
    public float Health { get; set; } = 500f;
    public float Armor { get; set; } = 50f;

    public float MaxFuel { get; set; } = 60f; // Liters
    public float CurrentFuel { get; set; } = 60f;
    public float FuelConsumptionPerSec { get; set; } = 0.25f;

    public bool IsOccupied { get; private set; } = false;
    public bool IsDestroyed => Health <= 0f;
    public bool HasFuel => CurrentFuel > 0f;

    public Inventory.Inventory TrunkStorage { get; }

    private readonly IEventBus? _eventBus;

    public VehicleController(VehicleType type, string name, Vector3F startPos, IEventBus? eventBus = null)
    {
        Type = type;
        Name = name;
        Position = startPos;
        _eventBus = eventBus;

        TrunkStorage = type switch
        {
            VehicleType.Motorcycle => new Inventory.Inventory(8, 15f, eventBus),
            VehicleType.Car => new Inventory.Inventory(20, 80f, eventBus),
            VehicleType.Truck => new Inventory.Inventory(45, 250f, eventBus),
            VehicleType.ArmoredVehicle => new Inventory.Inventory(30, 150f, eventBus),
            _ => new Inventory.Inventory(15, 50f, eventBus)
        };

        ConfigureTypeStats(type);
    }

    private void ConfigureTypeStats(VehicleType type)
    {
        switch (type)
        {
            case VehicleType.Motorcycle:
                MaxSpeed = 38f;
                Acceleration = 18f;
                MaxHealth = 250f;
                Health = 250f;
                Armor = 10f;
                MaxFuel = 25f;
                CurrentFuel = 25f;
                FuelConsumptionPerSec = 0.15f;
                break;
            case VehicleType.Truck:
                MaxSpeed = 22f;
                Acceleration = 8f;
                MaxHealth = 900f;
                Health = 900f;
                Armor = 80f;
                MaxFuel = 100f;
                CurrentFuel = 100f;
                FuelConsumptionPerSec = 0.45f;
                break;
            case VehicleType.ArmoredVehicle:
                MaxSpeed = 20f;
                Acceleration = 9f;
                MaxHealth = 1400f;
                Health = 1400f;
                Armor = 180f;
                MaxFuel = 120f;
                CurrentFuel = 120f;
                FuelConsumptionPerSec = 0.55f;
                break;
            default: // Car
                MaxSpeed = 30f;
                Acceleration = 14f;
                MaxHealth = 500f;
                Health = 500f;
                Armor = 35f;
                MaxFuel = 60f;
                CurrentFuel = 60f;
                FuelConsumptionPerSec = 0.25f;
                break;
        }
    }

    public bool EnterVehicle()
    {
        if (IsOccupied || IsDestroyed) return false;
        IsOccupied = true;
        _eventBus?.Publish(new VehicleEnteredEvent(Id, Type));
        return true;
    }

    public bool ExitVehicle(out Vector3F exitPosition)
    {
        if (!IsOccupied)
        {
            exitPosition = Position;
            return false;
        }
        IsOccupied = false;
        exitPosition = Position + (Vector3F.Cross(Forward, Vector3F.Up).Normalized * 2.0f);
        _eventBus?.Publish(new VehicleExitedEvent(Id));
        return true;
    }

    public void Drive(float throttle, float steering, float deltaTime, float surfaceGripMultiplier = 1.0f)
    {
        if (!IsOccupied || IsDestroyed || !HasFuel)
        {
            // Decelerate naturally
            Velocity *= MathF.Max(0f, 1.0f - (3.0f * deltaTime));
            Position += Velocity * deltaTime;
            return;
        }

        // Steer
        if (MathF.Abs(throttle) > 0.05f || Velocity.Magnitude > 1.0f)
        {
            float yawDelta = steering * TurnSpeed * surfaceGripMultiplier * deltaTime;
            // Approximate yaw rotation
            float rad = yawDelta * (MathF.PI / 180f);
            float newX = (Forward.X * MathF.Cos(rad)) - (Forward.Z * MathF.Sin(rad));
            float newZ = (Forward.X * MathF.Sin(rad)) + (Forward.Z * MathF.Cos(rad));
            Forward = new Vector3F(newX, 0, newZ).Normalized;
        }

        // Throttle & Fuel
        if (MathF.Abs(throttle) > 0.05f)
        {
            CurrentFuel = Math.Max(0f, CurrentFuel - (FuelConsumptionPerSec * MathF.Abs(throttle) * deltaTime));
            Vector3F targetVel = Forward * (throttle * MaxSpeed * surfaceGripMultiplier);
            Velocity = Vector3F.Lerp(Velocity, targetVel, Acceleration * deltaTime);
        }
        else
        {
            Velocity *= MathF.Max(0f, 1.0f - (2.0f * deltaTime));
        }

        Position += Velocity * deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed) return;
        float effectiveArmor = Armor;
        float mitigation = effectiveArmor / (effectiveArmor + 150f);
        float actualDamage = amount * (1.0f - mitigation);
        Health = Math.Max(0f, Health - actualDamage);
    }

    public void Repair(float amount)
    {
        Health = Math.Clamp(Health + amount, 0f, MaxHealth);
    }

    public void Refuel(float liters)
    {
        CurrentFuel = Math.Clamp(CurrentFuel + liters, 0f, MaxFuel);
    }
}
