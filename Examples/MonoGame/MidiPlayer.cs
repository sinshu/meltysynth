using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private static readonly int sampleRate = 44100;
    private static readonly int bufferLength = sampleRate / 10;

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private byte[] buffer;
    private DynamicSoundEffectInstance? dynamicSound;

    public MidiPlayer(string soundFontPath)
    {
        synthesizer = new Synthesizer(soundFontPath, sampleRate);
        sequencer = new MidiFileSequencer(synthesizer);

        dynamicSound = new DynamicSoundEffectInstance(sampleRate, AudioChannels.Stereo);
        buffer = new byte[4 * bufferLength];

        dynamicSound.BufferNeeded += (s, e) => SubmitBuffer();
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        if (dynamicSound == null)
        {
            throw new ObjectDisposedException(nameof(MidiPlayer));
        }

        sequencer.Play(midiFile, loop);

        if (dynamicSound.State != SoundState.Playing)
        {
            SubmitBuffer();
            dynamicSound.Play();
        }
    }

    public void Stop()
    {
        if (dynamicSound == null)
        {
            throw new ObjectDisposedException(nameof(MidiPlayer));
        }

        sequencer.Stop();
    }

    private void SubmitBuffer()
    {
        sequencer.RenderInterleavedInt16(MemoryMarshal.Cast<byte, short>(buffer));
        dynamicSound!.SubmitBuffer(buffer, 0, buffer.Length);
    }

    public void Dispose()
    {
        if (dynamicSound != null)
        {
            dynamicSound.Dispose();
            dynamicSound = null;
        }
    }

    public SoundState State
    {
        get
        {
            if (dynamicSound == null)
            {
                throw new ObjectDisposedException(nameof(MidiPlayer));
            }

            return dynamicSound.State;
        }
    }
}
