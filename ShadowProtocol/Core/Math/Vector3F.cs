namespace ShadowProtocol.Core.Math;

public readonly struct Vector3F : IEquatable<Vector3F>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3F(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3F Zero => new(0f, 0f, 0f);
    public static Vector3F One => new(1f, 1f, 1f);
    public static Vector3F Forward => new(0f, 0f, 1f);
    public static Vector3F Back => new(0f, 0f, -1f);
    public static Vector3F Up => new(0f, 1f, 0f);
    public static Vector3F Down => new(0f, -1f, 0f);
    public static Vector3F Right => new(1f, 0f, 0f);
    public static Vector3F Left => new(-1f, 0f, 0f);

    public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);
    public float Magnitude => MathF.Sqrt(SqrMagnitude);

    public Vector3F Normalized
    {
        get
        {
            float mag = Magnitude;
            if (mag > 1e-6f)
            {
                return new Vector3F(X / mag, Y / mag, Z / mag);
            }
            return Zero;
        }
    }

    public static Vector3F operator +(Vector3F a, Vector3F b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3F operator -(Vector3F a, Vector3F b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3F operator -(Vector3F a) => new(-a.X, -a.Y, -a.Z);
    public static Vector3F operator *(Vector3F a, float d) => new(a.X * d, a.Y * d, a.Z * d);
    public static Vector3F operator *(float d, Vector3F a) => new(a.X * d, a.Y * d, a.Z * d);
    public static Vector3F operator /(Vector3F a, float d) => new(a.X / d, a.Y / d, a.Z / d);
    public static bool operator ==(Vector3F a, Vector3F b) => MathF.Abs(a.X - b.X) < 1e-5f && MathF.Abs(a.Y - b.Y) < 1e-5f && MathF.Abs(a.Z - b.Z) < 1e-5f;
    public static bool operator !=(Vector3F a, Vector3F b) => !(a == b);

    public static float Dot(Vector3F a, Vector3F b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
    public static Vector3F Cross(Vector3F a, Vector3F b) => new(
        (a.Y * b.Z) - (a.Z * b.Y),
        (a.Z * b.X) - (a.X * b.Z),
        (a.X * b.Y) - (a.Y * b.X)
    );

    public static float Distance(Vector3F a, Vector3F b) => (a - b).Magnitude;
    public static float DistanceSquared(Vector3F a, Vector3F b) => (a - b).SqrMagnitude;

    public static Vector3F Lerp(Vector3F a, Vector3F b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector3F(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t), a.Z + ((b.Z - a.Z) * t));
    }

    public bool Equals(Vector3F other) => this == other;
    public override bool Equals(object? obj) => obj is Vector3F other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"Vector3F({X:F2}, {Y:F2}, {Z:F2})";
}

public readonly struct QuaternionF : IEquatable<QuaternionF>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }

    public QuaternionF(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static QuaternionF Identity => new(0f, 0f, 0f, 1f);

    public static QuaternionF Euler(float pitch, float yaw, float roll)
    {
        float p = pitch * (MathF.PI / 180f) * 0.5f;
        float y = yaw * (MathF.PI / 180f) * 0.5f;
        float r = roll * (MathF.PI / 180f) * 0.5f;

        float sinP = MathF.Sin(p), cosP = MathF.Cos(p);
        float sinY = MathF.Sin(y), cosY = MathF.Cos(y);
        float sinR = MathF.Sin(r), cosR = MathF.Cos(r);

        return new QuaternionF(
            (sinP * cosY * cosR) - (cosP * sinY * sinR),
            (cosP * sinY * cosR) + (sinP * cosY * sinR),
            (cosP * cosY * sinR) - (sinP * sinY * cosR),
            (cosP * cosY * cosR) + (sinP * sinY * sinR)
        );
    }

    public bool Equals(QuaternionF other) =>
        MathF.Abs(X - other.X) < 1e-5f &&
        MathF.Abs(Y - other.Y) < 1e-5f &&
        MathF.Abs(Z - other.Z) < 1e-5f &&
        MathF.Abs(W - other.W) < 1e-5f;

    public override bool Equals(object? obj) => obj is QuaternionF other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
}

public static class MathUtils
{
    public const float Deg2Rad = MathF.PI / 180f;
    public const float Rad2Deg = 180f / MathF.PI;

    public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

    public static float AngleBetween(Vector3F from, Vector3F to)
    {
        float denominator = MathF.Sqrt(from.SqrMagnitude * to.SqrMagnitude);
        if (denominator < 1e-6f) return 0f;
        float dot = Math.Clamp(Vector3F.Dot(from, to) / denominator, -1f, 1f);
        return MathF.Acos(dot) * Rad2Deg;
    }
}
