## SoundFlow

This is an example implementation of a MIDI player backed by SoundFlow.

Sequencer SoundComponent:
```cs
using MeltySynth;
using SoundFlow.Abstracts;

namespace Sound_Flow;

public class SequencerSoundComponent : SoundComponent
{
    private readonly MidiFileSequencer _sequencer;

    public SequencerSoundComponent(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);
        var synthesizer = new Synthesizer(soundFontPath, settings);
        _sequencer = new MidiFileSequencer(synthesizer);
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        _sequencer.Play(midiFile, loop);
    }
    
    protected override void GenerateAudio(Span<float> buffer)
    {
        _sequencer.RenderInterleaved(buffer);
    }
}
```

Usage:
```cs
using MeltySynth;
using Sound_Flow;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;

using var audioEngine = new MiniAudioEngine(44100, Capability.Playback);

var sequencerSoundComponent = new SequencerSoundComponent("TimGM6mb.sf2");
var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

Mixer.Master.AddComponent(sequencerSoundComponent);

sequencerSoundComponent.Play(midiFile, true);

Console.WriteLine("Playing audio... Press any key to stop.");
Console.ReadKey();
```
