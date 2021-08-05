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
        Normal = 0,

        /// <summary>
        /// The RPG Maker style loop is used.
        /// CC #111 will be regarded as the loop start point.
        /// </summary>
        RpgMaker,

        /// <summary>
        /// The Incredible Machine style loop is used.
        /// CC #110 will be regarded as the loop start point.
        /// CC #111 will be regarded as the loop end point.
        /// </summary>
        IncredibleMachine,

        /// <summary>
        /// The Final Fantasy style loop is used.
        /// CC #116 will be regarded as the loop start point.
        /// CC #117 will be regarded as the loop end point.
        /// </summary>
        FinalFantasy
    }
}
