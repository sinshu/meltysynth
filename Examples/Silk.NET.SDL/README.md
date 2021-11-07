## Silk.NET Example (SDL)

This is an example implementation of a MIDI player backed by Silk.NET.

Usage:
```cs
using System;
using Silk.NET.SDL;
using MeltySynth;

class Program
{
    unsafe static void Main(string[] args)
    {
        var player = new MidiPlayer("TimGM6mb.sf2");

        var sdl = Sdl.GetApi();

        var desired = new AudioSpec();
        desired.Freq = 44100;
        desired.Format = Sdl.AudioF32;
        desired.Channels = 2;
        desired.Samples = 4096;
        desired.Callback = new PfnAudioCallback(new AudioCallback(player.ProcessAudio));

        var obtained = new AudioSpec();

        if (sdl.AudioInit((byte*)null) < 0)
        {
            throw new Exception("Failed to initialize audio.");
        }

        if (sdl.OpenAudio(ref desired, ref obtained) < 0)
        {
            throw new Exception("Failed to open audio.");
        }

        sdl.PauseAudio(0);

        // Load the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

        // Play the MIDI file.
        player.Play(midiFile, true);

        // Wait until any key is pressed.
        Console.ReadKey();

        sdl.PauseAudio(1);

        sdl.Dispose();
    }
}
```
