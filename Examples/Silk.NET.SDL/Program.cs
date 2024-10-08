﻿using System;
using Silk.NET.SDL;
using MeltySynth;

class Program
{
    unsafe static void Main()
    {
        var player = new MidiPlayer("TimGM6mb.sf2");

        using (var sdl = Sdl.GetApi())
        {
            sdl.InitSubSystem(Sdl.InitAudio);

            var desired = new AudioSpec();
            desired.Freq = 44100;
            desired.Format = Sdl.AudioF32;
            desired.Channels = 2;
            desired.Samples = 4096;
            desired.Callback = new PfnAudioCallback(new AudioCallback(player.ProcessAudio));

            var device = sdl.OpenAudioDevice((byte*)null, 0, &desired, (AudioSpec*)null, (int)Sdl.AudioAllowAnyChange);
            if (device == 0)
            {
                throw sdl.GetErrorAsException() ?? new Exception();
            }

            sdl.PauseAudioDevice(device, 0);

            // Load the MIDI file.
            var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

            // Play the MIDI file.
            player.Play(midiFile, true);

            // Wait until any key is pressed.
            Console.ReadKey();

            sdl.CloseAudioDevice(device);

            sdl.QuitSubSystem(Sdl.InitAudio);
        }
    }
}
