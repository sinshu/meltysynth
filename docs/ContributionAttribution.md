<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# Contribution attribution for the .NET 10 / C# 14 modernization

Prepared 2026-11-23 for review in Rider. The original MIT copyright notices and
permission/disclaimer text in LICENSE.txt are preserved. A separate 2026 notice
covers James Carpenter’s original KilnGod contributions; it does not claim authorship
of upstream code or of the MIT license text.

## File credit rule

Compared with upstream commit `4ba079c9ee1453b03be7e164d254f48426f0aac3` (the last upstream commit
before this fork’s contributions). File notices credit James Carpenter only for
newly contributed files or an estimated contribution **strictly greater than 55%**.
For existing files, compare multisets of trimmed nonblank lines before adding these
headers: matching upstream lines are retained even if moved or reindented. This is
a conservative file-credit estimate, not changed-line counts inflated by the new
headers. Partial fixes below the threshold keep upstream attribution only.

The two existing source files receiving an additional personal notice are:

| Existing file | Estimated new/reworked share |
| --- | ---: |
| `MeltySynth/src/SoundFontSampleData.cs` | 56.64% |
| `MeltySynthTest/src/TestSettings.cs` | 75.00% |


## Wholly contributed files

These new files receive `Copyright (C) 2026 James Carpenter` followed by
`Contributed by KilnGod (James Carpenter).` and the MIT SPDX identifier.

| File | Notice location |
| --- | --- |
| `.gitattributes` | File header |
| `MeltySynth/src/AssetLoadLimits.cs` | File header |
| `MeltySynth/src/AssetReader.cs` | File header |
| `MeltySynth/src/NumericDomain.cs` | File header |
| `MeltySynth/src/ReadOnlyAssetList.cs` | File header |
| `MeltySynthTest/Baselines/external-assets.example.json` | Adjacent .license sidecar |
| `MeltySynthTest/Baselines/fixtures.json` | Adjacent .license sidecar |
| `MeltySynthTest/Baselines/synthesis.json` | Adjacent .license sidecar |
| `MeltySynthTest/src/AssetParsingLimitsTest.cs` | File header |
| `MeltySynthTest/src/AssetPreflightTest.cs` | File header |
| `MeltySynthTest/src/BoundedAudioPipeTest.cs` | File header |
| `MeltySynthTest/src/ControlBoundaryTest.cs` | File header |
| `MeltySynthTest/src/MidiFileSequencerLoopTest.cs` | File header |
| `MeltySynthTest/src/MidiLoopTestAssets.cs` | File header |
| `MeltySynthTest/src/NumericAssetBoundaryTest.cs` | File header |
| `MeltySynthTest/src/OriginalFixtureReferenceTest.cs` | File header |
| `MeltySynthTest/src/OscillatorLoopRegressionTest.cs` | File header |
| `MeltySynthTest/src/PreparedAssetCollectionTest.cs` | File header |
| `MeltySynthTest/src/PresetLookupFlowTest.cs` | File header |
| `MeltySynthTest/src/SampleLoopValidationTest.cs` | File header |
| `MeltySynthTest/src/SequencerClockTest.cs` | File header |
| `MeltySynthTest/src/SequencerReentrancyTest.cs` | File header |
| `MeltySynthTest/src/SoundFontSampleLoadingTest.cs` | File header |
| `MeltySynthTest/src/SynthesisBaselineScenario.cs` | File header |
| `MeltySynthTest/src/SynthesisBaselineTest.cs` | File header |
| `MeltySynthTest/src/TestAssets.cs` | File header |
| `MeltySynthTest/src/WorkspaceToolTest.cs` | File header |
| `Workspace/AssetPreflight.cs` | File header |
| `Workspace/BoundedAudioPipe.cs` | File header |
| `Workspace/ToolPaths.cs` | File header |
| `Workspace/WorkspaceCommand.cs` | File header |
| `Workspace/asset-profile.example.json` | Adjacent .license sidecar |
| `docs/AudioCostAudit.md` | File header |
| `docs/CallbackReentrancy.md` | File header |
| `docs/ModernizationCloseout.md` | File header |
| `docs/ModernizationInventory.md` | File header |
| `docs/NativeAotAcceptance.md` | File header |
| `docs/NumericBoundaries.md` | File header |
| `docs/PreparedAssetCollections.md` | File header |
| `docs/RegressionBaselines.md` | File header |
| `docs/SequencerClock.md` | File header |
| `docs/WorkspaceTools.md` | File header |
| `global.json` | Adjacent .license sidecar |
| `tools/AotSmoke/AotSmoke.csproj` | File header |
| `tools/AotSmoke/Program.cs` | File header |
| `tools/AudioAudit/AudioAudit.csproj` | File header |
| `tools/AudioAudit/AudioAuditCounters.cs` | File header |
| `tools/AudioAudit/Program.cs` | File header |
| `tools/BrowserAudioProbe.html` | File header |
| `tools/WebGpuAudioProbe/Program.cs` | File header |
| `tools/WebGpuAudioProbe/WebGpuAudioProbe.csproj` | File header |
| `tools/audio_audit.py` | File header |
| `tools/browser_audio_probe.py` | File header |
| `tools/install_dotnet_sdk.py` | File header |
| `tools/modernization_closeout.py` | File header |
| `tools/native_aot_smoke.py` | File header |
| `tools/pre_upgrade_inventory.py` | File header |
| `tools/regression_baseline.py` | File header |
| `tools/webgpu_audio_probe.py` | File header |
| `docs/ContributionAttribution.md` | File header |

## Coverage without changing data

Tracked C#, Python, batch, HTML, Markdown, project/solution XML, repository
ignore and attributes files carry short MIT notices linked to LICENSE.txt, except
CHANGELOG.md: it originally had no copyright header and is left byte-for-byte
unchanged at the user’s request. Existing
upstream-only files retain inherited copyright through that canonical license; no
new personal notice is added for a small fix. Existing third-party acknowledgments
and the upstream contributors’ attribution remain intact.

Strict JSON files use adjacent `.license` sidecars so SDK configuration, asset
profiles and frozen baseline identities remain byte-for-byte unchanged. Reference
CSV/TSV/audio/MIDI fixtures are unchanged and retain their existing upstream license
coverage; no ownership of third-party fixture content is asserted. Generated .idea
settings, ignored bin/obj/codex artifacts and operating-system metadata are outside
the source-header pass.

Source and NuGet/native delivery already include LICENSE.txt. It now carries both
the unchanged inherited notices and the separate 2026 contribution notice. The
MIT permission and disclaimer are unchanged. Package authorship metadata remains
upstream-owned; this change does not replace the original library author.

The machine-readable pre-header classification, identities and verification logs
are local under ignored `bin/validation/license-attribution/`. No commit, push or
pull request is performed by this attribution pass; review and submit through Rider.

## Validation of this notice pass

Both Release/C# 14 compilation modes passed 704 self-contained tests, with 48
external-bank comparisons unavailable and no failures. All four fresh macOS ARM64
NativeAOT deliveries passed. Source archives and NuGet packages retain the exact
updated LICENSE.txt, and every exported source file carries its MIT identifier.
No numerical golden data, tolerance or runtime source body changed. CHANGELOG.md
was restored exactly to its pre-attribution content by explicit request. Local
receipts are under bin/validation/license-attribution/acceptance.json.
