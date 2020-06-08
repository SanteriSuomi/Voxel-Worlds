using System;

namespace Voxel.Utility
{
    public static class MathUtils
    {
        public static int GetNearestMultipleOf(int value, int factor)
            => (int)Math.Round(value / (double)factor, MidpointRounding.AwayFromZero) * factor;

        public static float GetNearestMultipleOf(float value, float factor)
            => (float)Math.Round(value / (double)factor, MidpointRounding.AwayFromZero) * factor;
    }
}