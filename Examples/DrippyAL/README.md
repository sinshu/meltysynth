## DrippyAL Example

This is an example implementation of a MIDI player backed by DrippyAL.

Usage:
```cs
using System;
using DrippyAL;
using MeltySynth;

class Program
{
    static void Main()
    {
        var midiPlayer = new MidiPlayer("TimGM6mb.sf2");

        using (var device = new AudioDevice())
        using (var stream = new AudioStream(device, 44100, 2))
        {
            stream.Play(midiPlayer.FillBlock);

            // Load the MIDI file.
            var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

            // Play the MIDI file.
            midiPlayer.Play(midiFile, true);

            // Wait until any key is pressed.
            Console.ReadKey();
        }
    }
}
```
