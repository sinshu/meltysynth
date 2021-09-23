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
        /// The RPG Maker style loop is used.
        /// CC #111 corresponds to the loop start point in this case.
        /// </summary>
        RpgMaker,

        /// <summary>
        /// The Incredible Machine style loop is used.
        /// CC #110 and #111 respectively correspond to the loop start point and end point in this case.
        /// </summary>
        IncredibleMachine,

        /// <summary>
        /// The Final Fantasy style loop is used.
        /// CC #116 and #117 respectively correspond to the loop start point and end point in this case.
        /// </summary>
        FinalFantasy
    }
}
