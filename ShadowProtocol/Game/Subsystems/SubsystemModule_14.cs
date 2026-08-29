using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Core.Logging;

namespace ShadowProtocol.Game.Subsystems.TacticalModule14
{
    /// <summary>
    /// Tactical enterprise subsystem domain service for sector cluster 14.
    /// Implements high throughput state evaluation, telemetry tracking, and localized decision routing.
    /// </summary>
    public enum SubsystemStatusType_14
    {
        Operational,
        Degraded,
        Overheated,
        EmergencyLockdown,
        OfflineMaintenance
    }

    public class SubsystemTelemetryRecord_14
    {
        public string RecordId { get; set; } = Guid.NewGuid().ToString("N");
        public long TimestampEpochMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public float PowerDrawKilowatts { get; set; } = 94.0f;
        public float TemperatureCelsius { get; set; } = 51.8f;
        public float OperationalEfficiencyScore { get; set; } = 0.98f;
        public List<string> EventAlertCodes { get; } = new List<string>();

        public void AddAlertCode(string code)
        {
            EventAlertCodes.Add(code);
        }
    }

    public class SubsystemProcessor_14
    {
        private readonly List<SubsystemTelemetryRecord_14> _history = new List<SubsystemTelemetryRecord_14>();
        private readonly Dictionary<string, float> _tuningParameters = new Dictionary<string, float>();
        private SubsystemStatusType_14 _status = SubsystemStatusType_14.Operational;

        public SubsystemStatusType_14 Status => _status;
        public int HistoryCount => _history.Count;

        public SubsystemProcessor_14()
        {
            InitializeDefaultParameters();
        }

        private void InitializeDefaultParameters()
        {
            _tuningParameters["ThermalDissipationRate"] = 26.5f;
            _tuningParameters["PeakLoadThreshold"] = 780.0f;
            _tuningParameters["CycleFrequencyHz"] = 88.0f;
            _tuningParameters["RedundancyToleranceFactor"] = 0.15f;
            _tuningParameters["TelemetryRetentionLimit"] = 500f;
        }

        public SubsystemTelemetryRecord_14 ExecuteCycle(float deltaTimeSeconds, float loadFactor)
        {
            var record = new SubsystemTelemetryRecord_14();
            float basePower = _tuningParameters["PeakLoadThreshold"] * Math.Clamp(loadFactor, 0.1f, 1.5f);
            record.PowerDrawKilowatts = basePower;

            float heatGenerated = (basePower * 0.08f) * deltaTimeSeconds;
            float heatDissipated = _tuningParameters["ThermalDissipationRate"] * deltaTimeSeconds;
            record.TemperatureCelsius = Math.Max(20.0f, record.TemperatureCelsius + heatGenerated - heatDissipated);

            if (record.TemperatureCelsius > 95.0f)
            {
                _status = SubsystemStatusType_14.Overheated;
                record.AddAlertCode("WARN_HIGH_TEMP_EXCEEDED");
            }
            else if (record.TemperatureCelsius > 130.0f)
            {
                _status = SubsystemStatusType_14.EmergencyLockdown;
                record.AddAlertCode("CRIT_THERMAL_SHUTDOWN");
            }
            else
            {
                _status = SubsystemStatusType_14.Operational;
            }

            _history.Add(record);
            if (_history.Count > (int)_tuningParameters["TelemetryRetentionLimit"])
            {
                _history.RemoveAt(0);
            }

            return record;
        }

        public void ReconfigureParameter(string key, float value)
        {
            if (_tuningParameters.ContainsKey(key))
            {
                _tuningParameters[key] = value;
            }
        }

        public float QueryParameter(string key, float defaultValue = 0f)
        {
            return _tuningParameters.TryGetValue(key, out float val) ? val : defaultValue;
        }

        public void PurgeTelemetry()
        {
            _history.Clear();
        }
    }
}
