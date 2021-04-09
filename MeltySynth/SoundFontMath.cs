using System;

namespace MeltySynth
{
    internal static class SoundFontMath
    {
        public static readonly float NonAudible = 1.0E-5F;

        public static float TimecentsToSeconds(int x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToHertz(int x)
        {
            return 8.176F * MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToMultiplyingFactor(int x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToMultiplyingFactor(float x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float DecibelsToLinear(float x)
        {
            if (x > -100F)
            {
                return MathF.Pow(10F, 0.05F * x);
            }
            else
            {
                return 0;
            }
        }
    }
}
