﻿namespace Voxel.Utility
{
	public static class NoiseUtils
	{
		private static readonly FastNoise noise = new FastNoise(seed: 1000);

		// Values for 2D fractal brownian motion
		private const int octaves2D = 2;
		private const float baseFrequency2D = 0.75f;
		private const float baseAmplitude2D = 0.35f;
		private const float lacunarity2D = 3f;
		private const float gain2D = 0.25f;
		private const float scale2D = 0.7f;
		private const float baseValue = 0.2f;

		// Values for 3D fractral brownian motion
		private const int octaves3D = 2;
		private const float baseFrequency3D = 1;
		private const float baseAmplitude3D = 0.4f;
		private const float lacunarity3D = 3;
		private const float gain3D = 0.25f;
		private const float scale3D = 1.2f;

		public static float FBM2D(float x, float z)
		{
			float frequency = baseFrequency2D;
			float amplitude = baseAmplitude2D;

			float value = 0;
			for (int i = 0; i < octaves2D; i++)
			{
				value += FastAbs(noise.GetSimplex(x * frequency, 1, z * frequency) * amplitude);
				frequency *= lacunarity2D;
				amplitude *= gain2D;
			}

			return baseValue + (value * scale2D);
		}

		public static float FBM3D(float x, float y, float z)
		{
			float frequency = baseFrequency3D;
			float amplitude = baseAmplitude3D;

			float value = 0;
			for (int i = 0; i < octaves3D; i++)
			{
				value += FastAbs(noise.GetSimplex(x * frequency, y * frequency, z * frequency) * amplitude);
				frequency *= lacunarity3D;
				amplitude *= gain3D;
			}

			return value * scale3D;
		}

		private static float FastAbs(float value) => value >= 0 ? value : -value;
	}
}