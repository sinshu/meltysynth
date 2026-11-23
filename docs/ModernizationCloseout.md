<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# .NET 10 modernization closeout

Local implementation and evidence are ready for review, 2026-11-23. No commit,
push or GitHub workflow run was performed by the assistant. Rider remains the
review/commit/push gate. Raw evidence is local and ignored; the code, tests and
summaries below are versioned outside codex/.

## Framework and source contract

Core, tests and Workspace target only **net10.0 / C# 14**, SDK **10.0.404** pinned.
The core has no runtime package/backend dependency. Its AOT compatibility analyzers
are enabled. Loop termination/wrapping, exact bounded asset loading, preset nullable
flow and finite gain/speed contracts have executable regression coverage; malformed
asset and command rejection occurs at load/command boundaries. Frozen synthesis
values and numerical tolerances are unchanged. No C# 13 or legacy target was added.

The current WebGPU native host consumes this core through a **direct project
reference**. Standard checkout layout (case matters on Linux):

```text
Projects/
  KilnGod.BlazorWebGPU/
  meltysynth/
    MeltySynth/MeltySynth.csproj
```

`KilnGod.WebGPU.BlazorNative` references
`../../meltysynth/MeltySynth/MeltySynth.csproj`, explicitly uses C# 14 and copies the
unchanged notice to `ThirdPartyNotices/MeltySynth-LICENSE.txt`. MeltySynth is also
listed in the WebGPU solution for Rider. The desktop assets file resolves it as a
**project**, not a NuGet dependency, and the copied notice matches byte for byte.
WebGPU's loader workflow now checks out the sibling at reviewed commit
`6323849fbf4f0713bb4f9f48a5da0829833703fe` and pins SDK 10.0.404. CI was not executed.
After committing newer library changes in Rider, update that pin to the reviewed
commit before pushing the WebGPU integration. Source import into WebGPU is a future
discussion: the current work does not move the core or remove this repository.

[Source inclusion, notices and NativeAOT](NativeAotAcceptance.md),
[control/preflight commands](WorkspaceTools.md#asset-packaging-preflight) and
[regression contract](RegressionBaselines.md) are the current integration guidance.
The local package/source archives are validation artifacts, not a published release.
The published upstream NuGet installation example in README remains separately named.

## Fresh builds and tests

Pinned-SDK root Release build: **zero warnings/errors**. All **16 example Release
builds passed**, NAudio cross-built with EnableWindowsTargeting on this Mac. No
backend migration or device-playback acceptance is inferred from those builds.
Each exact command, elapsed time, project identity, warnings and full build log is
retained in `bin/validation/modernization-closeout/summary.json` and sibling logs.
The earlier SDK 10.0.203 example receipts remain under pre-upgrade-inventory/.

Both complete test modes report **343 self-contained passed / 48 external unavailable
/ 0 failed**, **391 discovered** per mode, warnings as errors. No empty test run or
unavailable case counted as a pass. [Audio audit](AudioCostAudit.md) and
[NativeAOT delivery](NativeAotAcceptance.md) retain their separate measured evidence;
they were not silently promoted to all-platform acceptance by this closeout.

## Dependency and sample matrix

All sixteen remain reference-only and still reference upstream MeltySynth 2.4.1;
they do not receive this fork's fixes merely because their framework is upgraded.
They are not the WebGPU playback host. Exact package/license inventory remains in
[ModernizationInventory.md](ModernizationInventory.md). No package versions changed
for this closeout. Existing backend warnings are visible, not suppressed:

| Example | Target / C# | Direct packages | Fresh warning codes |
| --- | --- | --- | --- |
| CSCore | net10.0 / 14.0 | CSCore 1.2.1.2, MeltySynth 2.4.1 | NU1701 |
| DotFeather | net10.0 / 14.0 | DotFeather 3.2.0, MeltySynth 2.4.1 | NU1902, NU1903 |
| DrippyAL | net10.0 / 14.0 | DrippyAL 2.1.3, MeltySynth 2.4.1 | none |
| FNA.NET | net10.0 / 14.0 | FNA.NET 1.0.15.2403, MeltySynth 2.4.1 | none |
| MonoGame | net10.0 / 14.0 | MeltySynth 2.4.1, MonoGame.Framework.DesktopGL 3.8.2.1105 | none |
| NAudio | net10.0-windows / 14.0 | MeltySynth 2.4.1, NAudio 2.2.1 | none |
| OpenTK | net10.0 / 14.0 | MeltySynth 2.4.1, OpenTK 4.8.2 | none |
| Raylib_CsLo | net10.0 / 14.0 | MeltySynth 2.4.1, Raylib-CsLo 4.2.0.9 | none |
| Raylib_cs | net10.0 / 14.0 | MeltySynth 2.4.1, Raylib-cs 6.1.1 | none |
| SDL2 | net10.0 / 14.0 | MeltySynth 2.4.1, ppy.SDL2-CS 1.0.82 | none |
| SFML.Net | net10.0 / 14.0 | MeltySynth 2.4.1, SFML.Net 2.6.0 | NETSDK1206 |
| Silk.NET.OpenAL | net10.0 / 14.0 | MeltySynth 2.4.1, Silk.NET.OpenAL 2.21.0, Silk.NET.OpenAL.Soft.Native 1.23.1 | none |
| Silk.NET.SDL | net10.0 / 14.0 | MeltySynth 2.4.1, Silk.NET.SDL 2.21.0 | none |
| Sokol | net10.0 / 14.0 | MeltySynth 2.4.1, sokol_csharp.unofficial 0.0.1 | none |
| SoundFlow | net10.0 / 14.0 | MeltySynth 2.4.1, SoundFlow 1.0.3 | none |
| TinyAudio | net10.0 / 14.0 | MeltySynth 2.4.1, TinyAudio 0.3.1 | none |

CSCore's NU1701 is a legacy Framework-asset warning; DotFeather's NU1902/NU1903 are
known transitive ImageSharp advisories; SFML's NETSDK1206 concerns RID assets.
These reference examples are not approved production dependencies. No custom
backend repair, adoption of Silk.NET or vulnerability suppression was added.
The other thirteen examples built without these warnings; no unexpected warning
code appeared. Building a sample does not establish its bank/native license rights.

## Current WebGPU playback checks

**Actual desktop mode.** Built KilnGod.WebGPU.DesktopApp with the direct reference,
zero warnings/errors. The real `--motel-audio` entry point loaded pinned SDL3 3.4.20,
played through its normal mode, accepted cues 1–5 and both arrow-key volume controls
(Music 0%, Effects 0%, Music restored to 55%), and exited with Q/status 0.

**Instrumented actual native adapter.** The external, unchanged MotelNativeAudio.cs
is hashed and linked into an isolated probe against this project, with five guarded
observation-only insertions in an ignored copy. Only SDL resolution is isolated;
no game/rendering dependency graph is imported into the library. SDL selected the
real **CoreAudio** device, not a dummy/disk backend. Observed **1,489 blocks**, one
16-second score wrap, all five cues with positive contribution, independent music/
effects mute, finite distinct stereo and queue drain. Queued bytes peaked at
**12,280**, below three 4,096-byte blocks. Feed joined, the subsystem shut down and
a second open/close passed. Measurements describe computed/queued signals and
device processing, not an acoustic recording or a device-latency benchmark.

**Actual browser module.** Headless **Edge 158**, 48 kHz AudioContext, loaded the
unchanged MotelAudio module and existing pre-rendered score/cues. Its 16-second
stereo score continued past the loop boundary; all five cues produced output;
independent mute and enabled music checks passed; abort closed the context. This
path uses pre-rendered WAVs, not runtime C# synthesis. Autoplay is explicitly enabled
for this automated probe; full game UI click-policy/navigation was not retested.

Browser observation note: the initial strict-zero meter test failed, and a later
repeat captured only **1.36e-12 accumulated residual energy** with effects gain 0.
Those failures remain in first-attempt-note.json and per-attempt receipts. The
revised probe inserts pass-through meters in the actual output path, waits on the
audio clock, reports peak/RMS and tests a declared meter floor: peak at most one
16-bit input LSB (1/32768), RMS at most 1e-7. The final run measured **zero peak/RMS**
for both muted paths. The first failure's exact cause was not established; no
production JS was changed and no product defect is claimed fixed. This browser
meter criterion does not alter exact core PCM silence or frozen synthesis tests.

## Reproduce from the MeltySynth checkout

```sh
python3 tools/modernization_closeout.py
python3 tools/webgpu_audio_probe.py --webgpu-root ../KilnGod.BlazorWebGPU --sdl ../KilnGod.BlazorWebGPU/bin/native/osx-arm64/grassland-metal/libSDL3.dylib
python3 tools/browser_audio_probe.py --webgpu-root ../KilnGod.BlazorWebGPU --edge '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge'
```

Supply the actual pinned SDL path if staged elsewhere. Playback checks briefly use
the default output device. They require the sibling WebGPU checkout/assets; absence
is unavailable integration, not a self-contained test failure or passing playback.
All generated code/audio logs, receipts, browser profiles and caches stay in ignored
bin/. The helper leaves the original adapter, browser module and game assets intact.

## Explicitly unfinished acceptance

- The eight optional named SoundFont/MIDI fixtures are absent: 48 external cases
  remain unavailable, with their precise inventory in RegressionBaselines.md.
- Windows/Linux NativeAOT and actual audio-device checks remain on their target
  hosts; iOS/tvOS require their own AOT/static-link/bundle and simulator/device
  acceptance. This Mac does not establish those results.
- Reference-backend runtime/licensing/advisory dispositions remain separate from
  the dependency-free core and chosen WebGPU adapter.
- The updated pinned-checkout workflow is prepared locally; no CI outcome is claimed.
- The first browser short-window observation failure is retained as described above;
  browser tests do not certify acoustic zero, hardware latency or the whole game UI.
- Rider review, commits and pushes are user-owned and still pending. Nothing is
  automatically merged, published, source-imported or deleted.

## Rider review and handoff

1. In the MeltySynth solution, review core/test/Workspace changes and tools/docs
   outside codex/. Inspect the focused regressions and summary links. Keep bin/,
   obj/, codex/, external banks and incidental .DS_Store files out of the commit.
2. Commit/push the reviewed library work in Rider. Preserve the MIT notice and
   the recorded remaining platform/fixture limits.
3. In the WebGPU solution, review the MeltySynth project reference, solution entry,
   notice copying, SDK/source checkout in the loader workflow and audio documentation.
   Update the workflow source SHA to the reviewed library commit. Keep unrelated
   existing graphics/scene changes separate unless intentionally reviewed together.
4. Commit/push that integration through Rider and inspect CI when it runs. Resolve
   any new result separately; do not relabel absent fixtures or deferred hosts.

Machine receipts: `bin/validation/modernization-closeout/summary.json`,
`native-playback/summary.json`, `browser-playback/summary.json` and `acceptance.json`.
