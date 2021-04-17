using System;

namespace MeltySynth
{
    internal static class SpanMath
    {
        public static void MultiplyAdd(float a, Span<float> x, Span<float> destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] += a * x[i];
            }
        }

        public static void Mean(Span<float> x, Span<float> y, Span<float> destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = (x[i] + y[i]) / 2;
            }
        }
    }
}
