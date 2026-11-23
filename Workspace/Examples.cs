using System;
using System.Linq;
using System.IO;
using NAudio.Wave;
using MeltySynth;

public static class Examples
{
    public static void RunAll(ToolPaths paths)
    {
        SimpleChord(paths);
        Flourish(paths);
        ChangePlaybackSpeed(paths);
        MuteDrumTrack(paths);
        OverrideInstrument(paths);
    }

    public static void SimpleChord(ToolPaths paths)
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer(paths.Asset(paths.SoundFontName), sampleRate);

        // Play some notes (middle C, E, G).
        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOn(0, 64, 100);
        synthesizer.NoteOn(0, 67, 100);

        // The output buffer (3 seconds).
        var left = new float[3 * sampleRate];
        var right = new float[3 * sampleRate];

        // Render the waveform.
        synthesizer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, paths.Output("SimpleChord.wav"));
    }

    public static void Flourish(ToolPaths paths)
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer(paths.Asset(paths.SoundFontName), sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(paths.Asset(paths.MidiName));
        var sequencer = new MidiFileSequencer(synthesizer);
        sequencer.Play(midiFile, false);

        // The output buffer.
        var left = new float[(int)(sampleRate * BoundedDuration(midiFile))];
        var right = new float[(int)(sampleRate * BoundedDuration(midiFile))];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, paths.Output("Flourish.wav"));
    }

    public static void ChangePlaybackSpeed(ToolPaths paths)
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer(paths.Asset(paths.SoundFontName), sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(paths.Asset(paths.MidiName));
        var sequencer = new MidiFileSequencer(synthesizer);

        // Play the MIDI file.
        sequencer.Play(midiFile, false);

        // Change the playback speed.
        sequencer.Speed = 1.5F;

        // The output buffer.
        var left = new float[(int)(sampleRate * BoundedDuration(midiFile) / sequencer.Speed)];
        var right = new float[(int)(sampleRate * BoundedDuration(midiFile) / sequencer.Speed)];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, paths.Output("ChangePlaybackSpeed.wav"));
    }

    public static void MuteDrumTrack(ToolPaths paths)
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer(paths.Asset(paths.SoundFontName), sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(paths.Asset(paths.MidiName));
        var sequencer = new MidiFileSequencer(synthesizer);

        // Discard MIDI messages if its channel is the percussion channel.
        sequencer.OnSendMessage = (synthesizer, channel, command, data1, data2) =>
        {
            if (channel == 9)
            {
                return;
            }

            synthesizer.ProcessMidiMessage(channel, command, data1, data2);
        };

        // Play the MIDI file.
        sequencer.Play(midiFile, false);

        // The output buffer.
        var left = new float[(int)(sampleRate * BoundedDuration(midiFile))];
        var right = new float[(int)(sampleRate * BoundedDuration(midiFile))];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, paths.Output("MuteDrumTrack.wav"));
    }

    public static void OverrideInstrument(ToolPaths paths)
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer(paths.Asset(paths.SoundFontName), sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(paths.Asset(paths.MidiName));
        var sequencer = new MidiFileSequencer(synthesizer);

        // Turn all the instruments into electric guitars.
        sequencer.OnSendMessage = (synthesizer, channel, command, data1, data2) =>
        {
            if (command == 0xC0)
            {
                data1 = 30;
            }

            synthesizer.ProcessMidiMessage(channel, command, data1, data2);
        };

        // Play the MIDI file.
        sequencer.Play(midiFile, false);

        // The output buffer.
        var left = new float[(int)(sampleRate * BoundedDuration(midiFile))];
        var right = new float[(int)(sampleRate * BoundedDuration(midiFile))];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, paths.Output("OverrideInstrument.wav"));
    }

    private static double BoundedDuration(MidiFile file)
    {
        if (file.Length.TotalSeconds > 300)
            throw new InvalidDataException("Offline examples accept at most 300 seconds of MIDI; use bounded streaming for longer assets.");
        return file.Length.TotalSeconds;
    }

    internal static void WriteWaveFile(float[] left, float[] right, int sampleRate, string path)
    {
        var leftMax = left.Length == 0 ? 0 : left.Max(x => Math.Abs(x));
        var rightMax = right.Length == 0 ? 0 : right.Max(x => Math.Abs(x));
        var peak = Math.Max(leftMax, rightMax);
        var a = peak > 0 ? 0.99F / peak : 1F;

        var format = new WaveFormat(sampleRate, 16, 2);
        using (var writer = new WaveFileWriter(path, format))
        {
            for (var t = 0; t < left.Length; t++)
            {
                writer.WriteSample(a * left[t]);
                writer.WriteSample(a * right[t]);
            }
        }
    }
}
