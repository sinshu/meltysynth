using System;
using System.Linq;
using MeltySynth;

namespace MeltySynthTest
{
    public class TestSettings
    {
        public static object[] SoundFonts =
        {
            new object[] { "TimGM6mb", new SoundFont("TimGM6mb.sf2") },
            new object[] { "Arachno SoundFont - Version 1.0", new SoundFont("Arachno SoundFont - Version 1.0.sf2") },
            new object[] { "GeneralUser GS MuseScore v1.442", new SoundFont("GeneralUser GS MuseScore v1.442.sf2") },
            new object[] { "SGM-V2.01", new SoundFont("SGM-V2.01.sf2") }
        };

        public static object[] LightSoundFonts =
        {
            new object[] { "TimGM6mb", new SoundFont("TimGM6mb.sf2") },
            new object[] { "GeneralUser GS MuseScore v1.442", new SoundFont("GeneralUser GS MuseScore v1.442.sf2") }
        };

        public static SoundFont DefaultSoundFont => (SoundFont)((object[])SoundFonts[0])[1];
    }
}
