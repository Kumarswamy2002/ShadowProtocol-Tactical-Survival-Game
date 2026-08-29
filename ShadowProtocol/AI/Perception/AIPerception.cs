using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.Perception;

public enum StimulusType
{
    Visual,
    Audio,
    DamageReceived,
    SquadAlert
}

public readonly record struct PerceptionStimulus(
    StimulusType Type,
    Vector3F Location,
    float Intensity,
    object? Source,
    DateTime Timestamp
);

public class AIPerception
{
    public float FieldOfViewAngle { get; set; } = 110f; // degrees
    public float ViewDistance { get; set; } = 40f;      // meters
    public float HearingSensitivity { get; set; } = 1.0f;
    public float Alertness { get; private set; } = 0.0f; // 0 (calm) to 100 (fully alerted)

    public bool HasTargetInSight { get; private set; }
    public Vector3F? LastKnownTargetPosition { get; private set; }
    public float TimeSinceTargetLastSeen { get; private set; } = 999f;

    private readonly List<PerceptionStimulus> _recentStimuli = new();

    public void ProcessSight(
        Vector3F aiPosition,
        Vector3F aiForward,
        Vector3F targetPosition,
        float targetStealthFactor, // 0 is completely hidden, 1 is fully exposed
        bool hasLineOfSight,
        float deltaTime
    )
    {
        Vector3F toTarget = targetPosition - aiPosition;
        float distance = toTarget.Magnitude;

        if (distance > ViewDistance || !hasLineOfSight)
        {
            HasTargetInSight = false;
            TimeSinceTargetLastSeen += deltaTime;
            DecayAlertness(deltaTime * 5f);
            return;
        }

        float angle = MathUtils.AngleBetween(aiForward, toTarget);
        if (angle <= FieldOfViewAngle * 0.5f)
        {
            // Within sight cone
            float proximityFactor = 1.0f - (distance / ViewDistance);
            float detectionRate = 60f * proximityFactor * targetStealthFactor;

            Alertness = Math.Clamp(Alertness + (detectionRate * deltaTime), 0f, 100f);

            if (Alertness >= 60f)
            {
                HasTargetInSight = true;
                LastKnownTargetPosition = targetPosition;
                TimeSinceTargetLastSeen = 0f;
            }
        }
        else
        {
            HasTargetInSight = false;
            TimeSinceTargetLastSeen += deltaTime;
            DecayAlertness(deltaTime * 5f);
        }
    }

    public void ProcessHearing(Vector3F aiPosition, Vector3F soundOrigin, float soundLoudness)
    {
        float distance = Vector3F.Distance(aiPosition, soundOrigin);
        float effectiveVolume = soundLoudness * HearingSensitivity / Math.Max(1f, distance);

        if (effectiveVolume > 2.0f)
        {
            Alertness = Math.Clamp(Alertness + (effectiveVolume * 10f), 0f, 100f);
            LastKnownTargetPosition = soundOrigin;
            _recentStimuli.Add(new PerceptionStimulus(StimulusType.Audio, soundOrigin, effectiveVolume, null, DateTime.UtcNow));
        }
    }

    public void ReceiveDirectDamage(Vector3F sourcePosition)
    {
        Alertness = 100f;
        LastKnownTargetPosition = sourcePosition;
        _recentStimuli.Add(new PerceptionStimulus(StimulusType.DamageReceived, sourcePosition, 100f, null, DateTime.UtcNow));
    }

    public void DecayAlertness(float amount)
    {
        Alertness = Math.Max(0f, Alertness - amount);
        if (Alertness < 40f && TimeSinceTargetLastSeen > 10f)
        {
            HasTargetInSight = false;
        }
    }

    public void ClearStimuli()
    {
        _recentStimuli.Clear();
    }
}
