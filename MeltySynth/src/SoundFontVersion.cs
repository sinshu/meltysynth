using System;

namespace MeltySynth
{
    public struct SoundFontVersion
    {
        private readonly short major;
        private readonly short minor;

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
