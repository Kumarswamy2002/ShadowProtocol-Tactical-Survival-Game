// =============================================================================
// ShadowProtocol.Core — Curves: Spline and curve evaluation for animations/paths
// =============================================================================

using System;
using System.Collections.Generic;

namespace ShadowProtocol.Core.Math
{
    /// <summary>
    /// Types of spline interpolation supported by the curve system.
    /// </summary>
    public enum SplineType
    {
        /// <summary>Linear interpolation between control points.</summary>
        Linear,
        /// <summary>Catmull-Rom spline — passes through all control points.</summary>
        CatmullRom,
        /// <summary>Cubic Bezier spline — uses tangent handles for smooth curves.</summary>
        CubicBezier,
        /// <summary>Hermite spline — uses point-tangent pairs for interpolation.</summary>
        Hermite,
        /// <summary>B-Spline — smooth approximation that may not pass through control points.</summary>
        BSpline
    }

    /// <summary>
    /// Represents a keyframe on a curve with a value and optional tangent information.
    /// </summary>
    public struct CurveKeyframe
    {
        /// <summary>Time position of this keyframe (typically [0..1] or [0..duration]).</summary>
        public float Time;
        /// <summary>Value at this keyframe.</summary>
        public float Value;
        /// <summary>Incoming tangent slope.</summary>
        public float InTangent;
        /// <summary>Outgoing tangent slope.</summary>
        public float OutTangent;

        public CurveKeyframe(float time, float value, float inTangent = 0f, float outTangent = 0f)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
        }
    }

    /// <summary>
    /// An animation curve that interpolates between keyframes using Hermite splines.
    /// Similar to Unity's AnimationCurve — useful for animation blending,
    /// damage falloff curves, spawn rate profiles, etc.
    /// </summary>
    public class AnimationCurve
    {
        private readonly List<CurveKeyframe> _keyframes = new List<CurveKeyframe>();

        /// <summary>Number of keyframes in this curve.</summary>
        public int KeyframeCount => _keyframes.Count;

        /// <summary>Creates an empty animation curve.</summary>
        public AnimationCurve() { }

        /// <summary>Creates a curve from an array of keyframes.</summary>
        public AnimationCurve(params CurveKeyframe[] keyframes)
        {
            _keyframes.AddRange(keyframes);
            _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        /// <summary>
        /// Adds a keyframe at the given time and value with auto-computed tangents.
        /// </summary>
        public void AddKey(float time, float value)
        {
            _keyframes.Add(new CurveKeyframe(time, value, 0, 0));
            _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
            AutoComputeTangents();
        }

        /// <summary>
        /// Adds a keyframe with explicit tangent values.
        /// </summary>
        public void AddKey(float time, float value, float inTangent, float outTangent)
        {
            _keyframes.Add(new CurveKeyframe(time, value, inTangent, outTangent));
            _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        /// <summary>
        /// Evaluates the curve at the given time using Hermite interpolation.
        /// </summary>
        public float Evaluate(float time)
        {
            if (_keyframes.Count == 0) return 0f;
            if (_keyframes.Count == 1) return _keyframes[0].Value;

            // Clamp to curve range
            if (time <= _keyframes[0].Time) return _keyframes[0].Value;
            if (time >= _keyframes[_keyframes.Count - 1].Time) return _keyframes[_keyframes.Count - 1].Value;

            // Find segment
            int idx = 0;
            for (int i = 0; i < _keyframes.Count - 1; i++)
            {
                if (time >= _keyframes[i].Time && time <= _keyframes[i + 1].Time)
                {
                    idx = i;
                    break;
                }
            }

            var k0 = _keyframes[idx];
            var k1 = _keyframes[idx + 1];
            float dt = k1.Time - k0.Time;
            if (dt < MathUtils.Epsilon) return k0.Value;

            float t = (time - k0.Time) / dt;
            return HermiteInterpolate(k0.Value, k0.OutTangent * dt, k1.Value, k1.InTangent * dt, t);
        }

        /// <summary>
        /// Hermite basis interpolation.
        /// </summary>
        private static float HermiteInterpolate(float v0, float t0, float v1, float t1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + t;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;
            return h00 * v0 + h10 * t0 + h01 * v1 + h11 * t1;
        }

        /// <summary>
        /// Auto-computes smooth tangents using finite differences (Catmull-Rom style).
        /// </summary>
        public void AutoComputeTangents()
        {
            for (int i = 0; i < _keyframes.Count; i++)
            {
                var kf = _keyframes[i];

                if (i == 0 && _keyframes.Count > 1)
                {
                    float slope = (_keyframes[1].Value - kf.Value) / (_keyframes[1].Time - kf.Time);
                    kf.InTangent = slope;
                    kf.OutTangent = slope;
                }
                else if (i == _keyframes.Count - 1 && _keyframes.Count > 1)
                {
                    float slope = (kf.Value - _keyframes[i - 1].Value) / (kf.Time - _keyframes[i - 1].Time);
                    kf.InTangent = slope;
                    kf.OutTangent = slope;
                }
                else if (_keyframes.Count > 2)
                {
                    float slope = (_keyframes[i + 1].Value - _keyframes[i - 1].Value) /
                                  (_keyframes[i + 1].Time - _keyframes[i - 1].Time);
                    kf.InTangent = slope;
                    kf.OutTangent = slope;
                }

                _keyframes[i] = kf;
            }
        }

        // ==================================================
        // Predefined Curves
        // ==================================================

        /// <summary>
        /// Creates a linear curve from 0 to 1.
        /// </summary>
        public static AnimationCurve Linear()
        {
            return new AnimationCurve(
                new CurveKeyframe(0, 0, 1, 1),
                new CurveKeyframe(1, 1, 1, 1)
            );
        }

        /// <summary>
        /// Creates an ease-in ease-out S-curve from 0 to 1.
        /// </summary>
        public static AnimationCurve EaseInOut()
        {
            return new AnimationCurve(
                new CurveKeyframe(0, 0, 0, 0),
                new CurveKeyframe(1, 1, 0, 0)
            );
        }

        /// <summary>
        /// Creates a curve that quickly rises then holds constant (attack curve).
        /// </summary>
        public static AnimationCurve AttackCurve(float attackTime = 0.1f)
        {
            return new AnimationCurve(
                new CurveKeyframe(0, 0, 0, 0),
                new CurveKeyframe(attackTime, 1, 0, 0),
                new CurveKeyframe(1, 1, 0, 0)
            );
        }

        /// <summary>
        /// Creates a bell curve peaking at the midpoint.
        /// </summary>
        public static AnimationCurve BellCurve()
        {
            return new AnimationCurve(
                new CurveKeyframe(0, 0, 0, 0),
                new CurveKeyframe(0.5f, 1, 0, 0),
                new CurveKeyframe(1, 0, 0, 0)
            );
        }
    }

    /// <summary>
    /// Provides spline interpolation for 3D paths.
    /// Used for camera paths, AI patrol routes, vehicle tracks, and projectile arcs.
    /// </summary>
    public class Spline3D
    {
        private readonly List<Vector3F> _points = new List<Vector3F>();
        private SplineType _type;
        private float[] _segmentLengths;
        private float _totalLength;
        private bool _isDirty = true;

        /// <summary>Number of control points in this spline.</summary>
        public int PointCount => _points.Count;

        /// <summary>Total arc length of the spline (approximated).</summary>
        public float TotalLength
        {
            get
            {
                if (_isDirty) RecalculateLengths();
                return _totalLength;
            }
        }

        /// <summary>
        /// Creates a new spline with the specified interpolation type.
        /// </summary>
        public Spline3D(SplineType type = SplineType.CatmullRom)
        {
            _type = type;
        }

        /// <summary>
        /// Creates a spline from an array of control points.
        /// </summary>
        public Spline3D(Vector3F[] points, SplineType type = SplineType.CatmullRom)
        {
            _type = type;
            _points.AddRange(points);
            _isDirty = true;
        }

        /// <summary>Adds a control point to the end of the spline.</summary>
        public void AddPoint(Vector3F point)
        {
            _points.Add(point);
            _isDirty = true;
        }

        /// <summary>Inserts a control point at the given index.</summary>
        public void InsertPoint(int index, Vector3F point)
        {
            _points.Insert(index, point);
            _isDirty = true;
        }

        /// <summary>Removes a control point at the given index.</summary>
        public void RemovePoint(int index)
        {
            _points.RemoveAt(index);
            _isDirty = true;
        }

        /// <summary>Sets the position of a control point.</summary>
        public void SetPoint(int index, Vector3F point)
        {
            _points[index] = point;
            _isDirty = true;
        }

        /// <summary>Gets the position of a control point.</summary>
        public Vector3F GetPoint(int index) => _points[index];

        /// <summary>
        /// Evaluates the spline at parameter t ∈ [0, 1].
        /// t=0 is the start of the spline, t=1 is the end.
        /// </summary>
        public Vector3F Evaluate(float t)
        {
            if (_points.Count == 0) return Vector3F.Zero;
            if (_points.Count == 1) return _points[0];
            if (_points.Count == 2) return Vector3F.Lerp(_points[0], _points[1], t);

            return _type switch
            {
                SplineType.Linear => EvaluateLinear(t),
                SplineType.CatmullRom => EvaluateCatmullRom(t),
                SplineType.CubicBezier => EvaluateBezier(t),
                SplineType.BSpline => EvaluateBSpline(t),
                _ => EvaluateCatmullRom(t)
            };
        }

        /// <summary>
        /// Evaluates the spline at a specific arc-length distance from the start.
        /// This provides constant-speed traversal along the spline.
        /// </summary>
        public Vector3F EvaluateAtDistance(float distance)
        {
            if (_isDirty) RecalculateLengths();
            if (_totalLength <= 0) return Evaluate(0);
            float t = DistanceToParameter(distance);
            return Evaluate(t);
        }

        /// <summary>
        /// Returns the tangent (derivative) at parameter t.
        /// </summary>
        public Vector3F EvaluateTangent(float t)
        {
            const float h = 0.001f;
            Vector3F p0 = Evaluate(t - h);
            Vector3F p1 = Evaluate(t + h);
            return (p1 - p0).Normalized();
        }

        /// <summary>
        /// Returns the normal vector at parameter t (perpendicular to tangent, in the curve plane).
        /// </summary>
        public Vector3F EvaluateNormal(float t, Vector3F up)
        {
            Vector3F tangent = EvaluateTangent(t);
            Vector3F binormal = Vector3F.Cross(tangent, up).Normalized();
            return Vector3F.Cross(binormal, tangent).Normalized();
        }

        /// <summary>
        /// Returns the closest point on the spline to a given world position.
        /// Uses iterative refinement for accuracy.
        /// </summary>
        public Vector3F ClosestPoint(Vector3F position, out float closestT, int samples = 100)
        {
            float bestT = 0f;
            float bestDistSq = float.MaxValue;

            // Coarse search
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3F p = Evaluate(t);
                float distSq = (p - position).MagnitudeSquared();
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestT = t;
                }
            }

            // Refinement passes
            float searchRange = 1f / samples;
            for (int pass = 0; pass < 4; pass++)
            {
                float refineBest = bestT;
                float refineDistSq = bestDistSq;

                for (int i = -5; i <= 5; i++)
                {
                    float t = MathUtils.Clamp01(bestT + i * searchRange * 0.2f);
                    Vector3F p = Evaluate(t);
                    float distSq = (p - position).MagnitudeSquared();
                    if (distSq < refineDistSq)
                    {
                        refineDistSq = distSq;
                        refineBest = t;
                    }
                }

                bestT = refineBest;
                bestDistSq = refineDistSq;
                searchRange *= 0.2f;
            }

            closestT = bestT;
            return Evaluate(bestT);
        }

        /// <summary>
        /// Generates an array of evenly-spaced points along the spline.
        /// Useful for placing objects, drawing path indicators, or generating meshes.
        /// </summary>
        public Vector3F[] GenerateEvenlySpacedPoints(float spacing)
        {
            if (_isDirty) RecalculateLengths();
            if (_totalLength <= 0) return new[] { Evaluate(0) };

            int count = MathF.Max(2, (int)MathF.Ceiling(_totalLength / spacing) + 1);
            var points = new Vector3F[count];

            for (int i = 0; i < count; i++)
            {
                float distance = (float)i / (count - 1) * _totalLength;
                points[i] = EvaluateAtDistance(distance);
            }

            return points;
        }

        // ==================================================
        // Private Evaluation Methods
        // ==================================================

        private Vector3F EvaluateLinear(float t)
        {
            int segments = _points.Count - 1;
            float scaledT = t * segments;
            int idx = MathUtils.Clamp((int)scaledT, 0, segments - 1);
            float localT = scaledT - idx;
            return Vector3F.Lerp(_points[idx], _points[idx + 1], localT);
        }

        private Vector3F EvaluateCatmullRom(float t)
        {
            int segments = _points.Count - 1;
            float scaledT = t * segments;
            int idx = MathUtils.Clamp((int)scaledT, 0, segments - 1);
            float localT = scaledT - idx;

            Vector3F p0 = _points[System.Math.Max(0, idx - 1)];
            Vector3F p1 = _points[idx];
            Vector3F p2 = _points[System.Math.Min(_points.Count - 1, idx + 1)];
            Vector3F p3 = _points[System.Math.Min(_points.Count - 1, idx + 2)];

            return CatmullRomInterpolate(p0, p1, p2, p3, localT);
        }

        private Vector3F EvaluateBezier(float t)
        {
            // Treat every 4 points as a cubic bezier segment
            int bezierSegments = (_points.Count - 1) / 3;
            if (bezierSegments <= 0) return EvaluateLinear(t);

            float scaledT = t * bezierSegments;
            int segIdx = MathUtils.Clamp((int)scaledT, 0, bezierSegments - 1);
            float localT = scaledT - segIdx;

            int baseIdx = segIdx * 3;
            Vector3F p0 = _points[baseIdx];
            Vector3F p1 = _points[System.Math.Min(baseIdx + 1, _points.Count - 1)];
            Vector3F p2 = _points[System.Math.Min(baseIdx + 2, _points.Count - 1)];
            Vector3F p3 = _points[System.Math.Min(baseIdx + 3, _points.Count - 1)];

            return CubicBezierInterpolate(p0, p1, p2, p3, localT);
        }

        private Vector3F EvaluateBSpline(float t)
        {
            if (_points.Count < 4) return EvaluateCatmullRom(t);

            int segments = _points.Count - 3;
            float scaledT = t * segments;
            int idx = MathUtils.Clamp((int)scaledT, 0, segments - 1);
            float localT = scaledT - idx;

            Vector3F p0 = _points[idx];
            Vector3F p1 = _points[idx + 1];
            Vector3F p2 = _points[idx + 2];
            Vector3F p3 = _points[idx + 3];

            return BSplineInterpolate(p0, p1, p2, p3, localT);
        }

        // ==================================================
        // Spline Basis Functions
        // ==================================================

        /// <summary>
        /// Catmull-Rom spline interpolation between p1 and p2 using p0 and p3 as guides.
        /// </summary>
        public static Vector3F CatmullRomInterpolate(Vector3F p0, Vector3F p1, Vector3F p2, Vector3F p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float x = 0.5f * ((2f * p1.X) +
                (-p0.X + p2.X) * t +
                (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * t2 +
                (-p0.X + 3f * p1.X - 3f * p2.X + p3.X) * t3);

            float y = 0.5f * ((2f * p1.Y) +
                (-p0.Y + p2.Y) * t +
                (2f * p0.Y - 5f * p1.Y + 4f * p2.Y - p3.Y) * t2 +
                (-p0.Y + 3f * p1.Y - 3f * p2.Y + p3.Y) * t3);

            float z = 0.5f * ((2f * p1.Z) +
                (-p0.Z + p2.Z) * t +
                (2f * p0.Z - 5f * p1.Z + 4f * p2.Z - p3.Z) * t2 +
                (-p0.Z + 3f * p1.Z - 3f * p2.Z + p3.Z) * t3);

            return new Vector3F(x, y, z);
        }

        /// <summary>
        /// Cubic Bezier interpolation using De Casteljau's algorithm.
        /// </summary>
        public static Vector3F CubicBezierInterpolate(Vector3F p0, Vector3F p1, Vector3F p2, Vector3F p3, float t)
        {
            float u = 1f - t;
            float u2 = u * u;
            float u3 = u2 * u;
            float t2 = t * t;
            float t3 = t2 * t;

            return p0 * u3 + p1 * (3f * u2 * t) + p2 * (3f * u * t2) + p3 * t3;
        }

        /// <summary>
        /// B-Spline interpolation (uniform cubic B-spline basis).
        /// </summary>
        public static Vector3F BSplineInterpolate(Vector3F p0, Vector3F p1, Vector3F p2, Vector3F p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float b0 = (-t3 + 3f * t2 - 3f * t + 1f) / 6f;
            float b1 = (3f * t3 - 6f * t2 + 4f) / 6f;
            float b2 = (-3f * t3 + 3f * t2 + 3f * t + 1f) / 6f;
            float b3 = t3 / 6f;

            return p0 * b0 + p1 * b1 + p2 * b2 + p3 * b3;
        }

        // ==================================================
        // Arc Length Parameterization
        // ==================================================

        private void RecalculateLengths()
        {
            if (_points.Count < 2)
            {
                _segmentLengths = Array.Empty<float>();
                _totalLength = 0;
                _isDirty = false;
                return;
            }

            const int samplesPerSegment = 20;
            int segments = _points.Count - 1;
            int totalSamples = segments * samplesPerSegment;
            _segmentLengths = new float[totalSamples];
            _totalLength = 0;

            Vector3F prevPoint = Evaluate(0);
            for (int i = 1; i <= totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                Vector3F currentPoint = Evaluate(t);
                float dist = (currentPoint - prevPoint).Magnitude();
                _totalLength += dist;
                _segmentLengths[i - 1] = _totalLength;
                prevPoint = currentPoint;
            }

            _isDirty = false;
        }

        private float DistanceToParameter(float distance)
        {
            distance = MathUtils.Clamp(distance, 0, _totalLength);

            // Binary search for the segment containing this distance
            int lo = 0, hi = _segmentLengths.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (_segmentLengths[mid] < distance) lo = mid + 1;
                else hi = mid;
            }

            float prevDist = lo > 0 ? _segmentLengths[lo - 1] : 0;
            float segLen = _segmentLengths[lo] - prevDist;
            float segFrac = segLen > 0 ? (distance - prevDist) / segLen : 0;

            return ((float)lo + segFrac) / _segmentLengths.Length;
        }
    }

    /// <summary>
    /// A gradient class for color interpolation over a normalized [0..1] range.
    /// Used for health bar colors, terrain coloring, skybox gradients, etc.
    /// </summary>
    public class GradientF
    {
        /// <summary>Represents a color stop in the gradient.</summary>
        public struct ColorStop
        {
            public float Position;
            public float R, G, B, A;

            public ColorStop(float position, float r, float g, float b, float a = 1f)
            {
                Position = position;
                R = r; G = g; B = b; A = a;
            }
        }

        private readonly List<ColorStop> _stops = new List<ColorStop>();

        /// <summary>Creates an empty gradient.</summary>
        public GradientF() { }

        /// <summary>
        /// Adds a color stop at the given position [0..1].
        /// </summary>
        public void AddStop(float position, float r, float g, float b, float a = 1f)
        {
            _stops.Add(new ColorStop(position, r, g, b, a));
            _stops.Sort((x, y) => x.Position.CompareTo(y.Position));
        }

        /// <summary>
        /// Evaluates the gradient at position t ∈ [0, 1].
        /// Returns (R, G, B, A) tuple.
        /// </summary>
        public (float R, float G, float B, float A) Evaluate(float t)
        {
            if (_stops.Count == 0) return (0, 0, 0, 1);
            if (_stops.Count == 1) return (_stops[0].R, _stops[0].G, _stops[0].B, _stops[0].A);

            if (t <= _stops[0].Position) return (_stops[0].R, _stops[0].G, _stops[0].B, _stops[0].A);
            if (t >= _stops[^1].Position) return (_stops[^1].R, _stops[^1].G, _stops[^1].B, _stops[^1].A);

            for (int i = 0; i < _stops.Count - 1; i++)
            {
                if (t >= _stops[i].Position && t <= _stops[i + 1].Position)
                {
                    float localT = (t - _stops[i].Position) / (_stops[i + 1].Position - _stops[i].Position);
                    return (
                        MathUtils.Lerp(_stops[i].R, _stops[i + 1].R, localT),
                        MathUtils.Lerp(_stops[i].G, _stops[i + 1].G, localT),
                        MathUtils.Lerp(_stops[i].B, _stops[i + 1].B, localT),
                        MathUtils.Lerp(_stops[i].A, _stops[i + 1].A, localT)
                    );
                }
            }

            return (0, 0, 0, 1);
        }

        /// <summary>Creates a health-bar gradient (green → yellow → red).</summary>
        public static GradientF HealthGradient()
        {
            var g = new GradientF();
            g.AddStop(0f, 0.8f, 0f, 0f);       // Red (low health)
            g.AddStop(0.3f, 1f, 0.6f, 0f);      // Orange
            g.AddStop(0.5f, 1f, 1f, 0f);         // Yellow
            g.AddStop(1f, 0f, 0.9f, 0.2f);       // Green (full health)
            return g;
        }

        /// <summary>Creates a terrain height gradient (water → sand → grass → rock → snow).</summary>
        public static GradientF TerrainGradient()
        {
            var g = new GradientF();
            g.AddStop(0f, 0.05f, 0.15f, 0.4f);   // Deep water
            g.AddStop(0.3f, 0.1f, 0.3f, 0.6f);    // Shallow water
            g.AddStop(0.35f, 0.76f, 0.7f, 0.5f);   // Sand
            g.AddStop(0.5f, 0.2f, 0.5f, 0.1f);     // Grass
            g.AddStop(0.7f, 0.3f, 0.25f, 0.15f);   // Rock
            g.AddStop(0.85f, 0.5f, 0.45f, 0.4f);   // Mountain
            g.AddStop(1f, 0.95f, 0.95f, 0.97f);    // Snow
            return g;
        }
    }
}
