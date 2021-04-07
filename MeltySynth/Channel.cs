using System;

namespace MeltySynth
{
    internal sealed class Channel
    {
        private Synthesizer synthesizer;

        private short volume;
        private short expression;

        internal Channel(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;

            Reset();
        }

        public void Reset()
        {
            volume = 100 << 7;
            expression = 127 << 7;
        }

        public void SetVolumeCourse(int value)
        {
            volume = (short)((volume & 0x7F) | (value << 7));
        }

        public void SetVolumeFine(int value)
        {
            volume = (short)((volume & 0xFF80) | value);
        }

        public void SetExpressionCourse(int value)
        {
            expression = (short)((expression & 0x7F) | (value << 7));
        }

        public void SetExpressionFine(int value)
        {
            expression = (short)((expression & 0xFF80) | value);
        }

        public float Volume => volume / 16383F;
        public float Expression => expression / 16383F;
    }
}
