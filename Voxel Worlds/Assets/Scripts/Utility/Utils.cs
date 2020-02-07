namespace Voxel.Noise
{
    public static class Utils
    {
        private static readonly FastNoise noiseGenerator = new FastNoise();

        public static float fBm2D(float x, float z)
        {
            int octaves = 3;
            float frequency = 1;
            float amplitude = 0.4f;
            float lacunarity = 3;
            float gain = 0.25f;

            float finalScale = 0.75f;
            //float clampMax = 1;
            //float clampMin = 0;

            float finalValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                finalValue += Abs(noiseGenerator.GetSimplex(x * frequency, 1, z * frequency) * amplitude);
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return 0.2f + (finalValue * finalScale);

            //float Clamp(float value)
            //{
            //    if (value >= clampMin && value <= clampMax) return value;
            //    else return value <= clampMin ? clampMin : clampMax;
            //}
        }

        public static float fBm3D(float x, float y, float z)
        {
            int octaves = 3;
            float frequency = 1;
            float amplitude = 0.4f;
            float lacunarity = 3;
            float gain = 0.25f;

            float finalScale = 1;

            float finalValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                finalValue += Abs(noiseGenerator.GetSimplex(x * frequency, y * frequency, z * frequency) * amplitude);
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return finalValue * finalScale;
        }

        private static float Abs(float value)
        {
            return value >= 0 ? value : -value;
        }
    }
}