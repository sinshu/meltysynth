using System;

namespace MeltySynth
{
    internal static class SoundFontMath
    {
        public static readonly float NonAudible = 1.0E-3F;

        public static float TimecentsToSeconds(float x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToHertz(float x)
        {
            return 8.176F * MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToMultiplyingFactor(float x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float DecibelsToLinear(float x)
        {
            return MathF.Pow(10F, 0.05F * x);
        }

        public static float LinearToDecibels(float x)
        {
            return 20F * MathF.Log10(x);
        }
    }
}
