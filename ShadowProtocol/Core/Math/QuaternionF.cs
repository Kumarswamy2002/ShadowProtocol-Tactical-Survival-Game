// =============================================================================
// ShadowProtocol.Core — QuaternionF: Quaternion for rotation representation
// =============================================================================

using System;
using System.Runtime.CompilerServices;

namespace ShadowProtocol.Core.Math
{
    /// <summary>
    /// A quaternion representing a 3D rotation. Uses the convention (X, Y, Z, W)
    /// where W is the scalar component. Quaternions provide smooth interpolation
    /// and avoid gimbal lock compared to Euler angles.
    /// </summary>
    public struct QuaternionF : IEquatable<QuaternionF>
    {
        // =====================================================================
        // Fields
        // =====================================================================

        /// <summary>The X component of the imaginary vector part.</summary>
        public float X;

        /// <summary>The Y component of the imaginary vector part.</summary>
        public float Y;

        /// <summary>The Z component of the imaginary vector part.</summary>
        public float Z;

        /// <summary>The W (scalar) component.</summary>
        public float W;

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        /// Constructs a quaternion from individual components.
        /// </summary>
        public QuaternionF(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        /// <summary>
        /// Constructs a quaternion from a vector part and a scalar.
        /// </summary>
        public QuaternionF(Vector3F vectorPart, float scalarPart)
        {
            X = vectorPart.X; Y = vectorPart.Y; Z = vectorPart.Z; W = scalarPart;
        }

        // =====================================================================
        // Static Predefined Quaternions
        // =====================================================================

        /// <summary>The identity quaternion (no rotation).</summary>
        public static readonly QuaternionF Identity = new QuaternionF(0, 0, 0, 1);

        /// <summary>A zero quaternion (not a valid rotation).</summary>
        public static readonly QuaternionF Zero = new QuaternionF(0, 0, 0, 0);

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Gets the squared length (magnitude squared) of the quaternion.
        /// More efficient than Magnitude when only relative comparison is needed.
        /// </summary>
        public float MagnitudeSquared => X * X + Y * Y + Z * Z + W * W;

        /// <summary>
        /// Gets the length (magnitude) of the quaternion.
        /// A unit quaternion has magnitude of 1 and represents a valid rotation.
        /// </summary>
        public float Magnitude => MathF.Sqrt(MagnitudeSquared);

        /// <summary>
        /// Returns the conjugate of this quaternion (negated imaginary part).
        /// For unit quaternions, the conjugate equals the inverse.
        /// </summary>
        public QuaternionF Conjugate => new QuaternionF(-X, -Y, -Z, W);

        /// <summary>
        /// Returns the imaginary vector part (X, Y, Z).
        /// </summary>
        public Vector3F VectorPart => new Vector3F(X, Y, Z);

        /// <summary>
        /// Returns true if this is approximately a unit quaternion.
        /// </summary>
        public bool IsNormalized => MathF.Abs(MagnitudeSquared - 1f) < 1e-5f;

        /// <summary>
        /// Returns the axis of rotation represented by this quaternion.
        /// Returns Vector3F.UnitY if the quaternion represents no rotation.
        /// </summary>
        public Vector3F Axis
        {
            get
            {
                float sinHalfAngleSq = X * X + Y * Y + Z * Z;
                if (sinHalfAngleSq < 1e-10f)
                    return new Vector3F(0, 1, 0); // No rotation, return arbitrary axis
                float invSinHalf = 1f / MathF.Sqrt(sinHalfAngleSq);
                return new Vector3F(X * invSinHalf, Y * invSinHalf, Z * invSinHalf);
            }
        }

        /// <summary>
        /// Returns the angle of rotation in radians (0 to 2π).
        /// </summary>
        public float Angle => 2f * MathF.Acos(MathF.Min(MathF.Abs(W), 1f));

        // =====================================================================
        // Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a quaternion from an axis and angle (in radians).
        /// The axis does not need to be normalized.
        /// </summary>
        public static QuaternionF FromAxisAngle(Vector3F axis, float radians)
        {
            float length = axis.Magnitude();
            if (length < 1e-6f) return Identity;

            float invLen = 1f / length;
            float halfAngle = radians * 0.5f;
            float sinHalf = MathF.Sin(halfAngle);
            float cosHalf = MathF.Cos(halfAngle);

            return new QuaternionF(
                axis.X * invLen * sinHalf,
                axis.Y * invLen * sinHalf,
                axis.Z * invLen * sinHalf,
                cosHalf
            );
        }

        /// <summary>
        /// Creates a quaternion from Euler angles in radians (YXZ order: yaw → pitch → roll).
        /// This is the most common order for game cameras and characters.
        /// </summary>
        /// <param name="yaw">Rotation around Y axis in radians.</param>
        /// <param name="pitch">Rotation around X axis in radians.</param>
        /// <param name="roll">Rotation around Z axis in radians.</param>
        public static QuaternionF FromEuler(float yaw, float pitch, float roll)
        {
            float cy = MathF.Cos(yaw * 0.5f), sy = MathF.Sin(yaw * 0.5f);
            float cp = MathF.Cos(pitch * 0.5f), sp = MathF.Sin(pitch * 0.5f);
            float cr = MathF.Cos(roll * 0.5f), sr = MathF.Sin(roll * 0.5f);

            return new QuaternionF(
                cy * sp * cr + sy * cp * sr,
                sy * cp * cr - cy * sp * sr,
                cy * cp * sr - sy * sp * cr,
                cy * cp * cr + sy * sp * sr
            );
        }

        /// <summary>
        /// Creates a quaternion from Euler angles provided as a Vector3F (pitch, yaw, roll) in radians.
        /// </summary>
        public static QuaternionF FromEulerAngles(Vector3F eulerRadians)
        {
            return FromEuler(eulerRadians.Y, eulerRadians.X, eulerRadians.Z);
        }

        /// <summary>
        /// Creates a quaternion that rotates from one direction to another.
        /// Both directions should be normalized.
        /// </summary>
        public static QuaternionF FromToRotation(Vector3F fromDirection, Vector3F toDirection)
        {
            float dot = Vector3F.Dot(fromDirection, toDirection);

            if (dot > 0.999999f)
                return Identity;

            if (dot < -0.999999f)
            {
                // 180-degree rotation: find a perpendicular axis
                Vector3F axis = Vector3F.Cross(new Vector3F(1, 0, 0), fromDirection);
                if (axis.MagnitudeSquared() < 1e-6f)
                    axis = Vector3F.Cross(new Vector3F(0, 1, 0), fromDirection);
                axis = axis.Normalized();
                return new QuaternionF(axis.X, axis.Y, axis.Z, 0); // 180 degrees
            }

            Vector3F cross = Vector3F.Cross(fromDirection, toDirection);
            float w = 1f + dot;
            float invLen = 1f / MathF.Sqrt(cross.X * cross.X + cross.Y * cross.Y + cross.Z * cross.Z + w * w);

            return new QuaternionF(
                cross.X * invLen,
                cross.Y * invLen,
                cross.Z * invLen,
                w * invLen
            );
        }

        /// <summary>
        /// Creates a quaternion that represents looking in the specified forward direction.
        /// </summary>
        /// <param name="forward">The forward look direction (must be normalized).</param>
        /// <param name="up">The reference up direction.</param>
        public static QuaternionF LookRotation(Vector3F forward, Vector3F up)
        {
            Vector3F zAxis = forward.Normalized();
            Vector3F xAxis = Vector3F.Cross(up, zAxis).Normalized();
            Vector3F yAxis = Vector3F.Cross(zAxis, xAxis);

            float m00 = xAxis.X, m01 = yAxis.X, m02 = zAxis.X;
            float m10 = xAxis.Y, m11 = yAxis.Y, m12 = zAxis.Y;
            float m20 = xAxis.Z, m21 = yAxis.Z, m22 = zAxis.Z;

            float trace = m00 + m11 + m22;
            QuaternionF q;

            if (trace > 0)
            {
                float s = 0.5f / MathF.Sqrt(trace + 1f);
                q = new QuaternionF(
                    (m21 - m12) * s,
                    (m02 - m20) * s,
                    (m10 - m01) * s,
                    0.25f / s
                );
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = 2f * MathF.Sqrt(1f + m00 - m11 - m22);
                q = new QuaternionF(
                    0.25f * s,
                    (m01 + m10) / s,
                    (m02 + m20) / s,
                    (m21 - m12) / s
                );
            }
            else if (m11 > m22)
            {
                float s = 2f * MathF.Sqrt(1f + m11 - m00 - m22);
                q = new QuaternionF(
                    (m01 + m10) / s,
                    0.25f * s,
                    (m12 + m21) / s,
                    (m02 - m20) / s
                );
            }
            else
            {
                float s = 2f * MathF.Sqrt(1f + m22 - m00 - m11);
                q = new QuaternionF(
                    (m02 + m20) / s,
                    (m12 + m21) / s,
                    0.25f * s,
                    (m10 - m01) / s
                );
            }

            return q.Normalized();
        }

        /// <summary>
        /// Creates a quaternion from a 4×4 rotation matrix.
        /// Extracts rotation from the upper-left 3×3 submatrix.
        /// </summary>
        public static QuaternionF FromRotationMatrix(Matrix4x4F m)
        {
            float trace = m.M00 + m.M11 + m.M22;
            QuaternionF q;

            if (trace > 0)
            {
                float s = MathF.Sqrt(trace + 1f) * 2f;
                q = new QuaternionF(
                    (m.M21 - m.M12) / s,
                    (m.M02 - m.M20) / s,
                    (m.M10 - m.M01) / s,
                    0.25f * s
                );
            }
            else if (m.M00 > m.M11 && m.M00 > m.M22)
            {
                float s = MathF.Sqrt(1f + m.M00 - m.M11 - m.M22) * 2f;
                q = new QuaternionF(
                    0.25f * s,
                    (m.M01 + m.M10) / s,
                    (m.M02 + m.M20) / s,
                    (m.M21 - m.M12) / s
                );
            }
            else if (m.M11 > m.M22)
            {
                float s = MathF.Sqrt(1f + m.M11 - m.M00 - m.M22) * 2f;
                q = new QuaternionF(
                    (m.M01 + m.M10) / s,
                    0.25f * s,
                    (m.M12 + m.M21) / s,
                    (m.M02 - m.M20) / s
                );
            }
            else
            {
                float s = MathF.Sqrt(1f + m.M22 - m.M00 - m.M11) * 2f;
                q = new QuaternionF(
                    (m.M02 + m.M20) / s,
                    (m.M12 + m.M21) / s,
                    0.25f * s,
                    (m.M10 - m.M01) / s
                );
            }

            return q.Normalized();
        }

        // =====================================================================
        // Core Operations
        // =====================================================================

        /// <summary>
        /// Returns a normalized (unit length) version of this quaternion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QuaternionF Normalized()
        {
            float mag = Magnitude;
            if (mag < 1e-10f) return Identity;
            float inv = 1f / mag;
            return new QuaternionF(X * inv, Y * inv, Z * inv, W * inv);
        }

        /// <summary>
        /// Returns the inverse of this quaternion.
        /// For unit quaternions, this is equivalent to the conjugate.
        /// </summary>
        public QuaternionF Inverse()
        {
            float magSq = MagnitudeSquared;
            if (magSq < 1e-10f) return Identity;
            float inv = 1f / magSq;
            return new QuaternionF(-X * inv, -Y * inv, -Z * inv, W * inv);
        }

        /// <summary>
        /// Rotates a 3D vector by this quaternion using the sandwich product: q * v * q⁻¹.
        /// This is the primary way to apply a quaternion rotation to a point or direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3F Rotate(Vector3F v)
        {
            // Optimized rotation using the formula:
            // result = v + 2 * w * (q_vec × v) + 2 * (q_vec × (q_vec × v))
            Vector3F qv = new Vector3F(X, Y, Z);
            Vector3F cross1 = Vector3F.Cross(qv, v);
            Vector3F cross2 = Vector3F.Cross(qv, cross1);
            return v + (cross1 * (2f * W)) + (cross2 * 2f);
        }

        /// <summary>
        /// Computes the dot product of two quaternions.
        /// Used to determine the "similarity" between two rotations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(QuaternionF a, QuaternionF b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        }

        /// <summary>
        /// Returns the angular difference between two quaternions in radians.
        /// </summary>
        public static float AngleBetween(QuaternionF a, QuaternionF b)
        {
            float dot = MathF.Abs(Dot(a, b));
            return 2f * MathF.Acos(MathF.Min(dot, 1f));
        }

        // =====================================================================
        // Interpolation
        // =====================================================================

        /// <summary>
        /// Linearly interpolates between two quaternions and normalizes the result.
        /// Faster than Slerp but less accurate for large angular differences.
        /// </summary>
        public static QuaternionF Lerp(QuaternionF a, QuaternionF b, float t)
        {
            // Ensure shortest path
            if (Dot(a, b) < 0)
                b = new QuaternionF(-b.X, -b.Y, -b.Z, -b.W);

            return new QuaternionF(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            ).Normalized();
        }

        /// <summary>
        /// Spherical linear interpolation between two quaternions.
        /// Provides constant-speed rotation along the great arc between the two orientations.
        /// </summary>
        public static QuaternionF Slerp(QuaternionF a, QuaternionF b, float t)
        {
            float dot = Dot(a, b);

            // Ensure shortest path
            if (dot < 0)
            {
                b = new QuaternionF(-b.X, -b.Y, -b.Z, -b.W);
                dot = -dot;
            }

            // If quaternions are very close, use linear interpolation to avoid division by zero
            if (dot > 0.9995f)
                return Lerp(a, b, t);

            float theta = MathF.Acos(dot);
            float sinTheta = MathF.Sin(theta);
            float wa = MathF.Sin((1f - t) * theta) / sinTheta;
            float wb = MathF.Sin(t * theta) / sinTheta;

            return new QuaternionF(
                wa * a.X + wb * b.X,
                wa * a.Y + wb * b.Y,
                wa * a.Z + wb * b.Z,
                wa * a.W + wb * b.W
            );
        }

        /// <summary>
        /// Spherical quadratic interpolation using squad algorithm.
        /// Provides smooth C1 continuous interpolation through a series of keyframe rotations.
        /// </summary>
        /// <param name="q0">Previous keyframe rotation.</param>
        /// <param name="q1">Start rotation.</param>
        /// <param name="q2">End rotation.</param>
        /// <param name="q3">Next keyframe rotation.</param>
        /// <param name="t">Interpolation factor [0..1].</param>
        public static QuaternionF Squad(QuaternionF q0, QuaternionF q1, QuaternionF q2, QuaternionF q3, float t)
        {
            QuaternionF s1 = ComputeSquadIntermediate(q0, q1, q2);
            QuaternionF s2 = ComputeSquadIntermediate(q1, q2, q3);
            return Slerp(Slerp(q1, q2, t), Slerp(s1, s2, t), 2f * t * (1f - t));
        }

        /// <summary>
        /// Computes the intermediate control quaternion for Squad interpolation.
        /// </summary>
        private static QuaternionF ComputeSquadIntermediate(QuaternionF prev, QuaternionF curr, QuaternionF next)
        {
            QuaternionF currInv = curr.Conjugate;
            QuaternionF logPrev = Log(currInv * prev);
            QuaternionF logNext = Log(currInv * next);

            QuaternionF avg = new QuaternionF(
                -0.25f * (logPrev.X + logNext.X),
                -0.25f * (logPrev.Y + logNext.Y),
                -0.25f * (logPrev.Z + logNext.Z),
                -0.25f * (logPrev.W + logNext.W)
            );

            return curr * Exp(avg);
        }

        /// <summary>
        /// Computes the exponential of a quaternion.
        /// </summary>
        public static QuaternionF Exp(QuaternionF q)
        {
            float vectorMag = MathF.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            float expW = MathF.Exp(q.W);

            if (vectorMag < 1e-6f)
                return new QuaternionF(0, 0, 0, expW);

            float coeff = expW * MathF.Sin(vectorMag) / vectorMag;
            return new QuaternionF(
                q.X * coeff,
                q.Y * coeff,
                q.Z * coeff,
                expW * MathF.Cos(vectorMag)
            );
        }

        /// <summary>
        /// Computes the natural logarithm of a quaternion.
        /// </summary>
        public static QuaternionF Log(QuaternionF q)
        {
            float mag = q.Magnitude;
            float vectorMag = MathF.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);

            if (vectorMag < 1e-6f)
                return new QuaternionF(0, 0, 0, MathF.Log(mag));

            float coeff = MathF.Acos(q.W / mag) / vectorMag;
            return new QuaternionF(
                q.X * coeff,
                q.Y * coeff,
                q.Z * coeff,
                MathF.Log(mag)
            );
        }

        /// <summary>
        /// Rotates this quaternion towards a target quaternion by a maximum angular step.
        /// </summary>
        /// <param name="target">The target rotation.</param>
        /// <param name="maxRadians">Maximum rotation step in radians.</param>
        public QuaternionF RotateTowards(QuaternionF target, float maxRadians)
        {
            float angle = AngleBetween(this, target);
            if (angle < 1e-6f) return target;
            float t = MathF.Min(1f, maxRadians / angle);
            return Slerp(this, target, t);
        }

        // =====================================================================
        // Conversion
        // =====================================================================

        /// <summary>
        /// Converts this quaternion to Euler angles (pitch, yaw, roll) in radians.
        /// Returns values in the range: pitch [-π/2, π/2], yaw [-π, π], roll [-π, π].
        /// </summary>
        public Vector3F ToEulerAngles()
        {
            // Pitch (X-axis rotation)
            float sinP = 2f * (W * X - Y * Z);
            float pitch;
            if (MathF.Abs(sinP) >= 1f)
                pitch = MathF.CopySign(MathF.PI / 2f, sinP); // Gimbal lock
            else
                pitch = MathF.Asin(sinP);

            // Yaw (Y-axis rotation)
            float sinYCosP = 2f * (W * Y + X * Z);
            float cosYCosP = 1f - 2f * (X * X + Y * Y);
            float yaw = MathF.Atan2(sinYCosP, cosYCosP);

            // Roll (Z-axis rotation)
            float sinRCosP = 2f * (W * Z + X * Y);
            float cosRCosP = 1f - 2f * (X * X + Z * Z);
            float roll = MathF.Atan2(sinRCosP, cosRCosP);

            return new Vector3F(pitch, yaw, roll);
        }

        /// <summary>
        /// Converts this quaternion to a 4×4 rotation matrix.
        /// </summary>
        public Matrix4x4F ToMatrix()
        {
            return Matrix4x4F.CreateFromQuaternion(this);
        }

        /// <summary>
        /// Returns the forward direction (negative Z axis) rotated by this quaternion.
        /// </summary>
        public Vector3F GetForward() => Rotate(new Vector3F(0, 0, -1));

        /// <summary>
        /// Returns the up direction (positive Y axis) rotated by this quaternion.
        /// </summary>
        public Vector3F GetUp() => Rotate(new Vector3F(0, 1, 0));

        /// <summary>
        /// Returns the right direction (positive X axis) rotated by this quaternion.
        /// </summary>
        public Vector3F GetRight() => Rotate(new Vector3F(1, 0, 0));

        // =====================================================================
        // Operators
        // =====================================================================

        /// <summary>
        /// Multiplies two quaternions (Hamilton product).
        /// Represents combining two rotations: first b, then a.
        /// </summary>
        public static QuaternionF operator *(QuaternionF a, QuaternionF b)
        {
            return new QuaternionF(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            );
        }

        /// <summary>Scales a quaternion by a scalar (not a rotation operation).</summary>
        public static QuaternionF operator *(QuaternionF q, float s)
        {
            return new QuaternionF(q.X * s, q.Y * s, q.Z * s, q.W * s);
        }

        /// <summary>Scales a quaternion by a scalar.</summary>
        public static QuaternionF operator *(float s, QuaternionF q) => q * s;

        /// <summary>Adds two quaternions component-wise.</summary>
        public static QuaternionF operator +(QuaternionF a, QuaternionF b)
        {
            return new QuaternionF(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        /// <summary>Subtracts quaternion b from a component-wise.</summary>
        public static QuaternionF operator -(QuaternionF a, QuaternionF b)
        {
            return new QuaternionF(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        /// <summary>Negates all components of the quaternion.</summary>
        public static QuaternionF operator -(QuaternionF q)
        {
            return new QuaternionF(-q.X, -q.Y, -q.Z, -q.W);
        }

        // =====================================================================
        // Equality
        // =====================================================================

        public bool Equals(QuaternionF other)
        {
            return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        }

        /// <summary>
        /// Checks if two quaternions represent the same rotation (accounting for double cover).
        /// </summary>
        public bool EqualsRotation(QuaternionF other, float epsilon = 1e-5f)
        {
            float dot = MathF.Abs(Dot(this, other));
            return dot > 1f - epsilon;
        }

        public override bool Equals(object obj) => obj is QuaternionF q && Equals(q);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        public static bool operator ==(QuaternionF a, QuaternionF b) => a.Equals(b);
        public static bool operator !=(QuaternionF a, QuaternionF b) => !a.Equals(b);

        public override string ToString() => $"Quaternion({X:F4}, {Y:F4}, {Z:F4}, {W:F4})";
    }
}
