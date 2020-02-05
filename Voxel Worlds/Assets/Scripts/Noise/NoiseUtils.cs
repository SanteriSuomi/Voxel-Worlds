namespace Voxel.Noise
{
    public class NoiseUtils
    {
        public static NoiseUtils Instance { get; set; }

        private readonly FastNoise noiseGenerator;

        public NoiseUtils()
        {
            noiseGenerator = new FastNoise();
            noiseGenerator.SetNoiseType(FastNoise.NoiseType.Simplex);
            noiseGenerator.SetSeed(12309856);
            noiseGenerator.SetFrequency(0.01f);
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public float fBm2D(float x, float y)
        {
            int octaves = 2;
            float frequency = 1;
            float amplitude = 1;
            float lacunarity = 2;
            float gain = 0.5f;
            float scale = 0.005f;

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

            return finalValue * scale;
        }
    }
}