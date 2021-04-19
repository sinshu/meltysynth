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
        private short pan;
        private short expression;

        private short rpn;
        private short pitchBendRange;
        private short coarseTune;
        private short fineTune;

        private float pitchBend;

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
            pan = 64 << 7;
            expression = 127 << 7;

            rpn = -1;
            pitchBendRange = 2 << 7;
            coarseTune = 0;
            fineTune = 8192;

            pitchBend = 0F;
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

        public void SetModulationCoarse(int value)
        {
            modulation = (short)((modulation & 0x7F) | (value << 7));
        }

        public void SetModulationFine(int value)
        {
            modulation = (short)((modulation & 0xFF80) | value);
        }

        public void SetVolumeCoarse(int value)
        {
            volume = (short)((volume & 0x7F) | (value << 7));
        }

        public void SetVolumeFine(int value)
        {
            volume = (short)((volume & 0xFF80) | value);
        }

        public void SetPanCoarse(int value)
        {
            pan = (short)((pan & 0x7F) | (value << 7));
        }

        public void SetPanFine(int value)
        {
            pan = (short)((pan & 0xFF80) | value);
        }

        public void SetExpressionCoarse(int value)
        {
            expression = (short)((expression & 0x7F) | (value << 7));
        }

        public void SetExpressionFine(int value)
        {
            expression = (short)((expression & 0xFF80) | value);
        }

        public void SetRpnCoarse(int value)
        {
            rpn = (short)((rpn & 0x7F) | (value << 7));
        }

        public void SetRpnFine(int value)
        {
            rpn = (short)((rpn & 0xFF80) | value);
        }

        public void DataEntryCoarse(int value)
        {
            switch (rpn)
            {
                case 0:
                    pitchBendRange = (short)((pitchBendRange & 0x7F) | (value << 7));
                    break;

                case 1:
                    fineTune = (short)((fineTune & 0x7F) | (value << 7));
                    break;

                case 2:
                    coarseTune = (short)(value - 64);
                    break;
            }
        }

        public void DataEntryFine(int value)
        {
            switch (rpn)
            {
                case 0:
                    pitchBendRange = (short)((pitchBendRange & 0xFF80) | value);
                    break;

                case 1:
                    fineTune = (short)((fineTune & 0xFF80) | value);
                    break;
            }
        }

        public void SetPitchBend(int value1, int value2)
        {
            pitchBend = ((value1 | (value2 << 7)) - 8192) / 8192F;
        }

        public Preset Preset => synthesizer.GetPreset(bankNumber, patchNumber);

        public float Modulation => modulation * (50F / 16383F);
        public float Volume => volume / 16383F;
        public float Pan => pan * (100F / 16383F) - 50F;
        public float Expression => expression / 16383F;

        public float PitchBendRange => (pitchBendRange >> 7) + 0.01F * (pitchBendRange & 0x7F);
        public float Tune => coarseTune + (fineTune - 8192) / 8192F;

        public float PitchBend => PitchBendRange * pitchBend;
    }
}
