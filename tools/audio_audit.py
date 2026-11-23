#!/usr/bin/env python3
"""Build plain/profiled source-inclusive audio audits and compare exact PCM output."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--font', type=Path)
    parser.add_argument('--midi', type=Path)
    parser.add_argument('--blocks', type=int, default=4000)
    parser.add_argument('--trials', type=int, default=5)
    parser.add_argument('--runtime-tiering', action='store_true', help='Keep runtime-default tiering to observe startup separately.')
    parser.add_argument('--output-dir', type=Path, default=Path('bin/validation/audio-cost'))
    args = parser.parse_args()
    if bool(args.font) != bool(args.midi): parser.error('Supply both --font and --midi, or neither.')
    output = (ROOT / args.output_dir).resolve(); output.mkdir(parents=True, exist_ok=True)
    host = ROOT / 'bin/dotnet' / ('dotnet.exe' if os.name == 'nt' else 'dotnet')
    dotnet = str(host) if host.is_file() else 'dotnet'
    env = os.environ.copy()
    if not args.runtime_tiering: env['DOTNET_TieredCompilation'] = '0'
    result = {'tieredCompilation': env.get('DOTNET_TieredCompilation', 'runtime-default'), 'accepted': False, 'modes': {}, 'sdk': subprocess.check_output([dotnet, '--version'], cwd=ROOT, text=True).strip(),
              'head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
              'logicalCpuCount': os.cpu_count(), 'machine': platform.machine(), 'platform': platform.platform()}
    files = sorted((ROOT / 'MeltySynth/src').glob('*.cs')) + sorted((ROOT / 'tools/AudioAudit').glob('*.cs')) + [ROOT / 'Workspace/BoundedAudioPipe.cs', ROOT / 'MeltySynthTest/src/MidiLoopTestAssets.cs', ROOT / 'tools/AudioAudit/AudioAudit.csproj', ROOT / 'tools/audio_audit.py']
    result['sourceIdentities'] = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
    if platform.system() == 'Darwin':
        result['cpu'] = subprocess.check_output(['sysctl', '-n', 'machdep.cpu.brand_string'], text=True).strip()
    try:
        for mode in ('plain', 'profiled'):
            binary = ROOT / 'bin/audio-audit' / mode
            directory = output / mode; directory.mkdir(parents=True, exist_ok=True)
            command = [dotnet, 'build', 'tools/AudioAudit/AudioAudit.csproj', '-c', 'Release', '-warnaserror', '-o', str(binary), '-p:AudioAudit=' + str(mode == 'profiled').lower()]
            with (directory / 'build.log').open('w') as log:
                subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=120)
            command = [dotnet, str(binary / 'AudioAudit.dll'), str(directory), str(args.blocks), str(args.trials)]
            if args.font: command += [str(args.font.resolve()), str(args.midi.resolve())]
            with (directory / 'run.log').open('w') as log:
                subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, env=env, check=True, timeout=240)
            receipt = json.loads((directory / 'receipt.json').read_text())
            receipt['binarySha256'] = hashlib.sha256((binary / 'AudioAudit.dll').read_bytes()).hexdigest()
            result['modes'][mode] = receipt
            print(json.dumps({'mode': mode, 'render': [{'voices': r['voices'], 'effects': r['effects'], **r['render']} for r in receipt['results']]}), flush=True)
        plain = result['modes']['plain']; profiled = result['modes']['profiled']
        assert not plain['profiled'] and profiled['profiled']
        assert plain['fixture'] == profiled['fixture']
        for left, right in zip(plain['results'], profiled['results'], strict=True):
            assert (left['voices'], left['effects'], left['pcmSha256']) == (right['voices'], right['effects'], right['pcmSha256'])
            assert left['queue']['pcmMatchesSequential'] and right['queue']['pcmMatchesSequential']
            assert left['queue']['allBuffersReturned'] and right['queue']['allBuffersReturned']
        for left, right in zip(plain['music'], profiled['music'], strict=True):
            assert (left['effects'], left['pcmSha256']) == (right['effects'], right['pcmSha256'])
        # Each executable inspects its own compiled Synthesizer metadata.
        assert not plain['hooksPresent'] and profiled['hooksPresent']
        result['exactPcmMatchesAcrossBuilds'] = True
        result['hardwarePlaybackAccepted'] = False
        result['accepted'] = True
    finally:
        (output / 'summary.json').write_text(json.dumps(result, indent=2) + '\n')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
