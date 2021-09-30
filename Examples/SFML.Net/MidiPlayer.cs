using System;
using SFML.Audio;
using SFML.System;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private MidiSoundStream stream;

    public MidiPlayer(string soundFontPath)
    {
        stream = new MidiSoundStream(soundFontPath, 44100);
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        stream.PlayMidi(midiFile, loop);
    }

    public void Stop()
    {
        stream.StopMidi();
    }

    public void Dispose()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }



    private class MidiSoundStream : SoundStream
    {
        private Synthesizer synthesizer;
        private MidiFileSequencer sequencer;

        private short[] batch;

        private object mutex;

        public MidiSoundStream(string soundFontPath, int sampleRate)
        {
            synthesizer = new Synthesizer(soundFontPath, sampleRate);
            sequencer = new MidiFileSequencer(synthesizer);

            batch = new short[2 * (int)Math.Round(0.05 * sampleRate)];

            mutex = new object();

            Initialize(2, (uint)sampleRate);
        }

        public void PlayMidi(MidiFile midiFile, bool loop)
        {
            lock (mutex)
            {
                sequencer.Play(midiFile, loop);
            }

            if (Status == SoundStatus.Stopped)
            {
                Play();
            }
        }

        public void StopMidi()
        {
            lock (mutex)
            {
                sequencer.Stop();
            }
        }

        protected override bool OnGetData(out short[] samples)
        {
            lock (mutex)
            {
                sequencer.RenderInterleavedInt16(batch);
            }

            samples = batch;

            return true;
        }

        protected override void OnSeek(Time timeOffset)
        {
            throw new NotSupportedException();
        }
    }
}
