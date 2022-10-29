using System;
using System.Linq;
using NAudio.Wave;
using MeltySynth;

public static class Examples
{
    public static void RunAll()
    {
        SimpleChord();
        Flourish();
        ChangePlaybackSpeed();
        MuteDrumTrack();
        OverrideInstrument();
    }

    public static void SimpleChord()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Play some notes (middle C, E, G).
        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOn(0, 64, 100);
        synthesizer.NoteOn(0, 67, 100);

        // The output buffer (3 seconds).
        var left = new float[3 * sampleRate];
        var right = new float[3 * sampleRate];

        // Render the waveform.
        synthesizer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "SimpleChord.wav");
    }

    public static void Flourish()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        var sequencer = new MidiFileSequencer(synthesizer);
        sequencer.Play(midiFile, false);

        // The output buffer.
        var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];
        var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "Flourish.wav");
    }

    public static void ChangePlaybackSpeed()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        var sequencer = new MidiFileSequencer(synthesizer);

        // Play the MIDI file.
        sequencer.Play(midiFile, false);

        // Change the playback speed.
        sequencer.Speed = 1.5F;

        // The output buffer.
        var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds / sequencer.Speed)];
        var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds / sequencer.Speed)];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "ChangePlaybackSpeed.wav");
    }

    public static void MuteDrumTrack()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
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
        var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];
        var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "MuteDrumTrack.wav");
    }

    public static void OverrideInstrument()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
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
        var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];
        var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];

        // Render the waveform.
        sequencer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "OverrideInstrument.wav");
    }

    private static void WriteWaveFile(float[] left, float[] right, int sampleRate, string path)
    {
        var leftMax = left.Max(x => Math.Abs(x));
        var rightMax = right.Max(x => Math.Abs(x));
        var a = 0.99F / Math.Max(leftMax, rightMax);

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
