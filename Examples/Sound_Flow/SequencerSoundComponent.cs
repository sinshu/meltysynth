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