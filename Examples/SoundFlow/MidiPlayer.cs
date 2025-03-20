using System;
using SoundFlow.Abstracts;
using MeltySynth;

public class MidiPlayer : SoundComponent
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;

    public MidiPlayer(string soundFontPath, int sampleRate)
    {
        synthesizer = new Synthesizer(soundFontPath, sampleRate);
        sequencer = new MidiFileSequencer(synthesizer);

        mutex = new object();
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

    protected override void GenerateAudio(Span<float> buffer)
    {
        lock (mutex)
        {
            sequencer.RenderInterleaved(buffer);
        }
    }
}
