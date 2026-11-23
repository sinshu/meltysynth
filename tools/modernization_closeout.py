#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2026 James Carpenter
# Contributed by KilnGod (James Carpenter).
# See LICENSE.txt for the full MIT license and inherited notices.

"""Retain root/test/example closeout evidence without Git writes or backend migrations."""
import concurrent.futures
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'bin/validation/modernization-closeout'


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    host = ROOT / 'bin/dotnet' / ('dotnet.exe' if os.name == 'nt' else 'dotnet')
    dotnet = str(host) if host.is_file() else 'dotnet'
    receipt = {'accepted': False, 'sdk': subprocess.check_output([dotnet, '--version'], cwd=ROOT, text=True).strip(),
               'headAtStart': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(), 'examples': []}
    def build(project, root=False):
        name = 'root' if root else project.parent.name
        command = [dotnet, 'build', str(project.relative_to(ROOT)), '-c', 'Release', '--nologo', '-maxcpucount:1']
        if root: command.append('-warnaserror')
        elif name == 'NAudio': command.append('-p:EnableWindowsTargeting=true')
        start = time.monotonic(); log = OUTPUT / (name + '.build.log')
        with log.open('w') as stream:
            run = subprocess.run(command, cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT, timeout=180)
        content = log.read_text()
        return {'project': project.relative_to(ROOT).as_posix(), 'command': command, 'exitCode': run.returncode,
                'seconds': round(time.monotonic()-start, 3), 'warningCodes': sorted(set(re.findall(r'warning ([A-Z]+\d+)', content))),
                'errors': sorted(set(re.findall(r'error ([A-Z]+\d+)', content))), 'log': log.relative_to(ROOT).as_posix(),
                'sourceSha256': hashlib.sha256(project.read_bytes()).hexdigest()}
    try:
        receipt['root'] = build(ROOT / 'MeltySynth.slnx', True)
        assert receipt['root']['exitCode'] == 0
        projects = sorted((ROOT / 'Examples').glob('*/*.csproj'))
        assert len(projects) == 16
        # Independent projects only; cap build concurrency and each MSBuild worker count.
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as workers:
            for row in workers.map(build, projects):
                receipt['examples'].append(row)
                print(json.dumps({'project': row['project'], 'exitCode': row['exitCode'], 'warnings': row['warningCodes']}), flush=True)
        with (OUTPUT / 'regression.log').open('w') as log:
            subprocess.run(['python3', 'tools/regression_baseline.py', '--output-dir', str(OUTPUT / 'regression')], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, timeout=240, check=True)
        receipt['regression'] = json.loads((OUTPUT / 'regression/summary.json').read_text())
        receipt['expectedReferenceOnlyWarnings'] = {'CSCore': ['NU1701'], 'DotFeather': ['NU1902', 'NU1903'], 'SFML.Net': ['NETSDK1206']}
        receipt['unexpectedExampleWarnings'] = [{'project': r['project'], 'warnings': sorted(set(r['warningCodes'])-set(receipt['expectedReferenceOnlyWarnings'].get(Path(r['project']).parent.name, [])))} for r in receipt['examples'] if set(r['warningCodes'])-set(receipt['expectedReferenceOnlyWarnings'].get(Path(r['project']).parent.name, []))]
        assert all(r['exitCode'] == 0 for r in receipt['examples'])
        receipt['accepted'] = receipt['regression']['selfContainedAccepted']
        receipt['examplePlaybackAccepted'] = False
        receipt['gitWritesPerformed'] = False
    finally:
        (OUTPUT / 'summary.json').write_text(json.dumps(receipt, indent=2)+'\n')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
