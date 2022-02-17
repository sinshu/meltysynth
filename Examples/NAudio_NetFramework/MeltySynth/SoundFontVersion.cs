using System;

namespace MeltySynth
{
    /// <summary>
    /// Reperesents the version of a SoundFont.
    /// </summary>
    public struct SoundFontVersion
    {
        private readonly short major;
        private readonly short minor;

        internal SoundFontVersion(short major, short minor)
        {
            this.major = major;
            this.minor = minor;
        }

        /// <summary>
        /// Gets the string representation of the version.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{major}.{minor}";
        }

        /// <summary>
        /// The major version.
        /// </summary>
        public short Major => major;

        /// <summary>
        /// The minor version.
        /// </summary>
        public short Minor => minor;
    }
}
