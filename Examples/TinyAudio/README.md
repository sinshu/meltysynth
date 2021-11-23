## TinyAudio Example

This is an example implementation of a MIDI player backed by TinyAudio.

Usage:
```cs
using System;
using TinyAudio;
using MeltySynth;

class Program
{
    static void Main()
    {
        var midiPlayer = new MidiPlayer("TimGM6mb.sf2");

        var format = new AudioFormat(44100, 2, SampleFormat.SignedPcm16);
        var bufferLength = TimeSpan.FromMilliseconds(200);

        using (var player = new DirectSoundAudioPlayer(format, bufferLength))
        {
            player.BeginPlayback(midiPlayer.ProcessAudio);

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
