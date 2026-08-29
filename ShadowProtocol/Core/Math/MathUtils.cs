// =============================================================================
// ShadowProtocol.Core — MathUtils: Common math utilities for game development
// =============================================================================

using System;
using System.Runtime.CompilerServices;

namespace ShadowProtocol.Core.Math
{
    /// <summary>
    /// Provides essential math utility functions commonly used across the game engine,
    /// including interpolation, clamping, angle operations, random distributions,
    /// and geometric calculations.
    /// </summary>
    public static class MathUtils
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>Degrees-to-radians conversion factor.</summary>
        public const float Deg2Rad = MathF.PI / 180f;

        /// <summary>Radians-to-degrees conversion factor.</summary>
        public const float Rad2Deg = 180f / MathF.PI;

        /// <summary>Two times PI (full circle in radians).</summary>
        public const float TwoPi = MathF.PI * 2f;

        /// <summary>Half PI (quarter circle in radians).</summary>
        public const float HalfPi = MathF.PI * 0.5f;

        /// <summary>Golden ratio φ = (1 + √5) / 2 ≈ 1.618.</summary>
        public const float GoldenRatio = 1.6180339887f;

        /// <summary>Small epsilon for floating point comparisons.</summary>
        public const float Epsilon = 1e-6f;

        /// <summary>Square root of 2.</summary>
        public const float Sqrt2 = 1.41421356237f;

        /// <summary>Square root of 3.</summary>
        public const float Sqrt3 = 1.73205080757f;

        // =====================================================================
        // Clamping and Remapping
        // =====================================================================

        /// <summary>
        /// Clamps a value between a minimum and maximum range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Clamps a value to the [0, 1] range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        /// <summary>
        /// Clamps an integer value between min and max.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Remaps a value from one range to another.
        /// </summary>
        /// <param name="value">The value to remap.</param>
        /// <param name="fromMin">Source range minimum.</param>
        /// <param name="fromMax">Source range maximum.</param>
        /// <param name="toMin">Target range minimum.</param>
        /// <param name="toMax">Target range maximum.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = (value - fromMin) / (fromMax - fromMin);
            return toMin + t * (toMax - toMin);
        }

        /// <summary>
        /// Remaps and clamps a value from one range to another.
        /// </summary>
        public static float RemapClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Clamp01((value - fromMin) / (fromMax - fromMin));
            return toMin + t * (toMax - toMin);
        }

        /// <summary>
        /// Returns the fractional part of a float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Frac(float value) => value - MathF.Floor(value);

        /// <summary>
        /// Wraps a value to stay within [min, max) range.
        /// </summary>
        public static float Wrap(float value, float min, float max)
        {
            float range = max - min;
            if (range <= 0) return min;
            float result = ((value - min) % range + range) % range + min;
            return result;
        }

        /// <summary>
        /// Wraps an integer value to stay within [min, max) range.
        /// </summary>
        public static int Wrap(int value, int min, int max)
        {
            int range = max - min;
            if (range <= 0) return min;
            return ((value - min) % range + range) % range + min;
        }

        // =====================================================================
        // Interpolation
        // =====================================================================

        /// <summary>
        /// Linear interpolation between two values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>
        /// Linear interpolation with clamped t value [0,1].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpClamped(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>
        /// Inverse linear interpolation — returns t such that Lerp(a, b, t) == value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InverseLerp(float a, float b, float value)
        {
            if (MathF.Abs(b - a) < Epsilon) return 0f;
            return (value - a) / (b - a);
        }

        /// <summary>
        /// Smoothly interpolates between 0 and 1 using Hermite interpolation (3t² - 2t³).
        /// Provides smooth acceleration and deceleration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Ken Perlin's improved smoothstep function (6t⁵ - 15t⁴ + 10t³).
        /// Has zero first AND second derivatives at the endpoints.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmootherStep(float edge0, float edge1, float x)
        {
            float t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>
        /// Moves a value towards a target by a maximum delta per step.
        /// Useful for smooth value convergence in game loops.
        /// </summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta) return target;
            return current + MathF.Sign(target - current) * maxDelta;
        }

        /// <summary>
        /// Exponential decay interpolation — smooth framerate-independent dampening.
        /// Use this instead of Lerp for framerate-independent smoothing.
        /// </summary>
        /// <param name="current">Current value.</param>
        /// <param name="target">Target value.</param>
        /// <param name="halfLife">Time in seconds for the value to reach halfway to the target.</param>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public static float ExpDecay(float current, float target, float halfLife, float deltaTime)
        {
            if (halfLife <= 0) return target;
            float factor = 1f - MathF.Pow(0.5f, deltaTime / halfLife);
            return current + (target - current) * factor;
        }

        /// <summary>
        /// Spring-based damping interpolation. Simulates a critically damped spring.
        /// Provides natural-feeling motion with slight overshoot possibility.
        /// </summary>
        /// <param name="current">Current value.</param>
        /// <param name="target">Target value.</param>
        /// <param name="velocity">Current velocity (modified in-place by reference).</param>
        /// <param name="smoothTime">Approximate time to reach the target.</param>
        /// <param name="deltaTime">Frame delta time.</param>
        /// <param name="maxSpeed">Optional maximum speed limit.</param>
        public static float SmoothDamp(float current, float target, ref float velocity,
            float smoothTime, float deltaTime, float maxSpeed = float.MaxValue)
        {
            smoothTime = MathF.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            float change = current - target;
            float originalTo = target;

            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;

            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if (originalTo - current > 0f == output > originalTo)
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        // =====================================================================
        // Easing Functions
        // =====================================================================

        /// <summary>Quadratic ease-in: t²</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuad(float t) => t * t;

        /// <summary>Quadratic ease-out: 1 - (1-t)²</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Quadratic ease-in-out.</summary>
        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>Cubic ease-in: t³</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInCubic(float t) => t * t * t;

        /// <summary>Cubic ease-out: 1 - (1-t)³</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }

        /// <summary>Cubic ease-in-out.</summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>Quartic ease-in: t⁴</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuart(float t) => t * t * t * t;

        /// <summary>Quartic ease-out.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuart(float t) { float u = 1f - t; return 1f - u * u * u * u; }

        /// <summary>Elastic ease-in (spring-like bounce at start).</summary>
        public static float EaseInElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * (TwoPi / 3f));
        }

        /// <summary>Elastic ease-out (spring-like bounce at end).</summary>
        public static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * (TwoPi / 3f)) + 1f;
        }

        /// <summary>Bounce ease-out (ball-drop bounce effect).</summary>
        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        /// <summary>Bounce ease-in.</summary>
        public static float EaseInBounce(float t) => 1f - EaseOutBounce(1f - t);

        /// <summary>Back ease-in (slight overshoot at start).</summary>
        public static float EaseInBack(float t)
        {
            const float c = 1.70158f;
            return (c + 1f) * t * t * t - c * t * t;
        }

        /// <summary>Back ease-out (slight overshoot at end).</summary>
        public static float EaseOutBack(float t)
        {
            const float c = 1.70158f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        /// <summary>Sinusoidal ease-in.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInSine(float t) => 1f - MathF.Cos(t * HalfPi);

        /// <summary>Sinusoidal ease-out.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutSine(float t) => MathF.Sin(t * HalfPi);

        /// <summary>Exponential ease-in.</summary>
        public static float EaseInExpo(float t) => t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);

        /// <summary>Exponential ease-out.</summary>
        public static float EaseOutExpo(float t) => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);

        // =====================================================================
        // Angle Operations
        // =====================================================================

        /// <summary>
        /// Normalizes an angle to the range [-π, π].
        /// </summary>
        public static float NormalizeAngle(float radians)
        {
            radians = ((radians % TwoPi) + TwoPi) % TwoPi;
            if (radians > MathF.PI) radians -= TwoPi;
            return radians;
        }

        /// <summary>
        /// Normalizes a degree angle to the range [0, 360).
        /// </summary>
        public static float NormalizeDegrees(float degrees)
        {
            degrees = degrees % 360f;
            if (degrees < 0) degrees += 360f;
            return degrees;
        }

        /// <summary>
        /// Returns the shortest angular difference between two angles in radians.
        /// Result is in range [-π, π].
        /// </summary>
        public static float DeltaAngle(float current, float target)
        {
            float delta = ((target - current) % TwoPi + TwoPi + MathF.PI) % TwoPi - MathF.PI;
            return delta;
        }

        /// <summary>
        /// Linearly interpolates between two angles in radians, taking the shortest path.
        /// </summary>
        public static float LerpAngle(float a, float b, float t)
        {
            float delta = DeltaAngle(a, b);
            return a + delta * Clamp01(t);
        }

        /// <summary>
        /// Moves an angle towards a target angle by a maximum step, taking the shortest path.
        /// </summary>
        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = DeltaAngle(current, target);
            if (-maxDelta < delta && delta < maxDelta) return target;
            return current + Clamp(delta, -maxDelta, maxDelta);
        }

        // =====================================================================
        // Geometric Utilities
        // =====================================================================

        /// <summary>
        /// Computes the signed area of a 2D triangle.
        /// Positive if vertices are in counter-clockwise order.
        /// </summary>
        public static float TriangleArea2D(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            return (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) * 0.5f;
        }

        /// <summary>
        /// Computes barycentric coordinates of a point relative to a triangle.
        /// Returns (u, v, w) where p = u*a + v*b + w*c.
        /// </summary>
        public static (float u, float v, float w) Barycentric(
            Vector3F p, Vector3F a, Vector3F b, Vector3F c)
        {
            Vector3F v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector3F.Dot(v0, v0);
            float d01 = Vector3F.Dot(v0, v1);
            float d11 = Vector3F.Dot(v1, v1);
            float d20 = Vector3F.Dot(v2, v0);
            float d21 = Vector3F.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            if (MathF.Abs(denom) < Epsilon)
                return (1f / 3f, 1f / 3f, 1f / 3f);

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1f - v - w;

            return (u, v, w);
        }

        /// <summary>
        /// Tests if a point lies inside a triangle (2D).
        /// Uses barycentric coordinate method.
        /// </summary>
        public static bool PointInTriangle2D(float px, float py,
            float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = Sign2D(px, py, ax, ay, bx, by);
            float d2 = Sign2D(px, py, bx, by, cx, cy);
            float d3 = Sign2D(px, py, cx, cy, ax, ay);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private static float Sign2D(float px, float py, float ax, float ay, float bx, float by)
        {
            return (px - bx) * (ay - by) - (ax - bx) * (py - by);
        }

        /// <summary>
        /// Computes the closest point on a line segment to a given point.
        /// </summary>
        public static Vector3F ClosestPointOnSegment(Vector3F point, Vector3F segA, Vector3F segB)
        {
            Vector3F ab = segB - segA;
            float abLenSq = ab.MagnitudeSquared();
            if (abLenSq < Epsilon) return segA;

            float t = Clamp01(Vector3F.Dot(point - segA, ab) / abLenSq);
            return segA + ab * t;
        }

        /// <summary>
        /// Computes the squared distance from a point to a line segment.
        /// </summary>
        public static float PointToSegmentDistanceSq(Vector3F point, Vector3F segA, Vector3F segB)
        {
            Vector3F closest = ClosestPointOnSegment(point, segA, segB);
            return (point - closest).MagnitudeSquared();
        }

        /// <summary>
        /// Tests if a ray intersects a sphere.
        /// </summary>
        /// <param name="origin">Ray origin.</param>
        /// <param name="direction">Ray direction (must be normalized).</param>
        /// <param name="center">Sphere center.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <param name="hitDistance">Distance along the ray to the first intersection.</param>
        public static bool RaySphereIntersect(Vector3F origin, Vector3F direction,
            Vector3F center, float radius, out float hitDistance)
        {
            Vector3F oc = origin - center;
            float b = Vector3F.Dot(oc, direction);
            float c = Vector3F.Dot(oc, oc) - radius * radius;
            float discriminant = b * b - c;

            if (discriminant < 0)
            {
                hitDistance = float.MaxValue;
                return false;
            }

            float sqrtDisc = MathF.Sqrt(discriminant);
            float t0 = -b - sqrtDisc;
            float t1 = -b + sqrtDisc;

            if (t1 < 0)
            {
                hitDistance = float.MaxValue;
                return false;
            }

            hitDistance = t0 >= 0 ? t0 : t1;
            return true;
        }

        /// <summary>
        /// Tests if a ray intersects an axis-aligned bounding box (AABB).
        /// Uses the slab method for efficient intersection testing.
        /// </summary>
        public static bool RayAABBIntersect(Vector3F origin, Vector3F invDirection,
            Vector3F aabbMin, Vector3F aabbMax, out float tMin, out float tMax)
        {
            float t1 = (aabbMin.X - origin.X) * invDirection.X;
            float t2 = (aabbMax.X - origin.X) * invDirection.X;
            tMin = MathF.Min(t1, t2);
            tMax = MathF.Max(t1, t2);

            t1 = (aabbMin.Y - origin.Y) * invDirection.Y;
            t2 = (aabbMax.Y - origin.Y) * invDirection.Y;
            tMin = MathF.Max(tMin, MathF.Min(t1, t2));
            tMax = MathF.Min(tMax, MathF.Max(t1, t2));

            t1 = (aabbMin.Z - origin.Z) * invDirection.Z;
            t2 = (aabbMax.Z - origin.Z) * invDirection.Z;
            tMin = MathF.Max(tMin, MathF.Min(t1, t2));
            tMax = MathF.Min(tMax, MathF.Max(t1, t2));

            return tMax >= MathF.Max(tMin, 0f);
        }

        /// <summary>
        /// Ray-plane intersection test. Plane is defined by a normal and distance from origin.
        /// </summary>
        public static bool RayPlaneIntersect(Vector3F origin, Vector3F direction,
            Vector3F planeNormal, float planeDistance, out float hitT)
        {
            float denom = Vector3F.Dot(planeNormal, direction);
            if (MathF.Abs(denom) < Epsilon)
            {
                hitT = 0;
                return false;
            }

            hitT = -(Vector3F.Dot(planeNormal, origin) + planeDistance) / denom;
            return hitT >= 0;
        }

        // =====================================================================
        // Random Distributions
        // =====================================================================

        private static readonly Random _random = new Random();

        /// <summary>
        /// Returns a random float in the range [min, max].
        /// </summary>
        public static float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Returns a random integer in the range [min, max] (inclusive).
        /// </summary>
        public static int RandomRange(int min, int max)
        {
            return _random.Next(min, max + 1);
        }

        /// <summary>
        /// Returns a random point on the surface of a unit sphere.
        /// Uses uniform distribution via Marsaglia's method.
        /// </summary>
        public static Vector3F RandomOnUnitSphere()
        {
            float u = RandomRange(-1f, 1f);
            float theta = RandomRange(0f, TwoPi);
            float r = MathF.Sqrt(1f - u * u);
            return new Vector3F(r * MathF.Cos(theta), r * MathF.Sin(theta), u);
        }

        /// <summary>
        /// Returns a random point inside a unit sphere.
        /// Uses rejection sampling.
        /// </summary>
        public static Vector3F RandomInsideUnitSphere()
        {
            Vector3F point;
            do
            {
                point = new Vector3F(
                    RandomRange(-1f, 1f),
                    RandomRange(-1f, 1f),
                    RandomRange(-1f, 1f)
                );
            } while (point.MagnitudeSquared() > 1f);

            return point;
        }

        /// <summary>
        /// Returns a random point inside a unit circle (XY plane).
        /// </summary>
        public static Vector3F RandomInsideUnitCircle()
        {
            float angle = RandomRange(0f, TwoPi);
            float radius = MathF.Sqrt(RandomRange(0f, 1f));
            return new Vector3F(radius * MathF.Cos(angle), radius * MathF.Sin(angle), 0);
        }

        /// <summary>
        /// Returns a random value from a Gaussian (normal) distribution.
        /// Uses Box-Muller transform.
        /// </summary>
        /// <param name="mean">Mean of the distribution.</param>
        /// <param name="standardDeviation">Standard deviation of the distribution.</param>
        public static float RandomGaussian(float mean = 0f, float standardDeviation = 1f)
        {
            float u1 = 1f - (float)_random.NextDouble();
            float u2 = (float)_random.NextDouble();
            float normal = MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Sin(TwoPi * u2);
            return mean + standardDeviation * normal;
        }

        /// <summary>
        /// Returns true with the given probability [0..1].
        /// </summary>
        public static bool RandomChance(float probability)
        {
            return (float)_random.NextDouble() < probability;
        }

        /// <summary>
        /// Selects a random index from a list of weights (weighted random selection).
        /// </summary>
        public static int WeightedRandom(float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += weights[i];

            float roll = RandomRange(0f, total);
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return i;
            }

            return weights.Length - 1;
        }

        // =====================================================================
        // Floating Point Comparisons
        // =====================================================================

        /// <summary>
        /// Checks if two floats are approximately equal within an epsilon.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float a, float b, float epsilon = Epsilon)
        {
            return MathF.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// Checks if a float is approximately zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(float value, float epsilon = Epsilon)
        {
            return MathF.Abs(value) < epsilon;
        }

        /// <summary>
        /// Returns the sign of a value as -1, 0, or 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(float value)
        {
            if (value > Epsilon) return 1;
            if (value < -Epsilon) return -1;
            return 0;
        }

        // =====================================================================
        // Hashing Utilities
        // =====================================================================

        /// <summary>
        /// Combines two hash codes into one using a mixing function.
        /// Useful for spatial hashing and procedural generation.
        /// </summary>
        public static int HashCombine(int seed, int value)
        {
            return seed ^ (value + (int)0x9e3779b9 + (seed << 6) + (seed >> 2));
        }

        /// <summary>
        /// Integer hash function based on Robert Jenkins' 32-bit integer hash.
        /// Good distribution for procedural generation.
        /// </summary>
        public static uint JenkinsHash(uint key)
        {
            key = (key + 0x7ed55d16u) + (key << 12);
            key = (key ^ 0xc761c23cu) ^ (key >> 19);
            key = (key + 0x165667b1u) + (key << 5);
            key = (key + 0xd3a2646cu) ^ (key << 9);
            key = (key + 0xfd7046c5u) + (key << 3);
            key = (key ^ 0xb55a4f09u) ^ (key >> 16);
            return key;
        }

        /// <summary>
        /// Fast spatial hash for 2D coordinates. Returns a value in [0, 1].
        /// </summary>
        public static float SpatialHash2D(int x, int y)
        {
            uint hash = JenkinsHash((uint)(x * 73856093 ^ y * 19349663));
            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Fast spatial hash for 3D coordinates. Returns a value in [0, 1].
        /// </summary>
        public static float SpatialHash3D(int x, int y, int z)
        {
            uint hash = JenkinsHash((uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791));
            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        // =====================================================================
        // Bitwise Utilities
        // =====================================================================

        /// <summary>
        /// Returns true if the value is a power of two.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

        /// <summary>
        /// Rounds up to the next power of two.
        /// </summary>
        public static int NextPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        /// <summary>
        /// Fast integer log base 2 (floor).
        /// </summary>
        public static int Log2(int value)
        {
            int result = 0;
            while (value > 1) { value >>= 1; result++; }
            return result;
        }
    }
}
