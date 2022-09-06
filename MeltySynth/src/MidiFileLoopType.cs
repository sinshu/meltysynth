using System;

namespace MeltySynth
{
    /// <summary>
    /// Specifies the non-standard loop extension for MIDI files.
    /// </summary>
    public enum MidiFileLoopType
    {
        /// <summary>
        /// No special loop extension.
        /// </summary>
        None = 0,

        /// <summary>
        /// The RPG Maker style loop.
        /// CC #111 will be the loop start point.
        /// </summary>
        RpgMaker,

        /// <summary>
        /// The Incredible Machine style loop.
        /// CC #110 and #111 will be the loop start point and end point, respectively.
        /// </summary>
        IncredibleMachine,

        /// <summary>
        /// The Final Fantasy style loop.
        /// CC #116 and #117 will be the loop start point and end point, respectively.
        /// </summary>
        FinalFantasy
    }
}
