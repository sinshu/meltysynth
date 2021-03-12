using System;

namespace MeltySynth.SoundFont
{
    public enum SampleType
    {
        Mono = 1,
        Right = 2,
        Left = 4,
        Linked = 8,
        RomMono = 0x8001,
        RomRight = 0x8002,
        RomLeft = 0x8004,
        RomLinked = 0x8008
    }
}
