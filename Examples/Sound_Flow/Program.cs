// See https://aka.ms/new-console-template for more information

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