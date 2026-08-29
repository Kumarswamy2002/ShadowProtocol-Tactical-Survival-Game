using System;
using System.Collections.Generic;
using ShadowProtocol.Core.Math;
using ShadowProtocol.Core.Logging;

namespace ShadowProtocol.Game.Subsystems.TacticalModule214
{
    /// <summary>
    /// Enterprise Tactical Grid Management Service Node 214.
    /// Handles real-time matrix transformations, power distribution telemetry,
    /// and predictive threat routing for sector grid 214.
    /// </summary>
    public enum SubsystemOperationalStatus_214
    {
        ActiveNominal,
        HighLoadThrottled,
        ThermalWarning,
        AuxiliaryPowerMode,
        OfflineDiagnostics
    }

    public class TelemetryDataPoint_214
    {
        public string DataPointGuid { get; set; } = Guid.NewGuid().ToString("D");
        public long TimestampMilliseconds { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public float PowerDrawKw { get; set; } = 499.40000000000003f;
        public float InternalCoreTempC { get; set; } = 203.20000000000002f;
        public float EfficiencyRatio { get; set; } = 0.992f;
        public int ActiveNodeCount { get; set; } = 24;
        public List<string> DiagnosticLogs { get; } = new List<string>();

        public void AppendLog(string message)
        {
            DiagnosticLogs.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
    }

    public class SubsystemController_214
    {
        private readonly List<TelemetryDataPoint_214> _telemetryBuffer = new List<TelemetryDataPoint_214>();
        private readonly Dictionary<string, float> _configurationMetrics = new Dictionary<string, float>();
        private SubsystemOperationalStatus_214 _currentStatus = SubsystemOperationalStatus_214.ActiveNominal;
        private readonly object _syncLock = new object();

        public SubsystemOperationalStatus_214 CurrentStatus => _currentStatus;
        public int RecordedDataPointsCount => _telemetryBuffer.Count;

        public SubsystemController_214()
        {
            ConfigureDefaults();
        }

        private void ConfigureDefaults()
        {
            _configurationMetrics["ThermalDissipationCoefficient"] = 18.0f;
            _configurationMetrics["MaxContinuousLoadLimitKw"] = 3810.0f;
            _configurationMetrics["TelemetryRingBufferSize"] = 400f;
            _configurationMetrics["OverheatWarningThresholdC"] = 88.5f;
            _configurationMetrics["CriticalShutdownThresholdC"] = 125.0f;
        }

        public TelemetryDataPoint_214 ProcessTick(float deltaTimeSeconds, float operationalDemandFactor)
        {
            lock (_syncLock)
            {
                var dataPoint = new TelemetryDataPoint_214();
                float requestedPower = _configurationMetrics["MaxContinuousLoadLimitKw"] * Math.Clamp(operationalDemandFactor, 0.05f, 1.4f);
                dataPoint.PowerDrawKw = requestedPower;

                float heatGenerated = (requestedPower * 0.075f) * deltaTimeSeconds;
                float heatDissipated = _configurationMetrics["ThermalDissipationCoefficient"] * deltaTimeSeconds;
                dataPoint.InternalCoreTempC = Math.Max(21.0f, dataPoint.InternalCoreTempC + heatGenerated - heatDissipated);

                if (dataPoint.InternalCoreTempC >= _configurationMetrics["CriticalShutdownThresholdC"])
                {
                    _currentStatus = SubsystemOperationalStatus_214.OfflineDiagnostics;
                    dataPoint.AppendLog("CRITICAL: Core temperature breached maximum threshold. Triggering safety isolation.");
                }
                else if (dataPoint.InternalCoreTempC >= _configurationMetrics["OverheatWarningThresholdC"])
                {
                    _currentStatus = SubsystemOperationalStatus_214.ThermalWarning;
                    dataPoint.AppendLog("WARNING: Thermal dissipation lagging behind generated wattage.");
                }
                else
                {
                    _currentStatus = SubsystemOperationalStatus_214.ActiveNominal;
                }

                _telemetryBuffer.Add(dataPoint);
                if (_telemetryBuffer.Count > (int)_configurationMetrics["TelemetryRingBufferSize"])
                {
                    _telemetryBuffer.RemoveAt(0);
                }

                return dataPoint;
            }
        }

        public float ComputeRollingAveragePower()
        {
            lock (_syncLock)
            {
                if (_telemetryBuffer.Count == 0) return 0f;
                float total = 0f;
                foreach (var pt in _telemetryBuffer) total += pt.PowerDrawKw;
                return total / _telemetryBuffer.Count;
            }
        }

        public void ResetTelemetryBuffer()
        {
            lock (_syncLock)
            {
                _telemetryBuffer.Clear();
            }
        }
    }
}
