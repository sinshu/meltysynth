using System;

namespace MeltySynth
{
    internal sealed class Channel
    {
        private Synthesizer synthesizer;
        private bool isPercussionChannel;

        private float[] blockLeft;
        private float[] blockRight;

        private int bankNumber;
        private int patchNumber;

        private short modulation;
        private short volume;
        private short expression;

        internal Channel(Synthesizer synthesizer, bool isPercussionChannel)
        {
            this.synthesizer = synthesizer;
            this.isPercussionChannel = isPercussionChannel;

            blockLeft = new float[synthesizer.BlockSize];
            blockRight = new float[synthesizer.BlockSize];

            Reset();
        }

        public void Reset()
        {
            bankNumber = isPercussionChannel ? 128 : 0;
            patchNumber = 0;

            modulation = 0;
            volume = 100 << 7;
            expression = 127 << 7;
        }

        public void SetBank(int value)
        {
            bankNumber = value;

            if (isPercussionChannel)
            {
                bankNumber += 128;
            }
        }

        public void SetPatch(int value)
        {
            patchNumber = value;
        }

        public void SetModulationCourse(int value)
        {
            modulation = (short)((modulation & 0x7F) | (value << 7));
        }

        public void SetModulationFine(int value)
        {
            modulation = (short)((modulation & 0xFF80) | value);
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

        public Preset Preset => synthesizer.GetPreset(bankNumber, patchNumber);

        public float Modulation => modulation * (50F / 16383F);
        public float Volume => volume / 16383F;
        public float Expression => expression / 16383F;
    }
}
