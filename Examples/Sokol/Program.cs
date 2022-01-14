using System;
using System.Runtime.InteropServices;
using Sokol;
using MeltySynth;

class Program
{
    static Synthesizer synthesizer = new Synthesizer("TimGM6mb.sf2", 44100);
    static MidiFileSequencer sequencer = new MidiFileSequencer(synthesizer);
    static object mutex = new object();

    unsafe static void Main()
    {
        var desc = new Audio.Desc();
        desc.SampleRate = synthesizer.SampleRate;
        desc.NumChannels = 2;
        desc.StreamCb = (delegate* unmanaged<float*, int, int, void>)&ProcessAudio;

        Audio.Setup(in desc);

        if (!Audio.Isvalid())
        {
            throw new Exception("Failed to setup audio.");
        }

        // Load the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

        // Play the MIDI file.
        Play(midiFile, true);

        // Wait until any key is pressed.
        Console.ReadKey();

        Audio.Shutdown();
    }

    [UnmanagedCallersOnly]
    unsafe static void ProcessAudio(float* buffer, int numFrames, int numChannels)
    {
        lock (mutex)
        {
            sequencer.RenderInterleaved(new Span<float>(buffer, 2 * numFrames));
        }
    }

    static void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }
    }

    static void Stop()
    {
        lock (mutex)
        {
            sequencer.Stop();
        }
    }
}
