using System;

namespace MeltySynth.SoundFont
{
    public sealed class SoundFontVersion
    {
        public static readonly SoundFontVersion Empty = new SoundFontVersion(0, 0);

        private short major;
        private short minor;

        internal SoundFontVersion(short major, short minor)
        {
            this.major = major;
            this.minor = minor;
        }

        public override string ToString()
        {
            return $"{major}.{minor}";
        }

        public short Major => major;
        public short Minor => minor;
    }
}
