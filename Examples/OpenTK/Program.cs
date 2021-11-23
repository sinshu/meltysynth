using System;
using OpenTK.Audio.OpenAL;
using MeltySynth;

class Program
{
    unsafe static void Main()
    {
        var device = ALC.OpenDevice(null);
        var context = ALC.CreateContext(device, (int*)null);
        ALC.MakeContextCurrent(context);
        AL.GetError();

        using (var player = new MidiPlayer("TimGM6mb.sf2"))
        {
            // Load the MIDI file.
            var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

            // Play the MIDI file.
            player.Play(midiFile, true);

            // Wait until any key is pressed.
            Console.ReadKey();
        }

        ALC.DestroyContext(context);
        ALC.CloseDevice(device);
    }
}
