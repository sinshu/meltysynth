#!/usr/bin/env python3
"""Install the global.json SDK in ignored bin/dotnet (Python 3.12+)."""
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess
import tarfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    version = json.loads((ROOT / 'global.json').read_text())['sdk']['version']
    system = {'Darwin': 'osx', 'Linux': 'linux', 'Windows': 'win'}[platform.system()]
    arch = {'arm64': 'arm64', 'aarch64': 'arm64', 'x86_64': 'x64', 'amd64': 'x64'}[platform.machine().lower()]
    rid = system + '-' + arch
    destination = ROOT / 'bin/dotnet'
    host = destination / ('dotnet.exe' if os.name == 'nt' else 'dotnet')
    if host.is_file() and any(line.startswith(version + ' ') for line in subprocess.check_output([str(host), '--list-sdks'], cwd=ROOT, text=True).splitlines()):
        print(f'SDK {version} already installed: {host}')
        return
    channel = '.'.join(version.split('.')[:2])
    metadata_url = f'https://builds.dotnet.microsoft.com/dotnet/release-metadata/{channel}/releases.json'
    with urllib.request.urlopen(metadata_url, timeout=60) as response:
        metadata = json.load(response)
    sdks = [sdk for release in metadata['releases'] for sdk in release.get('sdks', [release['sdk']])]
    sdk = next(sdk for sdk in sdks if sdk['version'] == version)
    suffix = '.zip' if os.name == 'nt' else '.tar.gz'
    asset = next(asset for asset in sdk['files'] if asset['rid'] == rid and asset['name'].endswith(suffix))
    cache = ROOT / 'bin/sdk-downloads'
    cache.mkdir(parents=True, exist_ok=True)
    archive = cache / f'dotnet-sdk-{version}-{rid}{suffix}'
    if not archive.is_file():
        pending = archive.with_name(archive.name + '.partial')
        try:
            with urllib.request.urlopen(asset['url'], timeout=60) as response, pending.open('wb') as output:
                while block := response.read(1024 * 1024):
                    output.write(block)
            pending.replace(archive)
        finally:
            pending.unlink(missing_ok=True)
    with archive.open('rb') as stream:
        observed = hashlib.file_digest(stream, 'sha512').hexdigest()
    if observed.lower() != asset['hash'].lower():
        raise ValueError(f'SDK SHA-512 mismatch; remove {archive} before retrying')
    destination.mkdir(parents=True, exist_ok=True)
    if suffix == '.zip':
        with zipfile.ZipFile(archive) as package:
            for item in package.infolist():
                if not (destination / item.filename).resolve().is_relative_to(destination.resolve()):
                    raise ValueError('Archive path escapes SDK directory')
            package.extractall(destination)
    else:
        with tarfile.open(archive) as package:
            package.extractall(destination, filter='data')
    actual = subprocess.check_output([str(host), '--version'], cwd=ROOT, text=True).strip()
    if actual != version:
        raise ValueError(f'Expected SDK {version}, resolved {actual}')
    (cache / 'receipt.json').write_text(json.dumps({'version': version, 'rid': rid, 'url': asset['url'], 'sha512': observed}, indent=2) + '\n')
    print(f'Installed SDK {version}: {host}')


if __name__ == '__main__':
    main()
