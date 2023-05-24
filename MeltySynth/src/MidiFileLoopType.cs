using System;

namespace MeltySynth
{
    /// <summary>
    /// Specifies the non-standard loop extension for MIDI files.
    /// </summary>
    public enum MidiFileLoopType
    {
        /// <summary>
        /// No loop extension is used.
        /// </summary>
        None = 0,

        /// <summary>
        /// The RPG Maker style loop.
        /// CC #111 will be the loop start point.
        /// </summary>
        RpgMaker,

        /// <summary>
        /// The Incredible Machine style loop.
        /// CC #110 and #111 will be the start and end points of the loop.
        /// </summary>
        IncredibleMachine,

        /// <summary>
        /// The Final Fantasy style loop.
        /// CC #116 and #117 will be the start and end points of the loop.
        /// </summary>
        FinalFantasy
    }
}
