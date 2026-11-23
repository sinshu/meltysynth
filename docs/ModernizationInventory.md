# MeltySynth modernization inventory and frozen scope

Reviewed 2026-11-23. This inventory closes implementation-sequence item 1. It
records the checkout and review decisions; it does not establish device playback,
AOT acceptance or completion of the core framework upgrade.

## Scope and source baseline

- Reuse existing work branch `kilngod/fix-midi-loop-progress`; HEAD remains
  `4ba079c9ee1453b03be7e164d254f48426f0aac3`. Findings 1–5 are local changes above
  that upstream baseline. No new branch, commit, push or GitHub operation was
  performed for this inventory. Rider remains the Git review/push gatekeeper.
- `origin`: `https://github.com/kilngod/meltysynth.git`;
  `upstream`: `https://github.com/sinshu/meltysynth.git`.
- The active modernization covers the dependency-free synthesis core, its focused
  and numerical tests, and useful offline Workspace tooling. Preserve the MIT
  license and the C# Synth/TinySoundFont/SFZero/MeltySynth attribution in
  [LICENSE.txt](../LICENSE.txt). No replacement audio backend, speech system,
  rendering dependency or per-voice threading is included.
- Per the user's final scope decision, **all 16 examples receive only a basic
  .NET 10/C# 14 upgrade**, including narrow compilation fixes. Preserve their
  dependency versions and audio APIs. They are reference examples, not WebGPU
  dependencies; backend modernization and live playback are deferred unless a
  concrete platform consumer needs one. No example was deleted or moved.
- Before retargeting, all 19 project and 17 solution identities were recorded in
  `bin/validation/pre-upgrade-inventory/before-retarget.json`. Its source-manifest
  SHA-256 is `8ae7a9d935f5b7d8db250421ddfd4d0c514effe38b3b422f25e855b8bcf54c9c`.
  The post-change receipt separately records the updated source identities.

The WebGPU checkout currently has one direct package consumer:
`KilnGod.WebGPU.BlazorNative/KilnGod.WebGPU.BlazorNative.csproj` references NuGet
`MeltySynth` **2.4.1**. `MotelNativeAudio.cs` owns a synthesizer/sequencer on one
long-running producer, feeds stereo float32 SDL3 blocks at 32 kHz/512 frames,
uses a bounded cue channel, and joins the producer before destroying the stream.
It loads embedded MIDI/SoundFont/cues and does not reference the sixteen example
projects. It still consumes the upstream package, **not these local fork fixes**.
Switching it to the reviewed fork/source belongs to a separate integration step;
no WebGPU project was changed here. Inspect each Apple/mobile adapter separately
before inferring that desktop SDL3 audio establishes its playback support.

## Toolchain freeze

| Item | Observed value |
| --- | --- |
| Host | macOS 27.2.1, build 26B131; `osx-arm64` |
| Selected .NET SDK | 10.0.203, commit c23858a6d8 |
| MSBuild | 18.3.3+c23858a6d |
| .NET host/runtime | 10.0.7 ARM64; net8 runtime also installed |
| Xcode selection | `/Applications/Xcode.26.4.app/Contents/Developer` |
| Xcode | 26.4.1, build 17E202; macOS SDK 26.4 |
| Clang | Apple clang 21.0.0 (clang-2100.0.123.102) |
| SDK/workspace configuration | No `global.json`, Directory.Build props/targets, local tool manifest, NuGet config or GitHub workflow in this checkout. SDK pinning remains a core-upgrade task. |

Installed macOS/Mac Catalyst/MAUI workloads do not make MAUI a project dependency
or establish iOS/tvOS workload acceptance. The inventory receipt captures the full
`dotnet --info`, OS and Xcode output, including command diagnostics.

## All projects and solutions

There are **19 projects and 17 XML `.slnx` solutions**, not legacy `.sln` files.
All solution project paths resolve. The root [MeltySynth.slnx](../MeltySynth.slnx)
contains exactly core, tests and Workspace. Each of 16 example directories has
one solution pointing to its own project. The inventory tool validates all links;
adding the examples to the root solution is unnecessary for this scope.

| Project area | Current configuration | Frozen disposition |
| --- | --- | --- |
| [MeltySynth](../MeltySynth/MeltySynth.csproj) | netstandard2.1, nullable enabled, no package dependencies; package 2.4.1 generated on build | Primary .NET 10/C# 14 migration target; framework switch, SDK pin and nullable cleanup remain separate. |
| [MeltySynthTest](../MeltySynthTest/MeltySynthTest.csproj) | Default net8.0 project reference; opt-in net10.0/C# 14 source-inclusive lane | Retain focused/numerical coverage; make the primary framework upgrade in its own step. 153 focused cases already pass in both lanes. Full external-bank suite remains unaccepted. |
| [Workspace](../Workspace/Workspace.csproj) | net8.0; `Program.Main` is empty | Retain useful offline render/benchmark tools. No audio device owner in Main. Helpers allocate full-song buffers and write relative WAV paths; replace hardcoded inputs/output locations when upgrading. |
| Sixteen example projects | net10.0, except NAudio net10.0-windows; explicit LangVersion 14.0 | Basic upgrade completed; all 16 Release builds passed. Runtime behavior and backend dependencies otherwise unchanged. |

All examples still reference the **NuGet MeltySynth 2.4.1 package**, not the local
project. Their successful builds therefore do not test this fork's synthesis fixes.
The two basic source fixes are explicit mutable `buffer.AsSpan()` arguments in
FNA.NET/MonoGame, resolving C# 14 `ReadOnlySpan<short>` overload selection. No
threading, scheduling, resource ownership or device-format fixes were mixed in.

### Entry points, native dependencies and playback ownership

Every row below has the disposition **basic upgrade complete; reference-only for
WebGPU integration; platform playback deferred**. "Requires" describes the sample
API/native dependency, not a tested list of shipped architectures. All rows assume
44.1 kHz stereo unless the engine negotiates otherwise.

| Directory / entry point | Native/loading path and ownership review | Remaining issues if adopted |
| --- | --- | --- |
| CSCore / `Program.Main` | CSCore WaveOut uses Windows multimedia; `MidiWaveSource.Read`/Play/Stop share a lock; `using WaveOut` owns device lifetime, source Dispose is empty. | Windows APIs behind generic net10 TFM; NU1701 framework compatibility warning. No Windows execution here. |
| DotFeather / `MidiExampleScene.Main` | `DF.Run` creates the scene; AudioPlayer enumerates locked, reusable left/right PCM batches; OnDestroy stops/disposes AudioPlayer. NuSpec pulls Silk.NET and native OpenAL Soft. | Iterator/callback and scene teardown acceptance; transitive ImageSharp warnings; not a way to avoid Silk.NET. |
| DrippyAL / `Program.Main` | Nested `using AudioDevice`/AudioStream; locked short-buffer callback/control; package depends on Silk.NET.OpenAL and OpenAL Soft. | Native RIDs and callback shutdown untested; also indirectly adopts Silk.NET. |
| FNA.NET / `Program.Main` → `Game1.LoadContent` | Game owns MidiPlayer/DynamicSoundEffectInstance; BufferNeeded renders reusable byte PCM; UnloadContent disposes player. Native assets supplied through FNA.NET.NativeAssets. | Validate callback/control affinity and in-flight disposal on any future target; no locking shown in the example. |
| MonoGame / `Program.Main` → `Game1.LoadContent` | Same dynamic-sound ownership pattern; DesktopGL package depends on native SDL and Vorbis support. Game is disposed by using. | Same callback/control acceptance; native graphics/audio runtime is unrelated to WebGPU. |
| NAudio / `Program.Main` | Explicit Windows target; WaveOut function callbacks request float PCM from locked ISampleProvider; device disposed by using. | Windows playback only; Mac cross-build used EnableWindowsTargeting, not execution. |
| OpenTK / `Program.Main` | Raw OpenAL device/context created on Main; MidiPlayer owns queued AL buffers/source and a cancellation/polling task; locks serialize sequencer controls/rendering. | Verify context affinity, background faults and cancellation/join. Device/context cleanup is after the using block rather than in finally. Old batch script expects external openal32.dll. |
| Raylib_CsLo / `Program.Main` | Main/window loop refills one reusable Int16 stereo buffer when requested; Raylib owns audio device/stream. | Loop closes only the window explicitly; stream unload/device close and failure cleanup are not shown. MPL-2.0 wrapper is not adopted into the engine. |
| Raylib_cs / `Program.Main` | Same main-loop refill design, with fixed pointer upload and raylib native binaries resolved by package/backend. | Same cleanup omissions; no runtime/native architecture acceptance from a build. |
| SDL2 / `Program.Main` | SDL audio callback and controls share a lock; close device then quit audio on normal exit. ppy package includes native SDL2 assets. | `ALLOW_ANY_CHANGE` accepts a changed format while synth stays fixed; obtained format ignored; cleanup is not finally-protected. Inspected package lacks an osx-arm64 native asset. This is not the engine's SDL3 adapter. |
| SFML.Net / `Program.Main` | MidiPlayer owns a SoundStream; OnGetData renders a reusable ~50 ms stereo Int16 batch under lock; Dispose releases stream. Native CSFML resolved by package. | NETSDK1206 flags a version-specific `fedora-x64` native RID; validate callback drain and each adopted architecture. |
| Silk.NET.OpenAL / `Program.Main` | API wrappers, native OpenAL Soft, manual device/context; MidiPlayer owns AL buffers/source and cancel/join polling task. | Context/device cleanup outside finally; verify context affinity/fault cleanup. Reference-only per no-Silk application preference. |
| Silk.NET.SDL / `Program.Main` | Sdl.GetApi owns wrapper; locked callback/control; native SDL2 arrives through Ultz.Native.SDL; manual device close/audio quit. | Callback delegate lifetime, ignored obtained format, ANY_CHANGE and exception cleanup need review only if adopted. Not an SDL3 replacement. |
| Sokol / `Program.Main` | Static synth/sequencer, unmanaged stream callback with a shared lock, Audio.Setup/Shutdown; native sokol package resolves platform library. | Static asset construction, callback channel assumption, exceptions and shutdown on failure not accepted; experimental package remains reference-only. |
| SoundFlow / `Program.Main` | Using MiniAudioEngine owns native miniaudio; MidiPlayer added to static Master mixer; locked GenerateAudio/Play/Stop; engine rate supplied to synth. | Confirm mixer removal, callback drain and negotiated channels. Native package presence, including iOS framework, does not establish AOT/static or platform playback. |
| TinyAudio / `Program.Main` | DirectSoundAudioPlayer, 200 ms buffer, locked Int16 callback; using owns player. | Windows-specific implementation despite generic net10 TFM; no Mac playback claim. |

Common input/launch assumptions: every example loads `TimGM6mb.sf2` relative to its
working directory and uses absolute `C:\Windows\Media\flourish.mid`. None provides a
portable asset CLI or obtains the bank. Console samples block on ReadKey; graphical
samples run their framework loop. `Workspace/Examples.cs` shares those inputs;
its WAV normalization needs an explicit silence policy before reuse. Workspace
Benchmark.Run times a single full-buffer render, excluding setup/file output; it
is not a warmed block/p99/device-latency benchmark.

## Dependency versions and license evidence

The table below is exact-version direct PackageReference metadata, inspected from
NuGet manifests; file-based license texts were read from their nupkgs. Local raw
manifests, hashes and license text receipts are under
`bin/validation/pre-upgrade-inventory/`. This is a provenance inventory, not a claim
that one wrapper license covers every native/transitive component. The inventory
receipt also records resolved package versions/SHA-512 and available transitive
license metadata from each project's `project.assets.json` and local NuSpec cache.
No dependency versions were upgraded in this task.

