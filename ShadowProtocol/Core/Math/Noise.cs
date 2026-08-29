// =============================================================================
// ShadowProtocol.Core — Noise: Perlin, Simplex, and procedural noise generators
// =============================================================================

using System;
using System.Runtime.CompilerServices;

namespace ShadowProtocol.Core.Math
{
    /// <summary>
    /// Provides various noise generation algorithms for procedural content generation
    /// including terrain, textures, clouds, and other natural-looking patterns.
    /// </summary>
    public static class Noise
    {
        // =====================================================================
        // Permutation Table (Ken Perlin's standard permutation)
        // =====================================================================

        private static readonly int[] _perm = new int[512];
        private static readonly int[] _basePermutation = {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,
            140,36,103,30,69,142,8,99,37,240,21,10,23,190,6,148,
            247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,
            57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,
            74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,
            60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
            65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,
            200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,
            52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,
            207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,
            119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,
            129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,
            218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,
            81,51,145,235,249,14,239,107,49,192,214,31,181,199,106,157,
            184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,
            222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        static Noise()
        {
            for (int i = 0; i < 256; i++)
            {
                _perm[i] = _basePermutation[i];
                _perm[i + 256] = _basePermutation[i];
            }
        }

        // =====================================================================
        // Gradient Vectors
        // =====================================================================

        private static readonly float[][] _grad3 = {
            new[]{1f,1f,0f}, new[]{-1f,1f,0f}, new[]{1f,-1f,0f}, new[]{-1f,-1f,0f},
            new[]{1f,0f,1f}, new[]{-1f,0f,1f}, new[]{1f,0f,-1f}, new[]{-1f,0f,-1f},
            new[]{0f,1f,1f}, new[]{0f,-1f,1f}, new[]{0f,1f,-1f}, new[]{0f,-1f,-1f}
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot3(float[] g, float x, float y, float z)
        {
            return g[0] * x + g[1] * y + g[2] * z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot2(float[] g, float x, float y)
        {
            return g[0] * x + g[1] * y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fade(float t)
        {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        // =====================================================================
        // Perlin Noise 2D
        // =====================================================================

        /// <summary>
        /// Generates 2D Perlin noise at the given coordinates.
        /// Returns a value in the range approximately [-1, 1].
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public static float Perlin2D(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;

            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = _perm[_perm[xi] + yi];
            int ab = _perm[_perm[xi] + yi + 1];
            int ba = _perm[_perm[xi + 1] + yi];
            int bb = _perm[_perm[xi + 1] + yi + 1];

            float x1 = Lerp(Dot2(_grad3[aa % 12], xf, yf),
                           Dot2(_grad3[ba % 12], xf - 1f, yf), u);
            float x2 = Lerp(Dot2(_grad3[ab % 12], xf, yf - 1f),
                           Dot2(_grad3[bb % 12], xf - 1f, yf - 1f), u);

            return Lerp(x1, x2, v);
        }

        // =====================================================================
        // Perlin Noise 3D
        // =====================================================================

        /// <summary>
        /// Generates 3D Perlin noise at the given coordinates.
        /// Returns a value in the range approximately [-1, 1].
        /// </summary>
        public static float Perlin3D(float x, float y, float z)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            int zi = (int)MathF.Floor(z) & 255;

            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);
            float zf = z - MathF.Floor(z);

            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            int aaa = _perm[_perm[_perm[xi] + yi] + zi];
            int aba = _perm[_perm[_perm[xi] + yi + 1] + zi];
            int aab = _perm[_perm[_perm[xi] + yi] + zi + 1];
            int abb = _perm[_perm[_perm[xi] + yi + 1] + zi + 1];
            int baa = _perm[_perm[_perm[xi + 1] + yi] + zi];
            int bba = _perm[_perm[_perm[xi + 1] + yi + 1] + zi];
            int bab = _perm[_perm[_perm[xi + 1] + yi] + zi + 1];
            int bbb = _perm[_perm[_perm[xi + 1] + yi + 1] + zi + 1];

            float x1 = Lerp(Dot3(_grad3[aaa % 12], xf, yf, zf),
                           Dot3(_grad3[baa % 12], xf - 1, yf, zf), u);
            float x2 = Lerp(Dot3(_grad3[aba % 12], xf, yf - 1, zf),
                           Dot3(_grad3[bba % 12], xf - 1, yf - 1, zf), u);
            float y1 = Lerp(x1, x2, v);

            x1 = Lerp(Dot3(_grad3[aab % 12], xf, yf, zf - 1),
                      Dot3(_grad3[bab % 12], xf - 1, yf, zf - 1), u);
            x2 = Lerp(Dot3(_grad3[abb % 12], xf, yf - 1, zf - 1),
                      Dot3(_grad3[bbb % 12], xf - 1, yf - 1, zf - 1), u);
            float y2 = Lerp(x1, x2, v);

            return Lerp(y1, y2, w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        // =====================================================================
        // Fractal Brownian Motion (FBM)
        // =====================================================================

        /// <summary>
        /// Generates fractal Brownian motion noise by layering multiple octaves of Perlin noise.
        /// Used for terrain heightmaps, clouds, and other natural-looking patterns.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="octaves">Number of noise layers to combine (more = more detail).</param>
        /// <param name="lacunarity">Frequency multiplier between octaves (typically 2.0).</param>
        /// <param name="persistence">Amplitude multiplier between octaves (typically 0.5).</param>
        /// <param name="scale">Base frequency scale.</param>
        public static float FBM2D(float x, float y, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += Perlin2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value / maxValue;
        }

        /// <summary>
        /// Generates 3D fractal Brownian motion noise.
        /// </summary>
        public static float FBM3D(float x, float y, float z, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += Perlin3D(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value / maxValue;
        }

        // =====================================================================
        // Turbulence
        // =====================================================================

        /// <summary>
        /// Generates turbulence noise (absolute value FBM).
        /// Creates sharp ridges and veins, useful for marble and cloud textures.
        /// </summary>
        public static float Turbulence2D(float x, float y, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += MathF.Abs(Perlin2D(x * frequency, y * frequency)) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value / maxValue;
        }

        // =====================================================================
        // Ridge Noise
        // =====================================================================

        /// <summary>
        /// Generates ridged multi-fractal noise.
        /// Creates mountain-ridge-like patterns with sharp peaks.
        /// </summary>
        public static float Ridge2D(float x, float y, int octaves = 6,
            float lacunarity = 2f, float gain = 2f, float offset = 1f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = scale;
            float weight = 1f;
            float previous = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float signal = offset - MathF.Abs(Perlin2D(x * frequency, y * frequency));
                signal *= signal;
                signal *= weight;
                weight = MathUtils.Clamp01(signal * gain);

                value += signal * amplitude;
                amplitude *= 0.5f;
                frequency *= lacunarity;
            }

            return value;
        }

        // =====================================================================
        // Domain Warping
        // =====================================================================

        /// <summary>
        /// Applies domain warping to noise, creating fluid-like distortions.
        /// The result is visually similar to natural fluid dynamics.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="warpStrength">How strongly to warp the domain.</param>
        /// <param name="octaves">Number of FBM octaves.</param>
        public static float WarpedFBM2D(float x, float y, float warpStrength = 4f, int octaves = 6)
        {
            float qx = FBM2D(x, y, octaves);
            float qy = FBM2D(x + 5.2f, y + 1.3f, octaves);

            float rx = FBM2D(x + warpStrength * qx + 1.7f, y + warpStrength * qy + 9.2f, octaves);
            float ry = FBM2D(x + warpStrength * qx + 8.3f, y + warpStrength * qy + 2.8f, octaves);

            return FBM2D(x + warpStrength * rx, y + warpStrength * ry, octaves);
        }

        // =====================================================================
        // Simplex Noise 2D
        // =====================================================================

        private const float F2 = 0.3660254037844386f;  // (sqrt(3)-1)/2
        private const float G2 = 0.21132486540518713f; // (3-sqrt(3))/6

        /// <summary>
        /// Generates 2D Simplex noise at the given coordinates.
        /// Simplex noise is faster than Perlin noise in higher dimensions
        /// and produces fewer directional artifacts.
        /// Returns a value in the range approximately [-1, 1].
        /// </summary>
        public static float Simplex2D(float x, float y)
        {
            float s = (x + y) * F2;
            int i = (int)MathF.Floor(x + s);
            int j = (int)MathF.Floor(y + s);

            float t = (i + j) * G2;
            float x0 = x - (i - t);
            float y0 = y - (j - t);

            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;

            float n0 = 0f, n1 = 0f, n2 = 0f;

            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 >= 0)
            {
                int gi0 = _perm[ii + _perm[jj]] % 12;
                t0 *= t0;
                n0 = t0 * t0 * Dot2(_grad3[gi0], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 >= 0)
            {
                int gi1 = _perm[ii + i1 + _perm[jj + j1]] % 12;
                t1 *= t1;
                n1 = t1 * t1 * Dot2(_grad3[gi1], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 >= 0)
            {
                int gi2 = _perm[ii + 1 + _perm[jj + 1]] % 12;
                t2 *= t2;
                n2 = t2 * t2 * Dot2(_grad3[gi2], x2, y2);
            }

            return 70f * (n0 + n1 + n2);
        }

        // =====================================================================
        // Simplex Noise 3D
        // =====================================================================

        private const float F3 = 1f / 3f;
        private const float G3 = 1f / 6f;

        /// <summary>
        /// Generates 3D Simplex noise at the given coordinates.
        /// Returns a value in the range approximately [-1, 1].
        /// </summary>
        public static float Simplex3D(float x, float y, float z)
        {
            float s = (x + y + z) * F3;
            int i = (int)MathF.Floor(x + s);
            int j = (int)MathF.Floor(y + s);
            int k = (int)MathF.Floor(z + s);

            float t = (i + j + k) * G3;
            float x0 = x - (i - t);
            float y0 = y - (j - t);
            float z0 = z - (k - t);

            int i1, j1, k1, i2, j2, k2;

            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }

            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2f * G3;
            float y2 = y0 - j2 + 2f * G3;
            float z2 = z0 - k2 + 2f * G3;
            float x3 = x0 - 1f + 3f * G3;
            float y3 = y0 - 1f + 3f * G3;
            float z3 = z0 - 1f + 3f * G3;

            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            float n0 = 0, n1 = 0, n2 = 0, n3 = 0;

            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 >= 0)
            {
                int gi0 = _perm[ii + _perm[jj + _perm[kk]]] % 12;
                t0 *= t0;
                n0 = t0 * t0 * Dot3(_grad3[gi0], x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 >= 0)
            {
                int gi1 = _perm[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]] % 12;
                t1 *= t1;
                n1 = t1 * t1 * Dot3(_grad3[gi1], x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 >= 0)
            {
                int gi2 = _perm[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]] % 12;
                t2 *= t2;
                n2 = t2 * t2 * Dot3(_grad3[gi2], x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 >= 0)
            {
                int gi3 = _perm[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]] % 12;
                t3 *= t3;
                n3 = t3 * t3 * Dot3(_grad3[gi3], x3, y3, z3);
            }

            return 32f * (n0 + n1 + n2 + n3);
        }

        // =====================================================================
        // Simplex FBM
        // =====================================================================

        /// <summary>
        /// Fractal Brownian motion using Simplex noise as the base function.
        /// </summary>
        public static float SimplexFBM2D(float x, float y, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += Simplex2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value / maxValue;
        }

        /// <summary>
        /// Fractal Brownian motion using 3D Simplex noise.
        /// </summary>
        public static float SimplexFBM3D(float x, float y, float z, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += Simplex3D(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value / maxValue;
        }

        // =====================================================================
        // Voronoi / Worley Noise
        // =====================================================================

        /// <summary>
        /// Generates 2D Voronoi (Worley/cellular) noise.
        /// Returns the distance to the nearest cell center, creating a cell-like pattern.
        /// Useful for cobblestone, scales, crystal, and organic cell textures.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="cellJitter">Amount of randomness in cell point positions (0 = grid, 1 = fully random).</param>
        /// <param name="distanceType">0 = Euclidean, 1 = Manhattan, 2 = Chebyshev.</param>
        /// <returns>Tuple of (F1: nearest distance, F2: second nearest distance).</returns>
        public static (float F1, float F2) Voronoi2D(float x, float y, float cellJitter = 1f, int distanceType = 0)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);

            float d1 = float.MaxValue;
            float d2 = float.MaxValue;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = xi + dx;
                    int cy = yi + dy;

                    // Deterministic random offset for this cell
                    float rx = MathUtils.SpatialHash2D(cx, cy) * cellJitter;
                    float ry = MathUtils.SpatialHash2D(cx + 127, cy + 311) * cellJitter;

                    float px = cx + rx;
                    float py = cy + ry;

                    float dist;
                    switch (distanceType)
                    {
                        case 1: // Manhattan
                            dist = MathF.Abs(x - px) + MathF.Abs(y - py);
                            break;
                        case 2: // Chebyshev
                            dist = MathF.Max(MathF.Abs(x - px), MathF.Abs(y - py));
                            break;
                        default: // Euclidean
                            float ddx = x - px, ddy = y - py;
                            dist = MathF.Sqrt(ddx * ddx + ddy * ddy);
                            break;
                    }

                    if (dist < d1)
                    {
                        d2 = d1;
                        d1 = dist;
                    }
                    else if (dist < d2)
                    {
                        d2 = dist;
                    }
                }
            }

            return (d1, d2);
        }

        /// <summary>
        /// Generates 3D Voronoi noise.
        /// </summary>
        public static (float F1, float F2) Voronoi3D(float x, float y, float z,
            float cellJitter = 1f, int distanceType = 0)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            int zi = (int)MathF.Floor(z);

            float d1 = float.MaxValue;
            float d2 = float.MaxValue;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int cx = xi + dx, cy = yi + dy, cz = zi + dz;

                        float rx = MathUtils.SpatialHash3D(cx, cy, cz) * cellJitter;
                        float ry = MathUtils.SpatialHash3D(cx + 127, cy + 311, cz + 523) * cellJitter;
                        float rz = MathUtils.SpatialHash3D(cx + 269, cy + 631, cz + 101) * cellJitter;

                        float px = cx + rx, py = cy + ry, pz = cz + rz;

                        float dist;
                        switch (distanceType)
                        {
                            case 1:
                                dist = MathF.Abs(x - px) + MathF.Abs(y - py) + MathF.Abs(z - pz);
                                break;
                            case 2:
                                dist = MathF.Max(MathF.Max(MathF.Abs(x - px), MathF.Abs(y - py)), MathF.Abs(z - pz));
                                break;
                            default:
                                float ddx = x - px, ddy = y - py, ddz = z - pz;
                                dist = MathF.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);
                                break;
                        }

                        if (dist < d1) { d2 = d1; d1 = dist; }
                        else if (dist < d2) { d2 = dist; }
                    }
                }
            }

            return (d1, d2);
        }

        // =====================================================================
        // Specialized Noise Patterns
        // =====================================================================

        /// <summary>
        /// Generates a marble-like noise pattern using turbulence distortion.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="frequency">Base frequency of the marble veins.</param>
        /// <param name="turbulence">Strength of noise distortion.</param>
        public static float MarbleNoise(float x, float y, float frequency = 5f, float turbulence = 5f)
        {
            float value = x * frequency + turbulence * Turbulence2D(x, y, 6, 2f, 0.5f, 0.01f);
            return MathF.Sin(value * MathF.PI) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Generates a wood-grain-like noise pattern with concentric rings.
        /// </summary>
        public static float WoodNoise(float x, float y, float ringFrequency = 20f, float noiseScale = 0.1f)
        {
            float dist = MathF.Sqrt(x * x + y * y);
            float noise = Perlin2D(x * noiseScale, y * noiseScale);
            float value = MathF.Sin((dist + noise) * ringFrequency);
            return value * 0.5f + 0.5f;
        }

        /// <summary>
        /// Generates billowy cloud noise using absolute value of FBM.
        /// </summary>
        public static float CloudNoise(float x, float y, float z, int octaves = 8)
        {
            float value = MathF.Abs(FBM3D(x, y, z, octaves, 2f, 0.5f, 0.02f));
            return MathUtils.Clamp01(value * 2f);
        }

        /// <summary>
        /// Generates erosion-like noise with sharp channels carved into terrain.
        /// Combines ridge noise with FBM for realistic mountain erosion patterns.
        /// </summary>
        public static float ErosionNoise(float x, float y, int octaves = 8)
        {
            float base_terrain = Ridge2D(x, y, octaves / 2, 2f, 2f, 1f, 0.005f);
            float erosion = FBM2D(x + base_terrain * 2f, y + base_terrain * 2f, octaves, 2f, 0.5f, 0.01f);
            return base_terrain * 0.7f + erosion * 0.3f;
        }

        // =====================================================================
        // Seeded Noise Utilities
        // =====================================================================

        /// <summary>
        /// Generates seeded 2D Perlin noise by offsetting coordinates with a seed value.
        /// This allows multiple independent noise fields from the same generator.
        /// </summary>
        public static float SeededPerlin2D(float x, float y, int seed)
        {
            float offsetX = (seed * 73856093 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;
            float offsetY = (seed * 19349663 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;
            return Perlin2D(x + offsetX, y + offsetY);
        }

        /// <summary>
        /// Generates seeded 2D FBM noise.
        /// </summary>
        public static float SeededFBM2D(float x, float y, int seed, int octaves = 6,
            float lacunarity = 2f, float persistence = 0.5f, float scale = 1f)
        {
            float offsetX = (seed * 73856093 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;
            float offsetY = (seed * 19349663 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;
            return FBM2D(x + offsetX, y + offsetY, octaves, lacunarity, persistence, scale);
        }

        // =====================================================================
        // Noise Map Generation
        // =====================================================================

        /// <summary>
        /// Generates a 2D heightmap array using FBM noise.
        /// Useful for terrain generation.
        /// </summary>
        /// <param name="width">Width of the heightmap.</param>
        /// <param name="height">Height of the heightmap.</param>
        /// <param name="scale">Noise frequency scale.</param>
        /// <param name="octaves">Number of FBM octaves.</param>
        /// <param name="seed">Random seed for offset.</param>
        /// <param name="normalize">If true, normalizes values to [0, 1].</param>
        public static float[,] GenerateHeightmap(int width, int height, float scale = 0.01f,
            int octaves = 6, int seed = 0, bool normalize = true)
        {
            float[,] map = new float[width, height];
            float offsetX = (seed * 73856093 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;
            float offsetY = (seed * 19349663 & 0x7FFFFFFF) / (float)0x7FFFFFFF * 1000f;

            float min = float.MaxValue, max = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = FBM2D(
                        (x + offsetX) * scale,
                        (y + offsetY) * scale,
                        octaves
                    );
                    map[x, y] = value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            if (normalize && max > min)
            {
                float range = max - min;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        map[x, y] = (map[x, y] - min) / range;
            }

            return map;
        }
    }
}
