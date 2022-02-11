using System;
using MeltySynth;

public class MidiPlayer
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;

    public MidiPlayer(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);

        synthesizer = new Synthesizer(soundFontPath, settings);
        sequencer = new MidiFileSequencer(synthesizer);

        mutex = new object();
    }

    public void ProcessAudio(Span<short> buffer, out int samplesWritten)
    {
        lock (mutex)
        {
            sequencer.RenderInterleavedInt16(buffer);
        }

        samplesWritten = buffer.Length;
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }
    }

    public void Stop()
    {
        lock (mutex)
        {
            sequencer.Stop();
        }
    }
}
