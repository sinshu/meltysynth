# MeltySynth

MeltySynth is a SoundFont synthesizer written in C#.
The purpose of this project is to provide a MIDI music playback functionality for any .NET applications without complicated dependencies.
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

All the classes are in the `MeltySynth` namespace:

```cs
using MeltySynth;
```

If you don't like DLLs, copy [all the .cs files](https://github.com/sinshu/meltysynth/tree/main/MeltySynth/src) to your project.



## Playing sound

MeltySynth can only generate PCM waveforms; MeltySynth itself does not have the ability to play sound from speakers. To make the sound audible, export the generated waveform as an audio file (e.g., WAV file) or pass it to some audio driver (e.g., NAudio). If you are not very familiar with how to handle PCM audio, [NAudio's tutorials](https://github.com/naudio/NAudio#tutorials) should be helpful.

Note that MeltySynth does not provide thread safety. If you want to send notes and render the waveform in separate threads, you must ensure that the related methods will not be called simultaneously.



## Demo

### A demo song generated with [Arachno SoundFont](http://www.arachnosoft.com/main/soundfont.php)

https://www.youtube.com/watch?v=xNgsIJKxPkI

[![Youtube video](https://img.youtube.com/vi/xNgsIJKxPkI/0.jpg)](https://www.youtube.com/watch?v=xNgsIJKxPkI)

### [A Doom port written in C#](https://github.com/sinshu/managed-doom) with MIDI music playback

https://www.youtube.com/watch?v=_j1izHgIT4U

[![Youtube video](https://img.youtube.com/vi/_j1izHgIT4U/0.jpg)](https://www.youtube.com/watch?v=_j1izHgIT4U)

### A virtual keyboard made with [Raylib-CsLo](https://github.com/NotNotTech/Raylib-CsLo)

https://www.youtube.com/watch?v=a8vuIq4JKhs

[![Youtube video](https://img.youtube.com/vi/a8vuIq4JKhs/0.jpg)](https://www.youtube.com/watch?v=a8vuIq4JKhs)

### Use MeltySynth as a MIDI device ([NAudio](https://github.com/naudio/NAudio) + [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html))

https://www.youtube.com/watch?v=BiFxvzs0jUI

[![Youtube video](https://img.youtube.com/vi/BiFxvzs0jUI/0.jpg)](https://www.youtube.com/watch?v=BiFxvzs0jUI)

### Use MeltySynth as a VST plugin ([VST.NET](https://github.com/obiwanjacobi/vst.net))

https://www.youtube.com/watch?v=IUKIEWvw6Ik

[![Youtube video](https://img.youtube.com/vi/IUKIEWvw6Ik/0.jpg)](https://www.youtube.com/watch?v=IUKIEWvw6Ik)



## Examples

### MIDI file player for various audio drivers

* [MIDI file player for SFML.Net](https://github.com/sinshu/meltysynth/tree/main/Examples/SFML.Net)
* [MIDI file player for Silk.NET (OpenAL)](https://github.com/sinshu/meltysynth/tree/main/Examples/Silk.NET.OpenAL)
* [MIDI file player for Silk.NET (SDL)](https://github.com/sinshu/meltysynth/tree/main/Examples/Silk.NET.SDL)
* [MIDI file player for OpenTK](https://github.com/sinshu/meltysynth/tree/main/Examples/OpenTK)
* [MIDI file player for SDL2#](https://github.com/sinshu/meltysynth/tree/main/Examples/SDL2)
* [MIDI file player for Sokol_csharp](https://github.com/sinshu/meltysynth/tree/main/Examples/Sokol)
* [MIDI file player for MonoGame](https://github.com/sinshu/meltysynth/tree/main/Examples/MonoGame)
* [MIDI file player for FNA.NET](https://github.com/sinshu/meltysynth/tree/main/Examples/FNA.NET)
* [MIDI file player for Raylib-cs](https://github.com/sinshu/meltysynth/tree/main/Examples/Raylib_cs)
* [MIDI file player for Raylib-CsLo](https://github.com/sinshu/meltysynth/tree/main/Examples/Raylib_CsLo)
* [MIDI file player for DotFeather](https://github.com/sinshu/meltysynth/tree/main/Examples/DotFeather)
* [MIDI file player for NAudio](https://github.com/sinshu/meltysynth/tree/main/Examples/NAudio)
* [MIDI file player for CSCore](https://github.com/sinshu/meltysynth/tree/main/Examples/CSCore)
* [MIDI file player for TinyAudio](https://github.com/sinshu/meltysynth/tree/main/Examples/TinyAudio)
* [MIDI file player for DrippyAL](https://github.com/sinshu/meltysynth/tree/main/Examples/DrippyAL)

### Handling SoundFont

To enumerate samples in the SoundFont:

```cs
var soundFont = new SoundFont("TimGM6mb.sf2");

foreach (var sample in soundFont.SampleHeaders)
{
    Console.WriteLine(sample.Name);
}
```

To enumerate instruments in the SoundFont:

```cs
var soundFont = new SoundFont("TimGM6mb.sf2");

foreach (var instrument in soundFont.Instruments)
{
    Console.WriteLine(instrument.Name);
}
```

To enumerate presets in the SoundFont:

```cs
var soundFont = new SoundFont("TimGM6mb.sf2");

foreach (var preset in soundFont.Presets)
{
    var bankNumber = preset.BankNumber.ToString("000");
    var patchNumber = preset.PatchNumber.ToString("000");

    Console.WriteLine($"{bankNumber}:{patchNumber} {preset.Name}");
}
```

### Handling synthesizer

To change the instrument to play, send a [program change command](https://en.wikipedia.org/wiki/General_MIDI#Program_change_events) (0xC0) to the synthesizer:

```cs
// Create the synthesizer.
var sampleRate = 44100;
var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

// Change the instrument to electric guitar (#30).
synthesizer.ProcessMidiMessage(0, 0xC0, 30, 0);

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

To play a melody, render the sound as a sequence of short blocks:

```cs
// Create the synthesizer.
var sampleRate = 44100;
var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

// The length of a block is 0.1 sec.
var blockSize = sampleRate / 10;

// The entire output is 3 sec.
var blockCount = 30;

// Define the melody.
// A single row indicates the start timing, end timing, and pitch.
var data = new int[][]
{
    new int[] {  5, 10, 60 },
    new int[] { 10, 15, 64 },
    new int[] { 15, 25, 67 }
};

// The output buffer.
var left = new float[blockSize * blockCount];
var right = new float[blockSize * blockCount];

for (var t = 0; t < blockCount; t++)
{
    // Process the melody.
    foreach (var row in data)
    {
        if (t == row[0]) synthesizer.NoteOn(0, row[2], 100);
        if (t == row[1]) synthesizer.NoteOff(0, row[2]);
    }

    // Render the block.
    var blockLeft = left.AsSpan(blockSize * t, blockSize);
    var blockRight = right.AsSpan(blockSize * t, blockSize);
    synthesizer.Render(blockLeft, blockRight);
}
```



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
