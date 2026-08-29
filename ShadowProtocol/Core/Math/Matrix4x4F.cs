// =============================================================================
// ShadowProtocol.Core — Matrix4x4F: Full 4×4 matrix for 3D transformations
// =============================================================================

using System;
using System.Runtime.CompilerServices;

namespace ShadowProtocol.Core.Math
{
    /// <summary>
    /// A 4×4 column-major matrix used for 3D transformations including
    /// translation, rotation, scaling, and projection operations.
    /// Optimized for game engine use with SIMD-friendly memory layout.
    /// </summary>
    public struct Matrix4x4F : IEquatable<Matrix4x4F>
    {
        // =====================================================================
        // Fields — Column-major storage for GPU compatibility
        // =====================================================================

        public float M00, M01, M02, M03;
        public float M10, M11, M12, M13;
        public float M20, M21, M22, M23;
        public float M30, M31, M32, M33;

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        /// Constructs a matrix from all 16 individual elements (row-major order for readability).
        /// </summary>
        public Matrix4x4F(
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            M00 = m00; M01 = m01; M02 = m02; M03 = m03;
            M10 = m10; M11 = m11; M12 = m12; M13 = m13;
            M20 = m20; M21 = m21; M22 = m22; M23 = m23;
            M30 = m30; M31 = m31; M32 = m32; M33 = m33;
        }

        /// <summary>
        /// Constructs a matrix from four column vectors.
        /// </summary>
        public Matrix4x4F(Vector3F col0, Vector3F col1, Vector3F col2, Vector3F col3)
        {
            M00 = col0.X; M10 = col0.Y; M20 = col0.Z; M30 = 0f;
            M01 = col1.X; M11 = col1.Y; M21 = col1.Z; M31 = 0f;
            M02 = col2.X; M12 = col2.Y; M22 = col2.Z; M32 = 0f;
            M03 = col3.X; M13 = col3.Y; M23 = col3.Z; M33 = 1f;
        }

        // =====================================================================
        // Static Predefined Matrices
        // =====================================================================

        /// <summary>The 4×4 identity matrix.</summary>
        public static readonly Matrix4x4F Identity = new Matrix4x4F(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        /// <summary>A zero matrix (all elements are 0).</summary>
        public static readonly Matrix4x4F Zero = new Matrix4x4F(
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        );

        // =====================================================================
        // Indexer
        // =====================================================================

        /// <summary>
        /// Gets or sets a matrix element by row and column index.
        /// </summary>
        public float this[int row, int col]
        {
            get
            {
                return (row, col) switch
                {
                    (0, 0) => M00, (0, 1) => M01, (0, 2) => M02, (0, 3) => M03,
                    (1, 0) => M10, (1, 1) => M11, (1, 2) => M12, (1, 3) => M13,
                    (2, 0) => M20, (2, 1) => M21, (2, 2) => M22, (2, 3) => M23,
                    (3, 0) => M30, (3, 1) => M31, (3, 2) => M32, (3, 3) => M33,
                    _ => throw new IndexOutOfRangeException($"Matrix index [{row},{col}] out of range")
                };
            }
            set
            {
                switch (row, col)
                {
                    case (0, 0): M00 = value; break; case (0, 1): M01 = value; break;
                    case (0, 2): M02 = value; break; case (0, 3): M03 = value; break;
                    case (1, 0): M10 = value; break; case (1, 1): M11 = value; break;
                    case (1, 2): M12 = value; break; case (1, 3): M13 = value; break;
                    case (2, 0): M20 = value; break; case (2, 1): M21 = value; break;
                    case (2, 2): M22 = value; break; case (2, 3): M23 = value; break;
                    case (3, 0): M30 = value; break; case (3, 1): M31 = value; break;
                    case (3, 2): M32 = value; break; case (3, 3): M33 = value; break;
                    default: throw new IndexOutOfRangeException($"Matrix index [{row},{col}] out of range");
                }
            }
        }

        // =====================================================================
        // Translation Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a translation matrix from the given position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4F CreateTranslation(float x, float y, float z)
        {
            return new Matrix4x4F(
                1, 0, 0, x,
                0, 1, 0, y,
                0, 0, 1, z,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a translation matrix from a vector position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4F CreateTranslation(Vector3F position)
        {
            return CreateTranslation(position.X, position.Y, position.Z);
        }

        // =====================================================================
        // Scaling Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a scaling matrix with the given scale factors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4F CreateScale(float sx, float sy, float sz)
        {
            return new Matrix4x4F(
                sx, 0, 0, 0,
                0, sy, 0, 0,
                0, 0, sz, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a uniform scaling matrix.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4F CreateScale(float uniformScale)
        {
            return CreateScale(uniformScale, uniformScale, uniformScale);
        }

        /// <summary>
        /// Creates a scaling matrix from a vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4F CreateScale(Vector3F scale)
        {
            return CreateScale(scale.X, scale.Y, scale.Z);
        }

        // =====================================================================
        // Rotation Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a rotation matrix around the X axis (pitch).
        /// </summary>
        public static Matrix4x4F CreateRotationX(float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            return new Matrix4x4F(
                1, 0, 0, 0,
                0, cos, -sin, 0,
                0, sin, cos, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around the Y axis (yaw).
        /// </summary>
        public static Matrix4x4F CreateRotationY(float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            return new Matrix4x4F(
                cos, 0, sin, 0,
                0, 1, 0, 0,
                -sin, 0, cos, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around the Z axis (roll).
        /// </summary>
        public static Matrix4x4F CreateRotationZ(float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            return new Matrix4x4F(
                cos, -sin, 0, 0,
                sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix around an arbitrary axis using Rodrigues' rotation formula.
        /// </summary>
        public static Matrix4x4F CreateFromAxisAngle(Vector3F axis, float radians)
        {
            float length = MathF.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z);
            if (length < 1e-6f) return Identity;

            float invLen = 1f / length;
            float x = axis.X * invLen, y = axis.Y * invLen, z = axis.Z * invLen;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            float oneMinusCos = 1f - cos;

            return new Matrix4x4F(
                cos + x * x * oneMinusCos,       x * y * oneMinusCos - z * sin,   x * z * oneMinusCos + y * sin,   0,
                y * x * oneMinusCos + z * sin,   cos + y * y * oneMinusCos,       y * z * oneMinusCos - x * sin,   0,
                z * x * oneMinusCos - y * sin,   z * y * oneMinusCos + x * sin,   cos + z * z * oneMinusCos,       0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix from Euler angles (yaw, pitch, roll) in radians.
        /// Applied in order: Yaw (Y) → Pitch (X) → Roll (Z).
        /// </summary>
        public static Matrix4x4F CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            return CreateRotationY(yaw) * CreateRotationX(pitch) * CreateRotationZ(roll);
        }

        /// <summary>
        /// Creates a rotation matrix from a quaternion.
        /// </summary>
        public static Matrix4x4F CreateFromQuaternion(QuaternionF q)
        {
            float xx = q.X * q.X, yy = q.Y * q.Y, zz = q.Z * q.Z;
            float xy = q.X * q.Y, xz = q.X * q.Z, yz = q.Y * q.Z;
            float wx = q.W * q.X, wy = q.W * q.Y, wz = q.W * q.Z;

            return new Matrix4x4F(
                1 - 2 * (yy + zz),     2 * (xy - wz),       2 * (xz + wy),       0,
                2 * (xy + wz),         1 - 2 * (xx + zz),   2 * (yz - wx),       0,
                2 * (xz - wy),         2 * (yz + wx),       1 - 2 * (xx + yy),   0,
                0, 0, 0, 1
            );
        }

        // =====================================================================
        // Projection Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a perspective projection matrix using field of view.
        /// </summary>
        /// <param name="fovYRadians">Vertical field of view in radians.</param>
        /// <param name="aspectRatio">Width divided by height of the viewport.</param>
        /// <param name="nearPlane">Distance to the near clipping plane.</param>
        /// <param name="farPlane">Distance to the far clipping plane.</param>
        public static Matrix4x4F CreatePerspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
        {
            if (fovYRadians <= 0 || fovYRadians >= MathF.PI)
                throw new ArgumentOutOfRangeException(nameof(fovYRadians), "FOV must be between 0 and PI");
            if (aspectRatio <= 0)
                throw new ArgumentOutOfRangeException(nameof(aspectRatio), "Aspect ratio must be positive");
            if (nearPlane <= 0)
                throw new ArgumentOutOfRangeException(nameof(nearPlane), "Near plane must be positive");
            if (farPlane <= nearPlane)
                throw new ArgumentOutOfRangeException(nameof(farPlane), "Far plane must be greater than near plane");

            float yScale = 1f / MathF.Tan(fovYRadians * 0.5f);
            float xScale = yScale / aspectRatio;
            float range = farPlane / (nearPlane - farPlane);

            return new Matrix4x4F(
                xScale, 0, 0, 0,
                0, yScale, 0, 0,
                0, 0, range, range * nearPlane,
                0, 0, -1, 0
            );
        }

        /// <summary>
        /// Creates an orthographic projection matrix.
        /// </summary>
        public static Matrix4x4F CreateOrthographic(float width, float height, float nearPlane, float farPlane)
        {
            float range = 1f / (nearPlane - farPlane);
            return new Matrix4x4F(
                2f / width, 0, 0, 0,
                0, 2f / height, 0, 0,
                0, 0, range, range * nearPlane,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Creates an orthographic projection matrix with explicit bounds.
        /// </summary>
        public static Matrix4x4F CreateOrthographicOffCenter(float left, float right, float bottom, float top, float near, float far)
        {
            float invWidth = 1f / (right - left);
            float invHeight = 1f / (top - bottom);
            float invDepth = 1f / (near - far);

            return new Matrix4x4F(
                2f * invWidth, 0, 0, -(right + left) * invWidth,
                0, 2f * invHeight, 0, -(top + bottom) * invHeight,
                0, 0, invDepth, near * invDepth,
                0, 0, 0, 1
            );
        }

        // =====================================================================
        // View Matrix Factory Methods
        // =====================================================================

        /// <summary>
        /// Creates a look-at view matrix (right-handed coordinate system).
        /// </summary>
        /// <param name="eye">The camera position.</param>
        /// <param name="target">The point the camera is looking at.</param>
        /// <param name="up">The world up direction.</param>
        public static Matrix4x4F CreateLookAt(Vector3F eye, Vector3F target, Vector3F up)
        {
            Vector3F zAxis = (eye - target).Normalized();
            Vector3F xAxis = Vector3F.Cross(up, zAxis).Normalized();
            Vector3F yAxis = Vector3F.Cross(zAxis, xAxis);

            return new Matrix4x4F(
                xAxis.X, xAxis.Y, xAxis.Z, -Vector3F.Dot(xAxis, eye),
                yAxis.X, yAxis.Y, yAxis.Z, -Vector3F.Dot(yAxis, eye),
                zAxis.X, zAxis.Y, zAxis.Z, -Vector3F.Dot(zAxis, eye),
                0, 0, 0, 1
            );
        }

        // =====================================================================
        // Arithmetic Operators
        // =====================================================================

        /// <summary>Multiplies two matrices together using standard matrix multiplication.</summary>
        public static Matrix4x4F operator *(Matrix4x4F a, Matrix4x4F b)
        {
            return new Matrix4x4F(
                a.M00*b.M00 + a.M01*b.M10 + a.M02*b.M20 + a.M03*b.M30,
                a.M00*b.M01 + a.M01*b.M11 + a.M02*b.M21 + a.M03*b.M31,
                a.M00*b.M02 + a.M01*b.M12 + a.M02*b.M22 + a.M03*b.M32,
                a.M00*b.M03 + a.M01*b.M13 + a.M02*b.M23 + a.M03*b.M33,

                a.M10*b.M00 + a.M11*b.M10 + a.M12*b.M20 + a.M13*b.M30,
                a.M10*b.M01 + a.M11*b.M11 + a.M12*b.M21 + a.M13*b.M31,
                a.M10*b.M02 + a.M11*b.M12 + a.M12*b.M22 + a.M13*b.M32,
                a.M10*b.M03 + a.M11*b.M13 + a.M12*b.M23 + a.M13*b.M33,

                a.M20*b.M00 + a.M21*b.M10 + a.M22*b.M20 + a.M23*b.M30,
                a.M20*b.M01 + a.M21*b.M11 + a.M22*b.M21 + a.M23*b.M31,
                a.M20*b.M02 + a.M21*b.M12 + a.M22*b.M22 + a.M23*b.M32,
                a.M20*b.M03 + a.M21*b.M13 + a.M22*b.M23 + a.M23*b.M33,

                a.M30*b.M00 + a.M31*b.M10 + a.M32*b.M20 + a.M33*b.M30,
                a.M30*b.M01 + a.M31*b.M11 + a.M32*b.M21 + a.M33*b.M31,
                a.M30*b.M02 + a.M31*b.M12 + a.M32*b.M22 + a.M33*b.M32,
                a.M30*b.M03 + a.M31*b.M13 + a.M32*b.M23 + a.M33*b.M33
            );
        }

        /// <summary>Adds two matrices element-wise.</summary>
        public static Matrix4x4F operator +(Matrix4x4F a, Matrix4x4F b)
        {
            return new Matrix4x4F(
                a.M00+b.M00, a.M01+b.M01, a.M02+b.M02, a.M03+b.M03,
                a.M10+b.M10, a.M11+b.M11, a.M12+b.M12, a.M13+b.M13,
                a.M20+b.M20, a.M21+b.M21, a.M22+b.M22, a.M23+b.M23,
                a.M30+b.M30, a.M31+b.M31, a.M32+b.M32, a.M33+b.M33
            );
        }

        /// <summary>Subtracts matrix b from matrix a element-wise.</summary>
        public static Matrix4x4F operator -(Matrix4x4F a, Matrix4x4F b)
        {
            return new Matrix4x4F(
                a.M00-b.M00, a.M01-b.M01, a.M02-b.M02, a.M03-b.M03,
                a.M10-b.M10, a.M11-b.M11, a.M12-b.M12, a.M13-b.M13,
                a.M20-b.M20, a.M21-b.M21, a.M22-b.M22, a.M23-b.M23,
                a.M30-b.M30, a.M31-b.M31, a.M32-b.M32, a.M33-b.M33
            );
        }

        /// <summary>Multiplies a matrix by a scalar.</summary>
        public static Matrix4x4F operator *(Matrix4x4F m, float s)
        {
            return new Matrix4x4F(
                m.M00*s, m.M01*s, m.M02*s, m.M03*s,
                m.M10*s, m.M11*s, m.M12*s, m.M13*s,
                m.M20*s, m.M21*s, m.M22*s, m.M23*s,
                m.M30*s, m.M31*s, m.M32*s, m.M33*s
            );
        }

        /// <summary>Multiplies a scalar by a matrix.</summary>
        public static Matrix4x4F operator *(float s, Matrix4x4F m) => m * s;

        /// <summary>Negates all elements of the matrix.</summary>
        public static Matrix4x4F operator -(Matrix4x4F m)
        {
            return new Matrix4x4F(
                -m.M00, -m.M01, -m.M02, -m.M03,
                -m.M10, -m.M11, -m.M12, -m.M13,
                -m.M20, -m.M21, -m.M22, -m.M23,
                -m.M30, -m.M31, -m.M32, -m.M33
            );
        }

        // =====================================================================
        // Vector Transformation
        // =====================================================================

        /// <summary>
        /// Transforms a 3D point by this matrix (applies translation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3F TransformPoint(Vector3F point)
        {
            return new Vector3F(
                M00 * point.X + M01 * point.Y + M02 * point.Z + M03,
                M10 * point.X + M11 * point.Y + M12 * point.Z + M13,
                M20 * point.X + M21 * point.Y + M22 * point.Z + M23
            );
        }

        /// <summary>
        /// Transforms a 3D direction by this matrix (ignores translation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3F TransformDirection(Vector3F direction)
        {
            return new Vector3F(
                M00 * direction.X + M01 * direction.Y + M02 * direction.Z,
                M10 * direction.X + M11 * direction.Y + M12 * direction.Z,
                M20 * direction.X + M21 * direction.Y + M22 * direction.Z
            );
        }

        /// <summary>
        /// Transforms a 3D normal using the inverse transpose of the upper-left 3×3.
        /// </summary>
        public Vector3F TransformNormal(Vector3F normal)
        {
            Matrix4x4F invT = Inverse().Transposed();
            return invT.TransformDirection(normal).Normalized();
        }

        // =====================================================================
        // Determinant
        // =====================================================================

        /// <summary>
        /// Computes the determinant of this 4×4 matrix using cofactor expansion.
        /// </summary>
        public float Determinant()
        {
            float a = M00, b = M01, c = M02, d = M03;
            float e = M10, f = M11, g = M12, h = M13;
            float i = M20, j = M21, k = M22, l = M23;
            float m = M30, n = M31, o = M32, p = M33;

            float kp_lo = k * p - l * o;
            float jp_ln = j * p - l * n;
            float jo_kn = j * o - k * n;
            float ip_lm = i * p - l * m;
            float io_km = i * o - k * m;
            float in_jm = i * n - j * m;

            return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
                   b * (e * kp_lo - g * ip_lm + h * io_km) +
                   c * (e * jp_ln - f * ip_lm + h * in_jm) -
                   d * (e * jo_kn - f * io_km + g * in_jm);
        }

        // =====================================================================
        // Transpose
        // =====================================================================

        /// <summary>
        /// Returns the transpose of this matrix.
        /// </summary>
        public Matrix4x4F Transposed()
        {
            return new Matrix4x4F(
                M00, M10, M20, M30,
                M01, M11, M21, M31,
                M02, M12, M22, M32,
                M03, M13, M23, M33
            );
        }

        // =====================================================================
        // Inverse
        // =====================================================================

        /// <summary>
        /// Computes the inverse of this matrix. Throws if the matrix is singular.
        /// Uses the adjugate method with cofactor expansion.
        /// </summary>
        public Matrix4x4F Inverse()
        {
            float a = M00, b = M01, c = M02, d = M03;
            float e = M10, f = M11, g = M12, h = M13;
            float i = M20, j = M21, k = M22, l = M23;
            float m = M30, n = M31, o = M32, p = M33;

            float kp_lo = k * p - l * o;
            float jp_ln = j * p - l * n;
            float jo_kn = j * o - k * n;
            float ip_lm = i * p - l * m;
            float io_km = i * o - k * m;
            float in_jm = i * n - j * m;

            float a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
            float a12 = -(e * kp_lo - g * ip_lm + h * io_km);
            float a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
            float a14 = -(e * jo_kn - f * io_km + g * in_jm);

            float det = a * a11 + b * a12 + c * a13 + d * a14;
            if (MathF.Abs(det) < 1e-10f)
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

            float invDet = 1f / det;

            float gp_ho = g * p - h * o;
            float fp_hn = f * p - h * n;
            float fo_gn = f * o - g * n;
            float ep_hm = e * p - h * m;
            float eo_gm = e * o - g * m;
            float en_fm = e * n - f * m;

            float gl_hk = g * l - h * k;
            float fl_hj = f * l - h * j;
            float fk_gj = f * k - g * j;
            float el_hi = e * l - h * i;
            float ek_gi = e * k - g * i;
            float ej_fi = e * j - f * i;

            return new Matrix4x4F(
                a11 * invDet,
                -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet,
                +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet,
                -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet,

                a12 * invDet,
                +(a * kp_lo - c * ip_lm + d * io_km) * invDet,
                -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet,
                +(a * gl_hk - c * el_hi + d * ek_gi) * invDet,

                a13 * invDet,
                -(a * jp_ln - b * ip_lm + d * in_jm) * invDet,
                +(a * fp_hn - b * ep_hm + d * en_fm) * invDet,
                -(a * fl_hj - b * el_hi + d * ej_fi) * invDet,

                a14 * invDet,
                +(a * jo_kn - b * io_km + c * in_jm) * invDet,
                -(a * fo_gn - b * eo_gm + c * en_fm) * invDet,
                +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet
            );
        }

        /// <summary>
        /// Attempts to compute the inverse. Returns false if the matrix is singular.
        /// </summary>
        public bool TryInverse(out Matrix4x4F result)
        {
            try
            {
                result = Inverse();
                return true;
            }
            catch
            {
                result = Identity;
                return false;
            }
        }

        // =====================================================================
        // Decomposition
        // =====================================================================

        /// <summary>
        /// Decomposes this matrix into translation, rotation (quaternion), and scale components.
        /// Assumes the matrix is an affine transformation (no shear or projection).
        /// </summary>
        /// <returns>True if decomposition was successful.</returns>
        public bool Decompose(out Vector3F translation, out QuaternionF rotation, out Vector3F scale)
        {
            translation = new Vector3F(M03, M13, M23);

            Vector3F col0 = new Vector3F(M00, M10, M20);
            Vector3F col1 = new Vector3F(M01, M11, M21);
            Vector3F col2 = new Vector3F(M02, M12, M22);

            scale = new Vector3F(col0.Magnitude(), col1.Magnitude(), col2.Magnitude());

            if (scale.X < 1e-6f || scale.Y < 1e-6f || scale.Z < 1e-6f)
            {
                rotation = QuaternionF.Identity;
                return false;
            }

            // Check for reflection (negative determinant of the 3x3 rotation part)
            float det3x3 = col0.X * (col1.Y * col2.Z - col1.Z * col2.Y)
                         - col0.Y * (col1.X * col2.Z - col1.Z * col2.X)
                         + col0.Z * (col1.X * col2.Y - col1.Y * col2.X);

            if (det3x3 < 0)
            {
                scale = new Vector3F(-scale.X, scale.Y, scale.Z);
                col0 = new Vector3F(-col0.X, -col0.Y, -col0.Z);
            }

            // Normalize columns to get rotation matrix
            Matrix4x4F rotMat = new Matrix4x4F(
                col0.X / scale.X, col1.X / scale.Y, col2.X / scale.Z, 0,
                col0.Y / scale.X, col1.Y / scale.Y, col2.Y / scale.Z, 0,
                col0.Z / scale.X, col1.Z / scale.Y, col2.Z / scale.Z, 0,
                0, 0, 0, 1
            );

            rotation = QuaternionF.FromRotationMatrix(rotMat);
            return true;
        }

        // =====================================================================
        // Utility Methods
        // =====================================================================

        /// <summary>
        /// Extracts the translation component from this matrix.
        /// </summary>
        public Vector3F GetTranslation() => new Vector3F(M03, M13, M23);

        /// <summary>
        /// Sets the translation component of this matrix.
        /// </summary>
        public void SetTranslation(Vector3F t) { M03 = t.X; M13 = t.Y; M23 = t.Z; }

        /// <summary>
        /// Extracts the scale factors from the matrix columns.
        /// </summary>
        public Vector3F GetScale()
        {
            float sx = MathF.Sqrt(M00 * M00 + M10 * M10 + M20 * M20);
            float sy = MathF.Sqrt(M01 * M01 + M11 * M11 + M21 * M21);
            float sz = MathF.Sqrt(M02 * M02 + M12 * M12 + M22 * M22);
            return new Vector3F(sx, sy, sz);
        }

        /// <summary>
        /// Linearly interpolates each element between two matrices.
        /// </summary>
        public static Matrix4x4F Lerp(Matrix4x4F a, Matrix4x4F b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Checks if this matrix is approximately equal to the identity matrix.
        /// </summary>
        public bool IsIdentity(float epsilon = 1e-5f)
        {
            return MathF.Abs(M00 - 1) < epsilon && MathF.Abs(M01) < epsilon && MathF.Abs(M02) < epsilon && MathF.Abs(M03) < epsilon
                && MathF.Abs(M10) < epsilon && MathF.Abs(M11 - 1) < epsilon && MathF.Abs(M12) < epsilon && MathF.Abs(M13) < epsilon
                && MathF.Abs(M20) < epsilon && MathF.Abs(M21) < epsilon && MathF.Abs(M22 - 1) < epsilon && MathF.Abs(M23) < epsilon
                && MathF.Abs(M30) < epsilon && MathF.Abs(M31) < epsilon && MathF.Abs(M32) < epsilon && MathF.Abs(M33 - 1) < epsilon;
        }

        /// <summary>
        /// Creates a TRS (Translation-Rotation-Scale) matrix.
        /// </summary>
        public static Matrix4x4F TRS(Vector3F translation, QuaternionF rotation, Vector3F scale)
        {
            return CreateTranslation(translation) * CreateFromQuaternion(rotation) * CreateScale(scale);
        }

        /// <summary>
        /// Converts the matrix to a float array in column-major order (for GPU upload).
        /// </summary>
        public float[] ToColumnMajorArray()
        {
            return new float[]
            {
                M00, M10, M20, M30,
                M01, M11, M21, M31,
                M02, M12, M22, M32,
                M03, M13, M23, M33
            };
        }

        /// <summary>
        /// Converts the matrix to a float array in row-major order.
        /// </summary>
        public float[] ToRowMajorArray()
        {
            return new float[]
            {
                M00, M01, M02, M03,
                M10, M11, M12, M13,
                M20, M21, M22, M23,
                M30, M31, M32, M33
            };
        }

        // =====================================================================
        // Equality
        // =====================================================================

        public bool Equals(Matrix4x4F other)
        {
            return M00 == other.M00 && M01 == other.M01 && M02 == other.M02 && M03 == other.M03
                && M10 == other.M10 && M11 == other.M11 && M12 == other.M12 && M13 == other.M13
                && M20 == other.M20 && M21 == other.M21 && M22 == other.M22 && M23 == other.M23
                && M30 == other.M30 && M31 == other.M31 && M32 == other.M32 && M33 == other.M33;
        }

        public override bool Equals(object obj) => obj is Matrix4x4F m && Equals(m);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(M00); h.Add(M01); h.Add(M02); h.Add(M03);
            h.Add(M10); h.Add(M11); h.Add(M12); h.Add(M13);
            h.Add(M20); h.Add(M21); h.Add(M22); h.Add(M23);
            h.Add(M30); h.Add(M31); h.Add(M32); h.Add(M33);
            return h.ToHashCode();
        }

        public static bool operator ==(Matrix4x4F left, Matrix4x4F right) => left.Equals(right);
        public static bool operator !=(Matrix4x4F left, Matrix4x4F right) => !left.Equals(right);

        public override string ToString()
        {
            return $"[{M00:F3} {M01:F3} {M02:F3} {M03:F3}]\n" +
                   $"[{M10:F3} {M11:F3} {M12:F3} {M13:F3}]\n" +
                   $"[{M20:F3} {M21:F3} {M22:F3} {M23:F3}]\n" +
                   $"[{M30:F3} {M31:F3} {M32:F3} {M33:F3}]";
        }
    }
}
