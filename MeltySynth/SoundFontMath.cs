using System;

namespace MeltySynth
{
    internal static class SoundFontMath
    {
        internal static float TimecentsToSeconds(int x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        internal static float CentsToHertz(int x)
        {
            return 8.176F * MathF.Pow(2F, x / 1200F);
        }
    }
}
