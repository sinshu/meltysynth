# MeltySynth

This is an attempt to implement a SoundFont synthesizer in C#.

Currently, some basic operations (note on/off, volume control, program change, and so on) are supported.

The entire code is heavily inspired by the following projects:

* [C# Synth](https://archive.codeplex.com/?p=csharpsynthproject) by Alex Veltsistas
* [TinySoundFont](https://github.com/schellingb/TinySoundFont) by Bernhard Schelling


## Demo

Here is a sample audio of "At Doom's Gate" generated with [TimGM6mb](https://musescore.org/en/handbook/soundfonts-and-sfz-files#gm_soundfonts).

https://www.youtube.com/watch?v=z8eNM-U1e0k  

[![Demo video](https://img.youtube.com/vi/z8eNM-U1e0k/0.jpg)](https://www.youtube.com/watch?v=z8eNM-U1e0k)


## Todo

* __Wave synthesis__
    - [x] SoundFont reader
    - [x] Wave generator
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
    - [ ] Hold pedal
    - [x] Program change
    - [ ] Pitch bend
* __Effects__
    - [ ] Chorus
    - [ ] Reverb
* __Other things__
    - [ ] SMF support
    - [ ] Performace optimization


## License

MeltySynth is available under the [MIT license](LICENSE.txt).
