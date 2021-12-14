using System;
using Raylib_CsLo;
using MeltySynth;

class Program
{
    static int sampleRate = 44100;
    static int bufferSize = 4096;

    unsafe static void Main()
    {
        Raylib.InitWindow(800, 450, "MIDI Player");

        Raylib.InitAudioDevice();
        Raylib.SetAudioStreamBufferSizeDefault(bufferSize);

        var stream = Raylib.LoadAudioStream((uint)sampleRate, 16, 2);
        var buffer = new short[2 * bufferSize];

        Raylib.PlayAudioStream(stream);

        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);
        var sequencer = new MidiFileSequencer(synthesizer);
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

        sequencer.Play(midiFile, true);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsAudioStreamProcessed(stream))
            {
                sequencer.RenderInterleavedInt16(buffer);
                fixed (void* p = buffer)
                {
                    Raylib.UpdateAudioStream(stream, p, bufferSize);
                }
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.LIGHTGRAY);
            Raylib.DrawText("MUSIC SHOULD BE PLAYING!", 255, 200, 20, Raylib.DARKGRAY);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
