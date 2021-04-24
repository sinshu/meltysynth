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
    }
}
