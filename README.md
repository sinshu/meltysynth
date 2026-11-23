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

* Suitable for both real-time and offline synthesis.
* Support for standard MIDI files with additional functionalities like dynamic tempo change.
* No dependencies other than .NET Standard 2.1.



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

> [!CAUTION]
> Note that MeltySynth does not provide thread safety. If you want to send notes and render the waveform in separate threads, you must ensure that the related methods will not be called simultaneously.



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
* [MIDI file player for SoundFlow](https://github.com/sinshu/meltysynth/tree/main/Examples/SoundFlow)
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

### Sample-loop validation

SoundFont loading rejects empty or reversed sample extents. Active sample loops
must have positive length inside the generator-adjusted sample range; unused loop
markers on non-looping regions need not define a positive loop. Playback wraps
multi-loop pitch advances and interpolates across the loop seam. Loop-until-note-off
playback normalizes its pending phase before continuing into the sample tail.

The self-contained oscillator tests compare PCM against an independent phase and
interpolation reference, including MIDI note 127 on a 32-sample loop:

```sh
dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release --filter "FullyQualifiedName~OscillatorLoopRegressionTest|FullyQualifiedName~SampleLoopValidationTest|FullyQualifiedName~MidiFileSequencerLoopTest"
```

### SoundFont sample loading

On seekable streams, PCM loading fills its single destination buffer across short
reads and throws `EndOfStreamException` on premature EOF. Sample chunk payloads
and RIFF padding must fit inside `sdta`, and `sdta` must fit inside the declared
RIFF range and available stream. Empty, odd-length, oversized and duplicate `smpl`
chunks are rejected with `InvalidDataException` before promotion. Ignored `sm24`
data is skipped within validated bounds, including its padding.

`SoundFont(Stream)` leaves the caller's stream open on success and failure;
`SoundFont(string)` owns and closes its file. Application-specific budgets and the remaining metadata/MIDI boundaries are described below.

### Asset packaging and loading budgets

Validate game assets thoroughly before packaging. Use the same loading policy in
packaging tools so an asset accepted there also fits its target runtime profile.
Loading retains one pass of structural range/budget checks to catch truncation,
corruption or mismatched cached files. These checks run at asset load, never per
sample or per audio render. No extra whole-file copy or validation stream is added.

Existing constructors use immutable `AssetLoadLimits.Default`. Every file/stream
constructor also has an overload with a final `AssetLoadLimits` argument:

```cs
var limits = new AssetLoadLimits(
    maxFileBytes: 64L * 1024 * 1024,
    maxDecodedSampleBytes: 32 * 1024 * 1024,
    maxMidiEvents: 100000);
var soundFont = new SoundFont(soundFontStream, limits);
var midi = new MidiFile(midiStream, MidiFileLoopType.None, limits);
```

| Budget | Default |
| --- | --- |
| Remaining asset stream bytes | 512 MiB |
| Decoded 16-bit PCM bytes | 256 MiB |
| One MIDI track | 16 MiB |
| MIDI tracks | 256 |
| Parsed MIDI events across tracks, including ignored events | 1,000,000 |
| Aggregate SoundFont table records, including terminators/modulators | 262,144 |
| Aggregate referenced instrument/preset zones, including global zones | 65,536 |
| SoundFont INFO payload after its four-byte type | 1 MiB |
| MIDI playback duration | 24 hours |

Budgets must be positive; choose a profile that fits the game's packaged content.
They bound source/decoded bytes and element counts, not exact total managed heap
usage (objects and temporary/final event arrays also consume memory). Streams must
be readable and seekable. Stream constructors leave the caller's stream open;
path constructors own their file. Byte budgets apply from the current position to
the stream's end, so supply one bounded asset stream per packaged entry.

MIDI event fields, variable-length quantities and skips must stay inside their
`MTrk`; reaching the boundary without EOT is invalid. Positive PPQN/tempo, channel
data/status, supported loop options and tick/duration ranges are checked. Running
status applies to channel events and is cleared by meta/SysEx events. SMPTE timing
and format 2 remain unsupported. Empty tracks require a valid EOT; zero-track
headers are rejected. A zero-duration track cannot loop. Non-progressing loop
markers may load for non-looped playback but are rejected by `Play(..., true)`.

The default policy requires **EOT at the exact declared track end**. No events or
opaque bytes after EOT are accepted. An explicit `allowMidiEndOfTrackPadding: true`
allows only zero-byte padding inside that track; it still rejects trailing events
and never borrows bytes from the next track. Bytes after all declared MIDI tracks
are rejected. SoundFont bytes beyond the declared RIFF are also rejected.
This is a strict asset contract.

SoundFont INFO/pdta child lengths, padding, record alignment and duplicate chunks
are validated before field allocation or skipping. Zone/generator references and
aggregate region expansion are bounded before region construction. Structural or
budget violations throw `InvalidDataException`; physical sample EOF throws
`EndOfStreamException`. Unsupported formats throw `NotSupportedException`.

### .NET 10 / C# 14 source integration

The compatibility configuration compiles the actual library sources into the test
assembly with `net10.0`, explicit `LangVersion=14.0` and nullable checking. It uses
no library project reference and does not change the packaged library target.
Run with a .NET 10 SDK:

```sh
dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release -p:SourceIntegration=true --filter "FullyQualifiedName~AssetParsingLimitsTest|FullyQualifiedName~ArrayMathTest|FullyQualifiedName~SoundFontSampleLoadingTest|FullyQualifiedName~OscillatorLoopRegressionTest|FullyQualifiedName~SampleLoopValidationTest|FullyQualifiedName~MidiFileSequencerLoopTest"
```

These self-contained cases cover parser budgets and malformed chunks, SIMD writes,
exact loaded PCM, sample-loop validation and MIDI-loop playback without external reference banks.
Omit `-p:SourceIntegration=true` to test the existing project-reference path.
The two write boundaries use explicit mutable spans to avoid C# 14 selecting
read-only array-to-span overloads. `.AsSpan()` creates a view without copying data.
Existing nullable warnings and the full framework/sample upgrade remain separate
work; the upstream tests requiring external SoundFonts are not included here.

### Handling MIDI file sequencer

Loop playback requires an effective loop with positive duration. `Play(midiFile,
true)` throws `ArgumentException` before changing the current playback if the first
reachable loop end, or the end of the sequence, does not advance beyond its loop
start. Loop validity is prepared when the MIDI file is loaded; it adds no per-block
validation. With looping disabled, loop markers remain ignored.

The self-contained loop regression fixture uses original in-memory MIDI/SoundFont
data and does not require the external reference SoundFonts:

```sh
dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release --filter "FullyQualifiedName~MidiFileSequencerLoopTest"
```


To change the playback speed:

```cs
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
```

To mute a certain track:

```cs
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
```

To change the instruments used in the MIDI file:

```cs
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



## Fork modernization scope

The synthesis core and its tests/tooling are the .NET 10/C# 14 modernization
focus. All sixteen standalone examples have received a basic .NET 10/C# 14
retarget, with their backend dependencies unchanged. They remain reference-only
for WebGPU integration; their builds do not establish live playback support or
exercise the local fork while they reference NuGet MeltySynth 2.4.1.
See the [project, dependency and asset inventory](docs/ModernizationInventory.md)
for the frozen scope, build results, ownership review and reproducible receipts.

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
https://github.com/sinshu/freeverb
