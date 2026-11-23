# Reproducible regression and numerical baseline

Frozen 2026-11-23 against production commit
`2af12ffb4588a89ddf74fce8e1fde7d9da0c4b55`, SDK 10.0.203 / .NET 10.0.7 ARM64.
No production-library code, dependency versions, or target frameworks were changed
by this baseline work. That capture used net8.0 against netstandard2.1 and net10.0/C# 14 source inclusion.
The subsequent framework upgrade targets both configurations at net10.0/C# 14;
they retain distinct project-reference and source-inclusive compilation paths and
execute the same assertions. The original numerical golden data stays unchanged.

## Run from a fresh checkout

```sh
python3 tools/regression_baseline.py
```

Requires the normal test NuGets and the SDK pinned in `global.json` (10.0.404).
Run `python3 tools/install_dotnet_sdk.py` for a local SDK/runtime under `bin/dotnet`;
the runner prefers that host. No .NET 8 runtime is required by the upgraded suite.
No game project, downloaded SoundFont, audio device, native playback library or
secret workstation path is required. The runner resolves the repository from its
own location and runs both configurations sequentially because they share restore
state. It hashes the checked-in reference corpus before testing, captures production
source/baseline identities, rejects empty/insufficient discovery and unexpected
skips, and stores logs/TRX/summary under ignored `bin/validation/regression-baseline`.
It never rewrites a baseline to turn a failed assertion into a pass.

Direct invocations are also available:

```sh
# Entire suite, including individually reported unavailable external cases.
dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release
# Explicit C# 14 source integration; only self-contained cases.
dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release -p:SourceIntegration=true --filter 'TestCategory!=ExternalAssets'
```

Reference paths default to `ReferenceData` beside the test assembly, not the process
working directory. `MELTYSYNTH_REFERENCE_DATA` or runner `--references /path/to/data`
can supply a different root, which must match the frozen corpus identities. CSV/TSV
numbers use invariant culture. The original MSBuild percent-escaped filenames stay
unchanged; the build copies all 2,258 reference files. Reference text and baseline
JSON have explicit LF checkout rules so Windows autocrlf does not change hashes.

## Self-contained fixtures and independent checks

- `MidiLoopTestAssets` creates original in-memory SMF/SoundFont bytes. No downloaded
  bank is renamed to impersonate another bank. A complete INFO variant supplies
  word-aligned engine/name metadata for the independent NAudio reader; the minimal
  parser fixture remains unchanged for the existing length/budget reproductions.
- `TestSettings.SyntheticSoundFont` is explicitly the original test tone, not a
  silent substitute for TimGM or another named bank. Filter, LFO and envelope tests
  need a synth's timing configuration, not an external instrument collection.
- Independent NAudio parsing checks the original full SoundFont's sample headers,
  instruments, presets and exact PCM. NAudio.Core was already a test dependency;
  no playback stack or game dependency graph was added.
- Original MIDI has independently specified quarter-note tempo changes: note-on at
  exactly 5,000,000 TimeSpan ticks and note-off/EOT at 15,000,000. Loaded MIDI stays
  usable after disposing its source; the caller's stream remains open during load.
- The existing independent double-precision oscillator oracle tests Q24 step
  quantization, seam interpolation, one-/32-sample loops, high pitch and release.
  Findings 1–5's parser, boundary, cancellation-free termination and ownership
  assertions remain active rather than being reclassified as optional tests.
- Full stereo conversion destinations are checked, including the previously
  unchecked second half of the interleaved float and Int16 buffers. Existing
  saturation/mono/channel conversion tests remain numerical assertions.

## Frozen synthesis contract

[The synthesis baseline](../MeltySynthTest/Baselines/synthesis.json) records original
MIDI/SoundFont SHA-256, profile, left/right energy, left/right peak and ten stereo
checkpoints per scenario. Profile: 32 kHz, block 64, maximum polyphony 8, master
volume 0.5, 32,768 frames; chorus/reverb disabled and enabled are separate scenarios.
The original score pans two notes and repeats its 0.1-second loop. Expected energy
is not just compared with itself: frozen values/checkpoints guard subsequent
framework/compiler changes; independent oscillator/reader/tempo checks provide
separate oracles. These values freeze the accepted implementation's sound, not a
claim of acoustic equivalence to another synthesizer.

| Contract | Acceptance |
| --- | --- |
| PCM loading / Int16 conversion / fixture identity | Exact equality; no tolerance |
| Fresh silence and Reset, with effects off/on | Every output sample exactly zero, including partial blocks |
| Float stereo/mono conversion | Absolute error at most 1e-6 |
| Oscillator independent phase/interpolation | Absolute error at most 1e-6 |
| One large render versus 1/63/65/257/511-frame slices | Both complete channels within 1e-6 across repeated MIDI loops |
| Frozen synthesis checkpoints | Absolute error at most 1e-6; all output finite and nonzero energy |
| Frozen energy/peak metrics | Error at most max(1e-6, abs(expected) × 1e-6) |
| MIDI loop/control order | Exact existing event/ordering assertions; no hanging zero-duration loop |
| Original tempo-track times | Exact TimeSpan ticks |
| Existing external MIDI durations | Existing 0.1-second tolerance, only if the real files are provided |
| Existing ArrayMath | Existing 1e-3 numerical threshold, unchanged |
| Existing Freeverb filters / full impulse | Existing strict/absolute 1e-3 thresholds, unchanged |
| Existing TinySoundFont LFO | Existing strict 0.025 threshold, unchanged |
| Existing volume/modulation envelopes | Existing strict 0.02 threshold, unchanged |
| Existing biquad/math/Polyphone comparisons | Their existing frequency-/parameter-specific thresholds remain in source; none widened |

Stream ownership remains asserted on successful/failed MIDI and SoundFont loads,
including short reads, zero-read premature EOF and source disposal after preparation.
Non-looping zero-padded tails, loop-until-note-off continuation and repeated release
retain the dedicated independent-reference tests. No threshold was relaxed to
obtain this acceptance. A future baseline or tolerance update needs an explained
behavior/precision decision and review, not automatic recapture.

## Optional external comparisons

External banks remain genuinely optional **external comparisons**, not missing
prerequisites for DSP/core coverage. The named-bank TestCaseSources enumerate cases
without decoding banks during discovery; each available bank is lazily prepared at
most once. Missing cases report an explicit unavailable reason, not a pass or an
invalid static-initializer error. Supplied corrupt assets fail normally. Oscillator
bank cases must exercise a matching region rather than pass vacuously.

Set `MELTYSYNTH_TEST_ASSETS` or use `--assets /path/to/permitted-assets`. Required
names and the relevant comparisons are:

| Filename | Optional work |
| --- | --- |
| TimGM6mb.sf2 | NAudio/Polyphone/region comparisons and oscillator bank coverage |
| Arachno SoundFont - Version 1.0.sf2 | NAudio/Polyphone/region comparisons |
| GeneralUser GS MuseScore v1.442.sf2 | NAudio/Polyphone/region comparisons and oscillator bank coverage |
| SGM-V2.01.sf2 | NAudio/Polyphone/region comparisons |
| MuseScore_General.sf3 | External unsupported-format rejection |
| flourish.mid | Existing duration 87.5 seconds ±0.1 |
| onestop.mid | Existing duration 247.4 seconds ±0.1 |
| town.mid | Existing duration 79.0 seconds ±0.1 |

These files were not obtained or redistributed in this task. Supply only assets
you may use; the library's MIT notice does not establish their provenance. Use
[the manifest template](../MeltySynthTest/Baselines/external-assets.example.json)
to record the exact file SHA-256 and its provenance. The runner records observed
identities but does not certify rights. A complete **external baseline acceptance**
requires all files, matching pins/provenance and no unavailable/failed cases:

```sh
python3 tools/regression_baseline.py --assets /path/to/permitted-assets --external-manifest /path/to/approved-manifest.json --require-external
```

Without those inputs, the external baseline remains unavailable/unpinned even if
some comparisons can execute. The optional default `MeltySynthTest/ExternalAssets`
folder is ignored by Git to prevent accidental bank redistribution.

## Acceptance receipt and limits

Before this work the unfiltered run had **233 passed / 69 failed / 0 skipped**.
The failures were missing-bank static initialization and asset/path failures;
parameterized discovery errors also collapsed multiple bank cases into fewer
reported tests. Therefore pre-/post-change totals are not directly comparable.

After the changes, both normal net8 and explicit net10/C# 14 source integration
reported **295 self-contained passed / 48 external unavailable / 0 failed**
(**343 discovered** per mode). This includes 52 existing DSP comparisons previously
blocked by fixture/path coupling and ten new original/end-to-end baseline cases.
All 153 prior focused cases remain included. The reference corpus SHA-256 groups
are frozen in [fixtures.json](../MeltySynthTest/Baselines/fixtures.json). Raw results
and production source identity are in ignored
`bin/validation/regression-baseline/summary.json`.

Strict missing-external preflight was verified to return failure before running
tests, with `selfContainedAccepted=false`, instead of reporting an empty run as
acceptance. The separate probe receipt is under `missing-external-probe/`.
A direct net10/C# 14 run from the ignored receipt directory (outside the solution
root) also passed all 295 self-contained cases, zero skips/failures, using the
assembly-relative copied data. Its receipt is under `non-root/non-root.trx`.
No full external-bank acceptance, hardware playback, allocation/latency benchmark,
AOT platform acceptance or core framework retarget is claimed here. Existing
nullable warnings remain separate upgrade work.

## .NET 10 / C# 14 migration acceptance

Completed 2026-11-23 on macOS ARM64, pinned SDK **10.0.404**, runtime **10.0.15**.
Both full configurations report **299 passed / 48 external unavailable / 0 failed**
(**347 discovered** each). The four added preset-selection cases cover exact hits,
GM fallback, default fallback and missing percussion requests. Frozen fixture and
synthesis identities, sample tolerances, loop/silence and ownership assertions are
unchanged; the upgrade does not regenerate expected audio.

`Synthesizer.NoteOn` now infers the nullable out variable at the first dictionary
lookup. Successful lookups or assignment of the synthesizer-owned `defaultPreset`
prove a nonnull value before use. No null-forgiving operator, warning suppression,
additional dictionary lookup or allocation was added. The core builds with warnings
as errors. The root Release solution build has zero warnings/errors. Source-inclusive
nullable checking still reports six pre-existing external-bank helper warnings
(CS8604/CS8618); these are not core warnings or hidden migration failures.

The generated package contains only `lib/net10.0` assembly/documentation plus normal
NuGet metadata and the preserved MIT license, with no package dependencies or native
playback modules. Local package metadata/version are unchanged; no package was
published. Broader Workspace path cleanup and platform/AOT playback remain separate.
Receipts: `bin/validation/net10-upgrade/summary.json` and `regression/summary.json`.

```sh
./bin/dotnet/dotnet build MeltySynth/MeltySynth.csproj -c Release -warnaserror
python3 tools/regression_baseline.py --output-dir bin/validation/net10-upgrade/regression
```
