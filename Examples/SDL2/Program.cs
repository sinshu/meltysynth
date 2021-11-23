using System;
using SDL2;
using MeltySynth;

class Program
{
    unsafe static void Main()
    {
        var player = new MidiPlayer("TimGM6mb.sf2");

        SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO);

        var desired = new SDL.SDL_AudioSpec();
        desired.freq = 44100;
        desired.format = SDL.AUDIO_F32;
        desired.channels = 2;
        desired.samples = 4096;
        desired.callback = player.ProcessAudio;

        var obtained = new SDL.SDL_AudioSpec();

        var name = SDL.SDL_GetAudioDeviceName(0, 0);
        var device = SDL.SDL_OpenAudioDevice(name, 0, ref desired, out obtained, 0);
        if (device == 0)
        {
            throw new Exception("Failed to open the audio device.");
        }

        SDL.SDL_PauseAudioDevice(device, 0);

        // Load the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

        // Play the MIDI file.
        player.Play(midiFile, true);

        // Wait until any key is pressed.
        Console.ReadKey();

        SDL.SDL_CloseAudioDevice(device);

        SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);
    }
}
