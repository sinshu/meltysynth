using System;

namespace MeltySynth
{
    public static class SoundFontMath
    {
        public static float TimecentsToSeconds(int x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        public static float CentsToHertz(int x)
        {
            return 8.176F * MathF.Pow(2F, x / 1200F);
        }
    }
}
