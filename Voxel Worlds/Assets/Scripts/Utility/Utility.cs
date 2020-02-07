namespace Voxel.Noise
{
    #pragma warning disable IDE1006 // Methods correctly named
    public static class Utility
    {
        private static readonly FastNoise noise = new FastNoise();

        private const int octaves2D = 3;
        private const float lacunarity2D = 3;
        private const float gain2D = 0.25f;
        private const float scale2D = 0.75f;

        public static float fBm2D(float x, float z)
        {
            float frequency = 1;
            float amplitude = 0.4f;

            float value = 0;
            for (int i = 0; i < octaves2D; i++)
            {
                value += Abs(noise.GetSimplex(x * frequency, 1, z * frequency) * amplitude);
                frequency *= lacunarity2D;
                amplitude *= gain2D;
            }

            return 0.2f + (value * scale2D);
        }

        private const int octaves3D = 3;
        private const float lacunarity3D = 3;
        private const float gain3D = 0.25f;
        private const float scale3D = 1;

        public static float fBm3D(float x, float y, float z)
        {
            float frequency = 1;
            float amplitude = 0.4f;

            float value = 0;
            for (int i = 0; i < octaves3D; i++)
            {
                value += Abs(noise.GetSimplex(x * frequency, y * frequency, z * frequency) * amplitude);
                frequency *= lacunarity3D;
                amplitude *= gain3D;
            }

            return value * scale3D;
        }

        private static float Abs(float value)
        {
            return value >= 0 ? value : -value;
        }
    }
}