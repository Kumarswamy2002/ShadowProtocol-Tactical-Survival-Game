using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShadowProtocol.Core.Diagnostics
{
    public struct ProfileSample
    {
        public string MarkerName;
        public double DurationMilliseconds;
        public long TimestampTicks;
        public int ThreadId;
    }

    public class ProfilerSection : IDisposable
    {
        private readonly string _name;
        private readonly Stopwatch _stopwatch;
        private readonly ProfilerDiagnostics _owner;

        public ProfilerSection(string name, ProfilerDiagnostics owner)
        {
            _name = name;
            _owner = owner;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _owner.RecordSample(_name, _stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public class ProfilerDiagnostics
    {
        private readonly ConcurrentDictionary<string, List<double>> _sampleHistory = new ConcurrentDictionary<string, List<double>>();
        private readonly Stopwatch _frameStopwatch = new Stopwatch();
        private double _lastFrameTimeMs = 16.66;
        private int _totalFramesCounted = 0;

        public double CurrentFps => _lastFrameTimeMs > 0.001 ? 1000.0 / _lastFrameTimeMs : 60.0;
        public double LastFrameTimeMs => _lastFrameTimeMs;

        public void BeginFrame()
        {
            _frameStopwatch.Restart();
        }

        public void EndFrame()
        {
            _frameStopwatch.Stop();
            _lastFrameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds;
            _totalFramesCounted++;
        }

        public ProfilerSection Sample(string markerName)
        {
            return new ProfilerSection(markerName, this);
        }

        public void RecordSample(string name, double durationMs)
        {
            _sampleHistory.AddOrUpdate(name,
                _ => new List<double> { durationMs },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(durationMs);
                        if (list.Count > 120) list.RemoveAt(0);
                    }
                    return list;
                });
        }

        public double GetAverageDurationMs(string markerName)
        {
            if (_sampleHistory.TryGetValue(markerName, out var list))
            {
                lock (list)
                {
                    if (list.Count == 0) return 0.0;
                    double sum = 0.0;
                    foreach (var s in list) sum += s;
                    return sum / list.Count;
                }
            }
            return 0.0;
        }
    }
}
