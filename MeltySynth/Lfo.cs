using System;

namespace MeltySynth
{
    internal sealed class Lfo
    {
        internal Synthesizer synthesizer;

        public Lfo(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }
    }
}
