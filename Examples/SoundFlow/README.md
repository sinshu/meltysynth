## SoundFlow Example

This is an example implementation of a MIDI player backed by SoundFlow.

Usage:
```cs
using System;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using MeltySynth;

class Program
{
    static void Main()
    {
        using (var engine = new MiniAudioEngine(44100, Capability.Playback))
        {
            var player = new MidiPlayer("TimGM6mb.sf2", engine.SampleRate);
            Mixer.Master.AddComponent(player);

            // Load the MIDI file.
            var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

            // Play the MIDI file.
            player.Play(midiFile, true);

            // Wait until any key is pressed.
            Console.ReadKey();
        }
    }
}
```
