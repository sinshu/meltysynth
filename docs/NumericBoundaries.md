<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# Numeric asset and live-command boundaries

Implemented 2026-11-23 for outline item 11, .NET 10/C# 14 on macOS ARM64.

## Invariants and ownership

`NumericDomain` validates assigned generator values during region construction,
before a SoundFont constructor can return. It checks signed generator domains,
ordered 7-bit key/velocity ranges, tuning, scale, filter, envelope, send and pan
parameters. Preset values are relative offsets rather than absolute instrument
settings; negative preset pan/attenuation/filter offsets remain supported.
Unknown/reserved generators retain their ignored behavior; this is a supported
synthesis profile, not a claim to implement every SoundFont modulator or generator.

The existing sample/loop bounds checks remain. A bounded pass through resolved,
playable preset/instrument pairs establishes a bank-wide safe channel-pitch interval.
Pair visits are capped by `MaxSoundFontRegions`, independently of the existing zone
budget, so a small zone table cannot cause an unbounded Cartesian expansion.
Packaging's `AssetPreflight.Validate` constructs the same SoundFont; there is no
second numeric validator or unchecked ready/promotion path.

The pitch proof includes intersected key/velocity ranges, effective root/tuning,
sample pitch correction, scale tuning, both LFO depths, modulation-envelope depth
and the maximum half-semitone modulation-wheel vibrato. It covers every supported
output sample rate (16,000..192,000 Hz). It bounds Q24 oscillator increments to
1..2^62, reserving room for the largest possible sample position below 2^55;
loop wrapping or non-loop termination then occurs before the next sample access.
A one-cent conservative margin covers rounding in the float production expression.
Zero scale is checked directly; negative effective scales use reversed endpoints.
Default channel pitch must be representable or the asset is rejected.

Live message validation occurs before dispatch: channels 0..15, channel-command
nibbles 0x80..0xE0 and both data arguments 0..127, including the unused second
argument of one-data-byte messages. Unsupported pressure messages remain ignored.
Direct note APIs check their own public boundary. Internal prepared-file dispatch
uses the established byte invariant rather than rescanning events or duplicating
public byte validation. Stateful RPN/data-entry and wheel changes still validate
proposed effective tuning before assigning fields. Both tuned pitch and tuned-plus-
bend pitch must fit the prepared interval so resetting the wheel remains safe.
The interval is deliberately bank-wide, independent of current program/voices;
some legal MIDI combinations can be unsupported by extreme banks and throw before
changing state. No automatic clamping or fallback synth is used.

One owner still serializes commands and rendering. No per-sample/per-voice guard,
new sample-buffer copy, render lock or synthesis task was added. Oscillator DSP,
loop wrapping, frozen PCM tolerances and the clock implementation are unchanged.
Callback reentrancy is handled separately in [CallbackReentrancy.md](CallbackReentrancy.md).
Fractional clock drift is addressed in [SequencerClock.md](SequencerClock.md).
Public collection ownership is addressed in [PreparedAssetCollections.md](PreparedAssetCollections.md).

## Supported extrema and sentinels

Tests cover coarse tuning -120/+120, fine tuning -99/+99, pitch modulation
-12000/+12000 cents, pan/filter bounds, fixed-key/velocity/root values -1/0/127,
zero scale and a single-key 1200-scale region. Broad-key high-scale configurations
that cannot fit the oscillator are rejected despite individually legal generators.
Preset offsets are validated relatively, and combined legal modulation depths are
rejected when their effective pitch is unrepresentable.

Time generators retain the existing -32768 near-zero-time interpretation; no DSP
change to those envelope/LFO semantics is claimed. Sample original pitch 255,
root override -1, fixed key/velocity -1, loop mode 2's no-loop interpretation,
null RPN 127/127 and NoteOn velocity zero remain supported. MIDI RPN and pitch-wheel
0/127 extrema render finite output when representable by the loaded bank.

## Verification

```sh
python3 tools/regression_baseline.py --output-dir bin/validation/numeric-boundaries/regression-final
python3 tools/audio_audit.py --font /path/to/neon-mile.sf2 --midi /path/to/neon-mile.mid --output-dir bin/validation/numeric-boundaries/audio
python3 tools/webgpu_audio_probe.py --webgpu-root /path/to/KilnGod.BlazorWebGPU --sdl /path/to/libSDL3.dylib
```

The isolated original-source reproduction in
`bin/validation/numeric-boundaries/original-reproduction` reproduces both
`IndexOutOfRangeException`s from coarse tune 32767 and live data-entry 32831.
It records the original commit and uses a 60-second subprocess limit with a
512-frame render. The versioned regressions require load-time `InvalidDataException`
or command-boundary `ArgumentOutOfRangeException` instead, compare subsequent
playback after rejection, and verify preflight publishes no ready receipt.

Final Release runs: **436 self-contained passes, 48 unavailable external-bank
comparisons, zero failures per lane**, project-reference and source integration;
484 discovered each. Existing synthesis checkpoints/metrics remain at 1e-6 and
all prior exact comparisons remain exact. The discovery floor is raised to 436;
reference corpus and synthesis golden data are unchanged.

The ordinary scene score loops with effects off/on in the offline plain/profiled
audio audit, with zero measured render allocations. Its PCM hashes exactly match
the earlier `bin/validation/audio-cost` and `audio-cost-tiered` captures:

- effects off: `07EBA26E2DA74BC68CF11BFB121DE61FD29321513248C9C83829892AE518BD18`;
- effects on: `C8923A4F1BC1A807FDBDDCFDFDB4ABE83ACD132AA33A94579B72B083C96A68DE`.

The audit includes saturated polyphony and recorded isolated deadline outliers;
these captures establish correctness/ownership, not a new performance improvement
or scheduling guarantee. Raw audio/timing/TRX data remains in ignored `bin/`.

The actual WebGPU SDL3/CoreAudio adapter also passed: 1,490 mixed/queued blocks,
one score wrap, all five cue kinds, independent music/effects volume checks,
finite distinct stereo, feed thread joined and repeated open/close. Peak queue was
12,280 bytes within the existing three-block bound. This observes mixed/queued
signals, not an acoustic recording, latency guarantee or full game UI acceptance.
The preserved step-11 receipt is `bin/validation/numeric-boundaries/acceptance.json`;
the actual-adapter tool also writes its usual modernization-closeout output, whose
prior capture was preserved before rerunning.
