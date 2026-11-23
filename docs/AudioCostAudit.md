# Audio cost and ownership audit

Accepted 2026-11-23 on Apple M1 Pro (10 logical CPUs), macOS 27.2.1 ARM64,
SDK 10.0.404 / runtime 10.0.15. Core remains dependency-free; no DSP algorithm,
voice parallelism or new playback backend was introduced.

## Reproduce

```sh
python3 tools/audio_audit.py
# Optional: use original scene audio without referencing its game projects.
python3 tools/audio_audit.py --font /path/to/neon-mile.sf2 --midi /path/to/neon-mile.mid --output-dir bin/validation/audio-cost/ring-final
python3 tools/regression_baseline.py --output-dir bin/validation/audio-cost/regression
```

No external bank is required for the default audit. The default uses the existing
small original test SoundFont/MIDI builders, linked directly rather than copied.
The optional run above used the existing original Motel California assets.
The standalone source-inclusive audit has no NuGet dependencies and builds itself
in Release twice. Outputs, binaries, raw per-block CSVs, source/binary/asset hashes
and receipts stay in ignored `bin/`; normal intermediates remain in project `obj/`.

Plain timings are the performance result. An audit-only compile symbol enables
timestamps around the actual voice processing, dry mix, chorus and reverb paths.
The same canonical RenderBlock executes; no second DSP implementation is used.
Both executables inspect compiled metadata to prove hooks absent/present as intended,
and render exact matching PCM hashes. The normal library contains no counter field,
clock call or profiling branch. Profiled setup has an extra counter reference;
profiled stage timings include clock overhead and must not be subtracted from plain
timings to invent exclusive costs. A timer pair measured about 0.041 µs median.

Render distributions use five 4,000-block windows, each warmed with 1,024 blocks,
and fixed 64-frame
blocks at 44.1 kHz; nearest-rank p99, max and deadline misses use the raw samples.
Each render trial resets/restarts its fixture outside the measured interval.
Event/format/real-score measurements instead use one warmup and five consecutive
windows on their existing owner; their individual window medians remain in JSON.
Representative load holds 8 voices with a 64-voice pool; saturation holds all 256
preallocated voices. Effects-on workloads enable both sends. Live voice counts and
finite output are asserted. Stage allocation fields are null when not independently
measured; zero total render allocation establishes zero in its enclosed stages.

Tiered compilation is disabled **only in the steady-state audit child processes**.
This is not an application setting recommendation. `--runtime-tiering` retains
the runtime policy to observe startup separately. The retained runtime-default
capture showed a 10-to-5 µs trial transition and a first-case 128-byte allocation;
do not mix that startup behavior into a supposedly stable throughput comparison.

## Uninstrumented block cost

| Active voices | Effects | Median µs | p99 µs | Max µs | Managed bytes / 20,000 blocks | Deadline misses |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 8 | off | 4.834 | 5.209 | 52.875 | 0 | 0 |
| 8 | on | 11.250 | 12.167 | 83.166 | 0 | 0 |
| 256 | off | 155.584 | 183.916 | 281.375 | 0 | 0 |
| 256 | on | 170.958 | 200.291 | 299.833 | 0 | 0 |

Budget: **1,451.247 µs per block**. These are offline CPU elapsed times, not
physical-device latency guarantees. All final-capture render, note/control burst,
format conversion and real-music stages allocated zero managed bytes after warmup.
An earlier preserved capture observed a 5.295 ms maximum and three budget misses
in the 256-voice effects case. The final result does not prove those OS scheduling
stalls are impossible or that replacing queue bookkeeping changed DSP deadlines.

## Stage attribution and conversion

| Voices / effects | Voice synthesis µs | Dry mix µs | Chorus/send mix µs | Reverb/send mix µs |
| --- | ---: | ---: | ---: | ---: |
| 8 / off | 4.625 | 0.166 | 0.000 | 0.000 |
| 8 / on | 4.750 | 0.166 | 0.541 | 5.833 |
| 256 / off | 153.917 | 5.250 | 0.000 | 0.000 |
| 256 / on | 150.333 | 5.167 | 5.209 | 10.459 |

Instrumented medians above include stage clock overhead; effects include their
input/send preparation and output mixing. Voice processing includes oscillator,
envelopes/LFO and filtering, rather than claiming each inner operation was isolated.

Note/control bursts (stop/restart, send controls and note-on) are measured apart
from synthesis: eight voices **1.375–1.417 µs median / 1.500–1.583 µs p99**;
256 voices **43.000–44.333 µs median / 50.208–58.500 µs p99**. The sequencer also
runs a prepared looping MIDI fixture end to end; this is distinct from the burst
measurement, not a subtraction-based estimate of exclusive scheduler overhead.

Actual AudioRendererEx helpers measured **0.084/0.125 µs** float interleave and
**0.125/0.167 µs** Int16 interleave (median/p99). Their fixed-source renderer
copies planar data into the helpers’ real ArrayPool storage before conversion.
Those numbers include pooling, copying and conversion; they are not zero-copy
claims or a promise the pool can never allocate under memory pressure. The normal
planar Render copies 512 payload bytes per stereo block to caller-owned spans.

## Loading, assets and setup

File reads are separated from in-memory parse/PCM decode and Synthesizer setup.
They are single setup observations, include first-use/JIT where applicable, and
are explicitly not cold-storage percentiles. Neon Mile’s warm-cache observations:

| Phase | Elapsed µs | Allocated bytes |
| --- | ---: | ---: |
| SoundFont file read | 330.166 | 201192 |
| MIDI file read | 77.958 | 1640 |
| SoundFont parse/PCM decode | 322.500 | 208352 |
| MIDI parse/timing preparation | 199.167 | 26280 |

The original microfixture is 760 SF2 bytes + 34 MIDI bytes; retained decoded PCM
payload is 348 bytes and retained MIDI event/time payload 36 bytes. Neon Mile is
**200,876 SF2 bytes + 1,549 MIDI bytes**, with **200,048 decoded PCM bytes** and
**4,524 MIDI event/time payload bytes**. These exact tracked payload sizes exclude
object/array headers, presets/metadata, dictionaries and allocator overhead; they
are not total managed heap measurements. The audit retains its own input arrays
to hash them; runtime loaders do not retain those source byte arrays.

Uninstrumented synthesis setup allocated 63,664/611,968 bytes for the 64-voice
pool with effects off/on and 247,984/796,288 for the 256-voice pool. Those are
constructor allocation observations, not exact retained-heap sizes. Voices, voice
blocks, filters/envelopes/LFOs and optional delay/effect buffers remain preallocated.

## Actual Neon Mile playback path

| Effects | Median µs | p99 µs | Max µs | Allocated bytes | Misses |
| --- | ---: | ---: | ---: | ---: | ---: |
| off | 1.167 | 3.584 | 23.042 | 0 | 0 |
| on | 7.125 | 9.833 | 34.333 | 0 | 0 |

The real score reached six active voices and crossed one MIDI wrap per scenario.
Both builds match the same exact PCM hashes, not just energy thresholds. No sound
was sent to a device; this verifies the upgraded local core with those assets,
not that the game’s current NuGet consumer has adopted this fork.

## Bounded ownership and completion

Workspace’s integration adapter owns four fixed PCM slots (**1,024 Int16 payload
bytes** at 64 stereo frames). One producer thread alone owns the synthesizer and
the format helper; the consumer receives a read-only span until its callback returns.
No queue-to-queue PCM copy, per-voice task or concurrent synth call is introduced.
Full capacity backpressures the producer; publication is FIFO. Complete drains
ready work; callback failure returns its lease; callers cancel the peer and join
both workers before Dispose retires queued slots and clears every PCM reference.
Monitor pulses wake ordinary waits; a 20 ms cancellation polling interval bounds
the polling component of cancellation response, in addition to OS scheduling.

The first BlockingCollection-based adapter reused PCM but allocated **56,672 producer
bytes per 256 blocks** under backpressure. The accepted fixed ring measured **zero
producer bytes** in every scenario. Consumer allocation was **112 bytes in its
first scenario, zero in the other three**; this first-use observation remains
visible and is not attributed to a particular allocation stack.
No per-block cancellation registration or collection-node allocation remains.

The paced offline sink consumed all 256 blocks in about **371.6–374.0 ms**, with
peak ready depth **3 of 4** in the plain capture, all buffers returned and an exact
match to sequential PCM. Producer wait p99 was **1.302–1.473 ms**, expected bounded
backpressure rather than synthesis cost. Fill-to-consumer-completion p99 was
**5.621–5.796 ms**, including queued work and simulated pacing. Wait/sink timings
include their deliberate wait/spin clock; this is not device CPU load or acoustic
latency. Hardware queue depth, OS callback scheduling, underruns and audible
completion need separate native/browser host acceptance; no device driver is added.

Seven integration cases cover effects off/on exact FIFO audio, saturation and
cancellation, fill/sink failure, cancellation during consumption, endpoint ownership
and repeated completed/canceled shutdown. Both complete test lanes pass **343
self-contained / 48 external unavailable / 0 failed**, 391 discovered, warnings
as errors. Frozen synthesis values/tolerances remain unchanged.

## Evidence

- Accepted timing/source/asset/binary receipts and raw CSVs: `bin/validation/audio-cost/ring-final/`.
- Previous collection queue and its source snapshot: `bin/validation/audio-cost/steady-final/`.
- Runtime-default startup capture: `bin/validation/audio-cost-tiered/`.
- Full suite and root Release build: `bin/validation/audio-cost/regression/` and `solution-build.log`.
- Combined acceptance: `bin/validation/audio-cost/acceptance.json`.

This completes the library/tool audit on this Mac. NativeAOT, other CPU/OS targets
and actual SDL3/browser/Apple audio-device playback remain separate acceptance.


Subsequent closeout integration now directly references this project from the
WebGPU native host and verifies its actual SDL3 adapter. The earlier timing capture
above did not change that consumer. See
[ModernizationCloseout.md](ModernizationCloseout.md) for current dependency and
playback status; importing the source itself is deferred.
