namespace MeltySynth
{
    public static class MidiCommand
    {
        public const int NOTE_OFF = 0x80;
        public const int NOTE_ON = 0x90;
        public const int CONTROLLER = 0xB0;
        public const int BANK_SELECTION = 0x00;
        public const int MODULATION_COARSE = 0x01;
        public const int MODULATION_FINE = 0x21;
        public const int DATA_ENTRY_COARSE = 0x06;
        public const int DATA_ENTRY_FINE = 0x26;
        public const int CHANNEL_VOLUME_COARSE = 0x07;
        public const int CHANNEL_VOLUME_FINE = 0x27;
        public const int PAN_COARSE = 0x0A;
        public const int PAN_FINE = 0x2A;
        public const int EXPRESSION_COARSE = 0x0B;
        public const int EXPRESSION_FINE = 0x2B;
        public const int HOLD_PEDAL = 0x40;
        public const int REVERB_SEND = 0x5B;
        public const int CHORUS_SEND = 0x5D;
        public const int RPN_COARSE = 0x65;
        public const int RPN_FINE = 0x64;
        public const int ALL_SOUND_OFF = 0x78;
        public const int RESET_ALL_CONTROLLERS = 0x79;
        public const int ALL_NOTE_OFF = 0x7B;
        public const int PROGRAM_CHANGE = 0xC0;
        public const int PITCH_BEND = 0xE0;
    }
}
