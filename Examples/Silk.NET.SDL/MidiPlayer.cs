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

    public unsafe void ProcessAudio(void* data, byte* stream, int length)
    {
        var sampleCount = length / sizeof(float);
        var destination = new Span<float>(stream, sampleCount);

        lock (mutex)
        {
            sequencer.RenderInterleaved(destination);
        }
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
