# MeltySynth

MeltySynth is a SoundFont synthesizer written in C#.

The entire code is heavily inspired by the following projects:

* [C# Synth](https://archive.codeplex.com/?p=csharpsynthproject) by Alex Veltsistas
* [TinySoundFont](https://github.com/schellingb/TinySoundFont) by Bernhard Schelling

An example code to synthesize a simple chord:

```cs
// Create the synthesizer.
var soundFont = new SoundFont("TimGM6mb.sf2");
var sampleRate = 44100;
var synthesizer = new Synthesizer(soundFont, sampleRate);

// The output buffer (2 seconds).
var buffer = new float[2 * sampleRate];

// Play some notes (middle C, E, G).
synthesizer.NoteOn(0, 60, 100);
synthesizer.NoteOn(0, 64, 100);
synthesizer.NoteOn(0, 67, 100);

// Render the waveform.
synthesizer.RenderMono(buffer);
```

## Demo

Here is a sample audio of "At Doom's Gate" generated with [TimGM6mb](https://musescore.org/en/handbook/soundfonts-and-sfz-files#gm_soundfonts).

https://www.youtube.com/watch?v=Wl-S0cOeIMY  

[![Demo video](https://img.youtube.com/vi/Wl-S0cOeIMY/0.jpg)](https://www.youtube.com/watch?v=Wl-S0cOeIMY)


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
    - [ ] Chorus
    - [ ] Reverb
* __Other things__
    - [x] SMF support
    - [ ] Performace optimization


## License

MeltySynth is available under the [MIT license](LICENSE.txt).
