<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# Fractional sequencer clock

Implemented for outline item 13 on 2026-11-23, .NET 10/C# 14, macOS ARM64.

## Exact accumulation without sample-loop work

Every supported positive binary32 speed, from 1/128 through 16 inclusive, is an
integer multiple of 2^-30. Zero is also representable. The sequencer therefore
uses the fixed denominator `SampleRate * 2^30` and converts the accepted speed to
its exact integer units. Speed is interpreted as the actual binary32 value, not
an ideal decimal spelling: e.g. `0.1F` is not exactly the rational 1/10.

Only construction/speed changes compute the block numerator, quotient and remainder:

```
numerator   = speedUnits * BlockSize * 10,000,000
denominator = SampleRate * 2^30
```

The setup product can exceed UInt64 at maximum block/speed, so setup uses UInt128.
The per-block path uses only cached whole ticks, UInt64 remainder addition, one
comparison/carry/subtraction and a TimeSpan tick addition. No allocation, division,
floating accumulation, sample-buffer copy or sample-loop guard was added. Setting
the same speed does not recompute the cache. The largest denominator is below
2^48; adding two bounded remainders is safely below 2^49. The setup numerator is
below 2^68 and the largest whole-tick increment is 10,240,000.

Remainders share the fixed denominator across every supported speed. This avoids
losing fractions at control changes or retaining a remainder measured in the wrong
units. The observable TimeSpan is the floor in 100 ns ticks; its quantization error
is below one tick rather than growing with the number of blocks.

## Lifecycle and scheduling semantics

- The clock advances once when an audio block starts, after processing due events.
  `Position` reports that block's end, including when only part of it was consumed.
  Fragmented renders do not advance it again within the same block. Speed changes
  apply at the next newly started block, including a change made by its MIDI hook.
- Zero speed preserves the fraction and stops clock advancement. Voices/effects
  still render, and already-due events can still dispatch, as in the existing policy.
- `Play` resets whole time, remainder, event/loop cursors and partial-block state,
  and resets synthesis. Speed/hook configuration remains set. A rejected Play or
  speed command does not reset the clock or its fraction.
- `Stop` detaches the file and resets synthesis, retaining the rendering clock and
  partial-block accounting. Silent rendering after Stop continues that clock as
  before; a subsequent Play resets it. Direct `Synthesizer.Reset` does not own or
  reset sequencer time. These behaviors are tested rather than silently redesigned.
- Loop rewind still sets the whole-tick score position to the recorded loop start,
  retaining the sub-tick remainder. Existing whole-block overshoot/restart policy
  remains: explicit loop ends can replay immediately; end-of-sequence replay starts
  in the following block. This fix does not turn the sequencer into a sample-accurate
  scheduler or redesign whole-block loop overshoot. Fractional time is not discarded
  at each rewind, and note-release/order behavior matches the independent reference.
- A hook failure consumes its event attempt but does not advance the failed block's
  clock. Guard release, pending-event recovery and owner-side playback changes retain
  the item-12 contract in [CallbackReentrancy.md](CallbackReentrancy.md).

MIDI asset timestamp conversion/tempo merging in `MidiFile` is unchanged. The
sequencer no longer calls its truncating seconds converter for each rendered block.
No frozen MIDI timestamps, input fixtures or synthesis golden data were recaptured.

## Independent verification

The versioned `SequencerClockTest` oracle decodes IEEE-754 fields and accumulates
BigInteger rational frame time. It does not reuse production Q30 units, cached
increments, floating-point time summation or TimeSpan-per-block conversions.

For 100,000 blocks / 6,400,000 frames at speed one, with the score still active:

| Sample rate | Prior ticks | Exact whole-tick reference / corrected ticks | Prior lost time | Corrected tick difference |
| --- | ---: | ---: | ---: | ---: |
| 32,000 | 2,000,000,000 | 2,000,000,000 | 0 | 0 |
| 44,100 | 1,451,200,000 | 1,451,247,165 | 4.7165 ms | 0 |
| 48,000 | 1,333,300,000 | 1,333,333,333 | 3.3333 ms | 0 |

The pre-fix TRX records two expected failures and the 32 kHz control pass. All
**226 new cases** now pass: 192 rate/block/speed profiles, long runs, irregular
fragmentation and speed changes (including adjacent binary32 extrema), zero/resume,
Play/Stop/reset/rejected commands, callback changes/failures and all four loop modes.
The profiles span 16/22.05/32/44.1/48/88.2/96/192 kHz and blocks 8/64/127/1024.
Independent synthesis comparisons verify event order, exact-boundary note release
and stereo, including the 16-second 44.1 kHz and one-second 48 kHz boundaries that
were formerly dispatched one block late. Clock correctness is not a wall-clock
latency, hardware callback or real-time deadline guarantee.

```sh
./bin/dotnet/dotnet test MeltySynthTest -c Release --filter FullyQualifiedName~SequencerClockTest
python3 tools/regression_baseline.py --output-dir bin/validation/sequencer-clock/regression
python3 tools/audio_audit.py --font /path/to/neon-mile.sf2 --midi /path/to/neon-mile.mid --output-dir bin/validation/sequencer-clock/audio
```

Both complete warning-as-error Release lanes pass **688 self-contained tests,
48 unavailable external-bank comparisons and zero failures**, 736 discovered per
project-reference/source-integration lane. The discovery floor is 688. The original
32 kHz frozen PCM checks and exact/1e-6 assertions remain unchanged.

## Intentional 44.1 kHz scene-audio impact

Motel California still loops with finite stereo and effects off/on; measured music
render allocations are zero. Plain/profiled builds produce identical PCM. Its
44.1 kHz output is intentionally different from item 12 because accumulated clock
truncation had delayed note/control deliveries. Neither an audible tolerance nor a
frozen baseline was changed to accept that difference.

An isolated evidence-only timeline probe renders 20,000 blocks (29.024 seconds),
comparing the actual sequencer with a whole-frame rational scheduler and a model of
the former truncated clock. It supports only this flat normal/EOT scene score and
is not a second production dispatcher. In each effects mode, all **686 events**
retain identity/order; **370** deliveries move earlier by 64 or 128 frames (maximum
2.9025 ms after the loop). Actual and rational-reference event frames agree exactly.
Their first 4,096 blocks also have identical Int16 PCM hashes. The old-clock model
reproduces the actual prior audit hashes, establishing that this is timing drift
correction rather than a DSP/synthesis/asset change:

| Effects | Previous PCM SHA-256 | Corrected PCM SHA-256 |
| --- | --- | --- |
| Off | `07EBA26E2DA74BC68CF11BFB121DE61FD29321513248C9C83829892AE518BD18` | `4D140CEB9BFB5249E5629B0E4DD03284150947FE7376293E37E8AFCF01365C73` |
| On | `C8923A4F1BC1A807FDBDDCFDFDB4ABE83ACD132AA33A94579B72B083C96A68DE` | `B64DD804763AFFFD230F6D8B693F603209B3EC1DA629536AB59E4DDD86B00437` |

Raw TRX, timing/audio receipts, prior sequencer snapshot and timeline probe sources
remain in ignored `bin/validation/sequencer-clock/`. Checked-in original-fixture
regressions independently cover the scheduling fix without any game dependency.

## macOS ARM64 NativeAOT compatibility

`python3 tools/native_aot_smoke.py --output-dir bin/validation/sequencer-clock/native-aot`
passed all four fresh-extraction deliveries: project/runtime effects, project/fixed-
effects-off, source/fixed-effects-off and local NuGet/runtime effects. Managed/native
frozen 32 kHz samples agree exactly, compiler/trimming/AOT diagnostics are empty,
MIT/source identities are retained, and packages have no standalone DLLs/dylibs.
This exercises the new setup-time UInt128 code in NativeAOT; the full rate/speed
matrix is separately proven in the two managed compilation lanes. Runtime-disabled
effects still retain their code, as before; no new trimming claim is made.
The consolidated local receipt is `bin/validation/sequencer-clock/acceptance.json`.
