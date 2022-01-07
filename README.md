# MeltySynth

MeltySynth is a SoundFont synthesizer written in C#.
The purpose of this project is to provide a MIDI music playback functionality for any .NET applications without suffering from complicated dependencies.
The codebase is lightweight and can be applied to any audio drivers which support streaming audio, such as [SFML.Net](https://github.com/SFML/SFML.Net), [Silk.NET](https://github.com/dotnet/Silk.NET), [OpenTK](https://github.com/opentk/opentk), and [NAudio](https://github.com/naudio/NAudio).

The entire code is heavily inspired by the following projects:

* [C# Synth](https://github.com/sinshu/CSharpSynthProject) by Alex Veltsistas
* [TinySoundFont](https://github.com/schellingb/TinySoundFont) by Bernhard Schelling

An example code to synthesize a simple chord:

```cs
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
```

Another example code to synthesize a MIDI file:
```cs
// Create the synthesizer.
var sampleRate = 44100;
var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

// Read the MIDI file.
var midiFile = new MidiFile("flourish.mid");
var sequencer = new MidiFileSequencer(synthesizer);
sequencer.Play(midiFile, false);

// The output buffer.
var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];
var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];

// Render the waveform.
sequencer.Render(left, right);
```

## Features

* No dependencies other than .NET Standard 2.1.
* No memory allocation in the rendering process.
* No unsafe code.


## Installation

[The NuGet package](https://www.nuget.org/packages/MeltySynth/) is available:

```ps1
Install-Package MeltySynth
```

If you don't like DLLs, copy [all the .cs files](https://github.com/sinshu/meltysynth/tree/main/MeltySynth/src) to your project.

## Demo

Here is a demo song generated with [Arachno SoundFont](http://www.arachnosoft.com/main/soundfont.php).

https://www.youtube.com/watch?v=xNgsIJKxPkI  

[![Demo video](https://img.youtube.com/vi/xNgsIJKxPkI/0.jpg)](https://www.youtube.com/watch?v=xNgsIJKxPkI)


## Examples

* [MIDI file player for SFML.Net](https://github.com/sinshu/meltysynth/tree/main/Examples/SFML.Net)
* [MIDI file player for Silk.NET (OpenAL)](https://github.com/sinshu/meltysynth/tree/main/Examples/Silk.NET.OpenAL)
* [MIDI file player for Silk.NET (SDL)](https://github.com/sinshu/meltysynth/tree/main/Examples/Silk.NET.SDL)
* [MIDI file player for OpenTK](https://github.com/sinshu/meltysynth/tree/main/Examples/OpenTK)
* [MIDI file player for SDL2#](https://github.com/sinshu/meltysynth/tree/main/Examples/SDL2)
* [MIDI file player for MonoGame](https://github.com/sinshu/meltysynth/tree/main/Examples/MonoGame)
* [MIDI file player for FNA.NET](https://github.com/sinshu/meltysynth/tree/main/Examples/FNA.NET)
* [MIDI file player for Raylib-cs](https://github.com/sinshu/meltysynth/tree/main/Examples/Raylib_cs)
* [MIDI file player for Raylib-CsLo](https://github.com/sinshu/meltysynth/tree/main/Examples/Raylib_CsLo)
* [MIDI file player for NAudio](https://github.com/sinshu/meltysynth/tree/main/Examples/NAudio)
* [MIDI file player for TinyAudio](https://github.com/sinshu/meltysynth/tree/main/Examples/TinyAudio)


## Todo

* __Wave synthesis__
    - [x] SoundFont reader
    - [x] Waveform generator
    - [x] Envelope generator
    - [x] Low-pass filter
    - [x] Vibrato LFO
    - [x] Modulation LFO
* __MIDI message processing__
    - [x] Note on/off
    - [x] Bank selection
    - [x] Modulation
    - [x] Volume control
    - [x] Pan
    - [x] Expression
    - [x] Hold pedal
    - [x] Program change
    - [x] Pitch bend
    - [x] Tuning
* __Effects__
    - [x] Reverb
    - [x] Chorus
* __Other things__
    - [x] Standard MIDI file support
    - [x] Loop extension support
    - [x] Performace optimization


## License

MeltySynth is available under [the MIT license](LICENSE.txt).


## References

* __SoundFont&reg; Technical Specification__  
http://www.synthfont.com/SFSPEC21.PDF

* __Polyphone Soundfont Editor__  
Some of the test cases were generated with Polyphone.  
https://www.polyphone-soundfonts.com/

* __Freeverb by Jezar at Dreampoint__  
The implementation of the reverb effect is based on Freeverb.  
https://music.columbia.edu/pipermail/music-dsp/2001-October/045433.html
