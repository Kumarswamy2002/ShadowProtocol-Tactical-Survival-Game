using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;

namespace ShadowProtocol.AI.Perception
{
    public enum SensoryChannel
    {
        VisualDirect,
        VisualPeripheral,
        AcousticGunfire,
        AcousticFootstep,
        AcousticExplosion,
        ThermalSignature,
        ElectromagneticRadar
    }

    public class SensoryStimulus
    {
        public string StimulusId { get; set; }
        public string SourceEntityId { get; set; }
        public SensoryChannel Channel { get; set; }
        public Vector3F WorldPosition { get; set; }
        public float IntensityDecibelsOrLux { get; set; }
        public float Timestamp { get; set; }
        public float DecayRate { get; set; } = 1.0f;
    }

    public class SensoryMemoryRecord
    {
        public string TargetEntityId { get; set; }
        public Vector3F LastKnownPosition { get; set; }
        public Vector3F EstimatedVelocity { get; set; }
        public float ConfidenceLevel { get; set; } = 1.0f;
        public float TimeSinceLastObserved { get; set; }
        public bool IsDirectlyVisible { get; set; }
    }

    public class AdvancedSensorySystem
    {
        private readonly Dictionary<string, SensoryMemoryRecord> _memory = new Dictionary<string, SensoryMemoryRecord>();
        private readonly List<SensoryStimulus> _incomingStimuli = new List<SensoryStimulus>();

        public float VisionConeDegrees { get; set; } = 95f;
        public float MaxVisionRangeMeters { get; set; } = 75f;
        public float HearingThresholdDecibels { get; set; } = 28f;

        public IReadOnlyDictionary<string, SensoryMemoryRecord> Memory => _memory;

        public void SubmitStimulus(SensoryStimulus stimulus)
        {
            _incomingStimuli.Add(stimulus);
        }

        public void ProcessPerception(Vector3F observerPosition, Vector3F observerForward, float deltaTime, float currentTime)
        {
            // 1. Process new stimuli
            foreach (var stim in _incomingStimuli)
            {
                float distance = (stim.WorldPosition - observerPosition).Magnitude();
                bool detected = false;

                if (stim.Channel == SensoryChannel.VisualDirect || stim.Channel == SensoryChannel.VisualPeripheral)
                {
                    if (distance <= MaxVisionRangeMeters)
                    {
                        Vector3F toStim = (stim.WorldPosition - observerPosition).Normalized();
                        float dot = Vector3F.Dot(observerForward.Normalized(), toStim);
                        float angleDeg = (float)(Math.Acos(Math.Clamp(dot, -1f, 1f)) * (180.0 / Math.PI));

                        if (angleDeg <= (VisionConeDegrees * 0.5f))
                        {
                            detected = true;
                        }
                    }
                }
                else if (stim.Channel == SensoryChannel.AcousticGunfire || stim.Channel == SensoryChannel.AcousticFootstep || stim.Channel == SensoryChannel.AcousticExplosion)
                {
                    // Inverse square law sound attenuation: L = L0 - 20*log10(d)
                    float attenuation = (float)(20.0 * Math.Log10(Math.Max(1.0, distance)));
                    float perceivedDb = stim.IntensityDecibelsOrLux - attenuation;

                    if (perceivedDb >= HearingThresholdDecibels)
                    {
                        detected = true;
                    }
                }

                if (detected)
                {
                    if (!_memory.TryGetValue(stim.SourceEntityId, out var record))
                    {
                        record = new SensoryMemoryRecord { TargetEntityId = stim.SourceEntityId };
                        _memory[stim.SourceEntityId] = record;
                    }

                    record.LastKnownPosition = stim.WorldPosition;
                    record.ConfidenceLevel = 1.0f;
                    record.TimeSinceLastObserved = 0f;
                    record.IsDirectlyVisible = (stim.Channel == SensoryChannel.VisualDirect);
                }
            }
            _incomingStimuli.Clear();

            // 2. Memory decay
            var toRemove = new List<string>();
            foreach (var kvp in _memory)
            {
                var record = kvp.Value;
                record.TimeSinceLastObserved += deltaTime;
                record.ConfidenceLevel -= deltaTime * 0.08f; // Decays over ~12s

                if (record.ConfidenceLevel <= 0f)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _memory.Remove(key);
            }
        }
    }
}
