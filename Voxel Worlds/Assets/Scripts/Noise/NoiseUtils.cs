using UnityEngine;

namespace Voxel.Noise
{
    public static class NoiseUtils
    {
        static NoiseUtils()
        {
            xNoise.Seed = 098123567;
        }

        public static float Noise2D(int x, int y)
        {
            return xNoise.CalcPixel2D(x, y, 1);
        }

        public static float Noise3D(int x, int y, int z)
        {
            return xNoise.CalcPixel3D(x, y, z, 1);
        }

        //private static int fBmOctaves = 1; // Number of noise "loops"
        //private static float fBmLacunarity = 2; // Steps
        //private static float fBmGain = 0.5f; // Decrease/increase of amplitude
        //private static float fBmAmplitude = 1; // How tall features are
        //private static float fBmFrequency = 1; // Number of feature points

        //public static void SetfBmProperties(int octaves, float lacunarity, float gain, float amplitude, float frequency)
        //{
        //    fBmOctaves = octaves;
        //    fBmLacunarity = lacunarity;
        //    fBmGain = gain;
        //    fBmAmplitude = amplitude;
        //    fBmFrequency = frequency;
        //}

        //public static float fBm1D(float x)
        //{
        //    float value = 0;
        //    float amplitude = fBmAmplitude;
        //    float frequency = fBmFrequency;
        //    for (int i = 0; i < fBmOctaves; i++)
        //    {
        //        value += amplitude * Noise2D((int)x, 1);
        //        amplitude *= fBmGain;
        //        frequency *= fBmLacunarity;
        //    }
            
        //    return value;
        //}

        public static float fBm2D(float x, float y)
        {
            int octaves = 2;
            float frequency = 1;
            float amplitude = 1;
            float gain = 0.5f;
            float lacunarity = 2;

            float value = 0;
            for (int i = 0; i < octaves; i++)
            {
                //value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                //xNoise.Seed = Random.Range(0, 1000);
                value += Noise2D((int)(x * frequency), (int)(y * frequency)) * amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return value * 0.00001f; // This OpenSimpleX implementation returns 0-255 so we need to make it in range of 0-255
        }
    }
}