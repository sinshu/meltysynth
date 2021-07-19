using System;

namespace MeltySynth
{
    internal static class ArrayMath
    {
        public static void MultiplyAdd(float a, float[] x, float[] destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] += a * x[i];
            }
        }

        public static void MultiplyAdd(float a, float step, float[] x, float[] destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] += a * x[i];
                a += step;
            }
        }
    }
}
