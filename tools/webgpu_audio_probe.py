#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2026 James Carpenter
# Contributed by KilnGod (James Carpenter).
# See LICENSE.txt for the full MIT license and inherited notices.

"""Run the existing SDL3 Motel adapter with this fork, isolated from game project dependencies."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--webgpu-root', required=True, type=Path)
    parser.add_argument('--sdl', required=True, type=Path)
    args = parser.parse_args()
    output = ROOT / 'bin/validation/modernization-closeout/native-playback'; output.mkdir(parents=True, exist_ok=True)
    origin = args.webgpu_root / 'KilnGod.WebGPU.BlazorNative/MotelNativeAudio.cs'
    text = origin.read_text()
    replacements = {
        'var queued = SDL_GetAudioStreamQueued(_stream);': 'var queued = SDL_GetAudioStreamQueued(_stream);\n            ProbeQueue(queued);',
        'while (_cues.Reader.TryRead(out var cue)) cursors[(int)cue] = 0;': 'while (_cues.Reader.TryRead(out var cue)) { cursors[(int)cue] = 0; ProbeCueSeen((int)cue); }',
        'var effects = EffectsVolume;': 'var effects = EffectsVolume;\n            ProbeBegin();',
        'cursor += cueFrames;': 'ProbeEffect(i, cursor, cueFrames, gainLeft, gainRight);\n                cursor += cueFrames;',
        'fixed (float* samples = stereo)': 'ProbeCommit(left, right, stereo, music, effects);\n            fixed (float* samples = stereo)'
    }
    for old,new in replacements.items():
        assert text.count(old) == 1, 'Adapter changed; review telemetry insertion: ' + old
        text = text.replace(old, new)
    instrumented = output / 'MotelNativeAudio.instrumented.cs'; instrumented.write_text(text)
    dotnet = str(ROOT / 'bin/dotnet/dotnet')
    result = {'accepted': False, 'adapterSha256': hashlib.sha256(origin.read_bytes()).hexdigest(),
              'instrumentedSha256': hashlib.sha256(instrumented.read_bytes()).hexdigest(),
              'sdlSha256': hashlib.sha256(args.sdl.read_bytes()).hexdigest(),
              'scope': 'Actual adapter source plus observation-only hooks and SDL resolver; not full UI host or automatic fork adoption.'}
    try:
        with (output / 'build.log').open('w') as log:
            subprocess.run([dotnet, 'build', 'tools/WebGpuAudioProbe/WebGpuAudioProbe.csproj', '-c', 'Release', '-warnaserror',
                '-p:WebGpuRoot=' + str(args.webgpu_root.resolve()), '-p:AdapterSource=' + str(instrumented), '-o', str(output / 'app')], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=180)
        with (output / 'run.log').open('w') as log:
            subprocess.run([dotnet, str(output / 'app/WebGpuAudioProbe.dll'), str(args.sdl.resolve())], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=45)
        result['playback'] = json.loads((output / 'run.log').read_text())
        result['accepted'] = result['playback']['accepted']
        assert hashlib.sha256(origin.read_bytes()).hexdigest()==result['adapterSha256']
        print(json.dumps(result['playback']), flush=True)
    finally:
        (output / 'summary.json').write_text(json.dumps(result, indent=2)+'\n')


if __name__ == '__main__':
    main()
