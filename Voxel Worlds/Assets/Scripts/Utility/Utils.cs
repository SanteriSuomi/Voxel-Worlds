namespace Voxel.Noise
{
    public static class Utils
    {
        private static readonly FastNoise noiseGenerator = new FastNoise();

        public static float fBm2D(float x, float y)
        {
            int octaves = 2;
            float frequency = 1;
            float amplitude = 1;
            float lacunarity = 2;
            float gain = 0.5f;

            float scale = 1;
            float clampMax = 1;

            float finalValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                finalValue += Absolute(noiseGenerator.GetNoise(x * frequency, y * frequency, 1)) * amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            float Absolute(float value)
            {
                return (value >= 0) ? value : -value;
            }

            float Clamp(float value)
            {
                return value <= clampMax ? value : clampMax;
            }

            return Clamp(finalValue * scale);
        }
    }
}