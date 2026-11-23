#!/usr/bin/env python3
"""Validate local ARM64 NativeAOT, trimming evidence, NuGet and MIT source delivery."""
import argparse
import array
import base64
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
import uuid

ROOT = Path(__file__).resolve().parents[1]


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, default=Path('bin/validation/native-aot'))
    args = parser.parse_args()
    if platform.system() != 'Darwin' or platform.machine().lower() != 'arm64':
        parser.error('This receipt targets macOS ARM64. Windows/Linux and Apple mobile/TV need their own host acceptance.')
    output = (ROOT / args.output_dir).resolve(); output.mkdir(parents=True, exist_ok=True)
    dotnet = str(ROOT / 'bin/dotnet/dotnet') if (ROOT / 'bin/dotnet/dotnet').is_file() else 'dotnet'
    env = os.environ.copy()
    env['SDKROOT'] = subprocess.check_output(['xcrun', '--sdk', 'macosx', '--show-sdk-path'], text=True).strip()
    result = {'accepted': False, 'sdkRoot': env['SDKROOT'], 'sdk': subprocess.check_output([dotnet, '--version'], cwd=ROOT, text=True).strip(),
              'xcode': subprocess.check_output(['xcodebuild', '-version'], text=True).strip(), 'variants': {}}

    def run(command, log):
        with log.open('w') as stream:
            subprocess.run(command, cwd=ROOT, env=env, stdout=stream, stderr=subprocess.STDOUT, check=True, timeout=300)
        text = log.read_text()
        if re.search(r'\b(?:warning|error) (?:IL|CS|NETSDK)\d+:', text):
            raise ValueError('Unaccepted compiler/trimming diagnostics: ' + str(log))

    def package_extract(archive, destination):
        if destination.exists(): shutil.rmtree(destination)
        destination.mkdir(parents=True)
        with zipfile.ZipFile(archive) as package:
            package.extractall(destination)
            for info in package.infolist():
                mode = info.external_attr >> 16
                if mode: (destination / info.filename).chmod(mode & 0o777)

    project = 'tools/AotSmoke/AotSmoke.csproj'
    try:
        assets = output / 'assets'; assets.mkdir(exist_ok=True)
        run([dotnet, 'run', '--project', project, '-c', 'Release', '-p:PrepareFixtures=true', '-p:PublishAot=false', '--', str(assets)], output / 'fixtures.log')
        shutil.copyfile(ROOT / 'MeltySynthTest/Baselines/synthesis.json', assets / 'synthesis.json')
        result['assets'] = {p.name: {'bytes': p.stat().st_size, 'sha256': sha(p)} for p in sorted(assets.iterdir())}
        sources = sorted((ROOT / 'MeltySynth/src').glob('*.cs'))
        source_rows = {p.name: {'bytes': p.stat().st_size, 'sha256': sha(p)} for p in sources}
        fingerprint = hashlib.sha256(json.dumps(source_rows, sort_keys=True).encode()).hexdigest()
        source_zip = output / 'MeltySynth-source.zip'
        source_readme = 'MeltySynth source delivery\n\nTarget .NET 10, C# 14. Include src/*.cs in your project. Retain LICENSE.txt\nand all upstream copyright/attribution. This is the full core, not a promise\nthat runtime-disabled features trim away. No game, test, native host or\naudit implementation is included. See manifest.json for exact identities.\n'
        with zipfile.ZipFile(source_zip, 'w', zipfile.ZIP_DEFLATED) as package:
            for path in sources: package.write(path, 'src/' + path.name)
            package.write(ROOT / 'LICENSE.txt', 'LICENSE.txt')
            package.write(ROOT / 'global.json', 'global.json')
            package.writestr('README.md', source_readme)
            package.writestr('manifest.json', json.dumps({'target': 'net10.0', 'language': '14.0', 'licenseSha256': sha(ROOT / 'LICENSE.txt'), 'source': source_rows}, indent=2) + '\n')
        extracted = output / 'source-extracted'; package_extract(source_zip, extracted)
        for path in sources: assert sha(extracted / 'src' / path.name) == sha(path)
        assert (extracted / 'LICENSE.txt').read_bytes() == (ROOT / 'LICENSE.txt').read_bytes()
        result['sourceDelivery'] = {'archiveBytes': source_zip.stat().st_size, 'sourceBytes': sum(p.stat().st_size for p in sources), 'sourceFiles': len(sources), 'fingerprint': fingerprint, 'licensePreserved': True}
        run([dotnet, 'build', project, '-c', 'Release', '-warnaserror', '-o', str(output / 'managed'), '-p:AssetDirectory=' + str(assets)], output / 'managed-build.log')
        run([dotnet, str(output / 'managed/AotSmoke.dll'), str(output / 'managed-result')], output / 'managed-result.json')
        managed = json.loads((output / 'managed-result.json').read_text()); assert managed['accepted'] and managed['dynamicCodeSupported']
        feed = output / 'feed'; feed.mkdir(exist_ok=True)
        # An isolated local version prevents an older global NuGet cache entry from
        # impersonating this run after metadata/SDK/repository revision changes.
        version = '2.4.1-aot-' + fingerprint[:12] + '-' + uuid.uuid4().hex[:8]
        run([dotnet, 'pack', 'MeltySynth/MeltySynth.csproj', '-c', 'Release', '-warnaserror', '-p:PackageVersion=' + version, '-o', str(feed)], output / 'pack.log')
        nupkg = feed / ('MeltySynth.' + version + '.nupkg')
        with zipfile.ZipFile(nupkg) as package:
            assert package.read('LICENSE.txt') == (ROOT / 'LICENSE.txt').read_bytes()
            assert 'lib/net10.0/MeltySynth.dll' in package.namelist()
            nuspec = ET.fromstring(package.read('MeltySynth.nuspec'))
            assert not any(n.tag.split('}')[-1] == 'dependency' for n in nuspec.iter())
        result['nugetDelivery'] = {'version': version, 'archiveBytes': nupkg.stat().st_size, 'sha256': sha(nupkg), 'licensePreserved': True, 'packageDependencies': [], 'published': False}
        variants = [('project-runtime', []), ('project-fixed-off', ['-p:FixedEffectsOff=true']),
                    ('source-fixed-off', ['-p:FixedEffectsOff=true', '-p:SourceIntegration=true', '-p:CoreSourceDirectory=' + str(extracted / 'src')]),
                    ('package-runtime', ['-p:UseLocalPackage=true', '-p:LocalPackageVersion=' + version, '-p:RestoreAdditionalProjectSources=' + str(feed)])]
        for name, properties in variants:
            directory = output / name; directory.mkdir(exist_ok=True); publish = directory / 'publish'
            print('Publishing ' + name, flush=True)
            run([dotnet, 'publish', project, '-c', 'Release', '-r', 'osx-arm64', '-p:PublishAot=true', '-p:IlcTreatWarningsAsErrors=true', '-p:ILLinkTreatWarningsAsErrors=true', '-warnaserror', '-p:AssetDirectory=' + str(assets), '-o', str(publish)] + properties, directory / 'publish.log')
            maps = list((ROOT / 'tools/AotSmoke/obj').rglob('AotSmoke.map.xml'))
            mapfile = max(maps, key=lambda p: p.stat().st_mtime)
            shutil.copyfile(mapfile, directory / 'compiler.map.xml')
            nodes = ET.parse(mapfile).getroot()
            methods = [n for n in nodes if n.tag == 'MethodCode']
            effects = [n for n in methods if '_MeltySynth_Reverb_' in n.get('Name', '') or '_MeltySynth_Chorus_' in n.get('Name', '')]
            (directory / 'effect-methods.json').write_text(json.dumps([n.attrib for n in effects], indent=2) + '\n')
            app = publish / 'AotSmoke'
            with zipfile.ZipFile(directory / 'distribution.zip', 'w', zipfile.ZIP_DEFLATED) as package:
                for path in [app, publish / 'LICENSE.txt'] + sorted((publish / 'assets').iterdir()): package.write(path, path.relative_to(publish))
            fresh = directory / 'fresh-extraction'; package_extract(directory / 'distribution.zip', fresh)
            assert not list(fresh.rglob('*.dll')) and not list(fresh.rglob('*.dylib'))
            assert (fresh / 'LICENSE.txt').read_bytes() == (ROOT / 'LICENSE.txt').read_bytes()
            run([str(fresh / 'AotSmoke'), str(directory / 'run-result')], directory / 'run.json')
            native = json.loads((directory / 'run.json').read_text()); assert native['accepted'] and native['assetCollectionsReadOnly'] and not native['dynamicCodeSupported'] and native['architecture'] == 'Arm64'
            comparisons = []
            for scenario in native['scenarios']:
                filename = 'effects-on.f32' if scenario['effects'] else 'effects-off.f32'
                expected = array.array('f'); expected.frombytes((output / 'managed-result' / filename).read_bytes())
                actual = array.array('f'); actual.frombytes((directory / 'run-result' / filename).read_bytes())
                assert len(expected) == len(actual) == 65536
                error = max(abs(a-b) for a,b in zip(expected, actual, strict=True)); assert error <= 1E-6
                comparisons.append({'effects': scenario['effects'], 'samples': len(actual), 'maxAbsoluteError': error})
            links = subprocess.check_output(['otool', '-L', str(app)], text=True); (directory / 'dependencies.txt').write_text(links)
            assert 'hostfxr' not in links.lower() and 'coreclr' not in links.lower()
            if name == 'package-runtime':
                restored = json.loads((ROOT / 'tools/AotSmoke/obj/project.assets.json').read_text())
                library = restored['libraries']['MeltySynth/' + version]
                digest = base64.b64encode(hashlib.sha512(nupkg.read_bytes()).digest()).decode()
                assert library['sha512'] == digest
                result['nugetDelivery']['restoredExactArchive'] = True
            result['variants'][name] = {'binaryBytes': app.stat().st_size, 'binarySha256': sha(app),
                'assetBytes': sum(p.stat().st_size for p in (publish / 'assets').iterdir()),
                'debugSymbolBytes': sum(p.stat().st_size for p in publish.rglob('*') if p.is_file() and ('.dSYM' in str(p) or p.suffix == '.pdb')),
                'distributionZipBytes': (directory / 'distribution.zip').stat().st_size, 'standaloneDllsOrDylibs': 0,
                'effectMethodCount': len(effects), 'effectMethodCodeBytes': sum(int(n.get('Length', '0')) for n in effects),
                'freshExtractionPassed': True, 'native': native, 'managedComparison': comparisons, 'compilerDiagnostics': []}
            print(json.dumps({'variant': name, 'binaryBytes': app.stat().st_size, 'effectMethodsRetained': len(effects), 'nativePassed': True}), flush=True)
        result['fixedOffEffectsCodeRetained'] = result['variants']['project-fixed-off']['effectMethodCount'] > 0
        result['accepted'] = True
        result['otherPlatformsAccepted'] = False
    finally:
        (output / 'summary.json').write_text(json.dumps(result, indent=2) + '\n')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
