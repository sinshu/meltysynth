using System;

namespace MeltySynth
{
    /// <summary>
    /// Specifies how the synthesizer loops the sample.
    /// </summary>
    public enum LoopMode
    {
        /// <summary>
        /// The sample will be played without loop.
        /// </summary>
        NoLoop = 0,

        /// <summary>
        /// The sample will continuously loop.
        /// </summary>
        Continuous = 1,

        /// <summary>
        /// The sample will loop until the note stops.
        /// </summary>
        LoopUntilNoteOff = 3
    }
}
