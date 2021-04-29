## SFML.Net Example

This is an example implementation of a MIDI player backed by SFML.Net.

Usage:
```cs
using System;
using MeltySynth;

class Program
{
    static void Main()
    {
        using (var player = new MidiPlayer("TimGM6mb.sf2"))
        {
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
