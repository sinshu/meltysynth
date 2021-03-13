using System;

namespace MeltySynth.SoundFont
{
    public static class SoundFontMath
    {
        public static float TimecentToSecond(short x)
        {
            return MathF.Pow(2F, x / 1200F);
        }
    }
}
