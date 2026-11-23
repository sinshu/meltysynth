#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2026 James Carpenter
# Contributed by KilnGod (James Carpenter).
# See LICENSE.txt for the full MIT license and inherited notices.

"""Read-only inventory; writes its receipt under the ignored solution bin directory."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import urllib.parse
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def identity(path):
    return {"path": path.relative_to(ROOT).as_posix(), "bytes": path.stat().st_size,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def digest(items):
    return hashlib.sha256(json.dumps(items, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def command(*args):
    result = subprocess.run(args, cwd=ROOT, capture_output=True, text=True, check=False)
    return {"exitCode": result.returncode, "output": result.stdout.strip(), "stderr": result.stderr.strip()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", default="bin/validation/pre-upgrade-inventory/inventory.json")
    parser.add_argument("--webgpu-root", type=Path)
    args = parser.parse_args()
    projects = sorted([ROOT / "MeltySynth/MeltySynth.csproj", ROOT / "MeltySynthTest/MeltySynthTest.csproj",
                       ROOT / "Workspace/Workspace.csproj", *ROOT.glob("Examples/*/*.csproj")])
    solutions = sorted([ROOT / "MeltySynth.slnx", *ROOT.glob("Examples/*/*.slnx")])
    project_rows, sources, missing = [], [], []
    for path in projects:
        tree = ET.parse(path).getroot()
        files = sorted(path.parent.glob("src/*.cs")) if path.parent.name in ("MeltySynth", "MeltySynthTest") else sorted(path.parent.glob("*.cs"))
        sources.extend(identity(p) for p in files)
        sources.append(identity(path))
        for item in tree.iter("None"):
            if "Update" in item.attrib:
                # MSBuild item escaping (%28, %25, etc.) is not a missing-file condition.
                relative = urllib.parse.unquote(item.attrib["Update"]).replace("\\", "/")
                if not (path.parent / relative).is_file(): missing.append(str(path.relative_to(ROOT)) + ": " + relative)
        project_rows.append({"path": path.relative_to(ROOT).as_posix(),
            "properties": [{e.tag: e.text} for group in tree.findall("PropertyGroup") for e in group],
            "packages": [e.attrib for e in tree.iter("PackageReference")],
            "references": [e.attrib for e in tree.iter("ProjectReference")],
            "entryPoints": [p.relative_to(ROOT).as_posix() for p in files if re.search(r"static\s+(?:unsafe\s+)?void\s+Main\s*\(", p.read_text(encoding="utf-8-sig"))],
            "sourceFiles": len(files), "scope": "basic-example-upgrade-only" if "Examples" in path.parts else "core-upgrade"})
    solution_rows = []
    known = {p.resolve() for p in projects}
    for path in solutions:
        refs = [(path.parent / p.attrib["Path"].replace("\\", "/")).resolve() for p in ET.parse(path).getroot().iter("Project")]
        if any(p not in known or not p.is_file() for p in refs): raise ValueError("Invalid solution reference: " + str(path))
        solution_rows.append({**identity(path), "projects": [p.relative_to(ROOT).as_posix() for p in refs]})
    for path in [ROOT / "LICENSE.txt", ROOT / "README.md", ROOT / "clean.bat", ROOT / "Examples/clean.bat", ROOT / "Examples/build_all.bat"]:
        sources.append(identity(path))
    assets = [identity(p) for p in sorted((ROOT / "MeltySynthTest/ReferenceData").rglob("*")) if p.is_file() and p.suffix.lower() in (".wav", ".csv", ".tsv")]
    assets += [identity(p) for p in sorted(ROOT.glob("*.mid"))]
    resolved = []
    for project in projects:
        asset_file = project.parent / "obj/project.assets.json"
        if not asset_file.is_file(): continue
        lock = json.loads(asset_file.read_text())
        packages = []
        for key, lib in lock.get("libraries", {}).items():
            if lib.get("type") != "package": continue
            metadata = None
            for folder in lock.get("packageFolders", {}):
                directory = Path(folder) / lib.get("path", "")
                specs = list(directory.glob("*.nuspec"))
                if not specs: continue
                spec = ET.parse(specs[0]).getroot()
                ns = spec.tag.split("}")[0] + "}" if "}" in spec.tag else ""
                meta = spec.find(ns + "metadata")
                license_node = meta.find(ns + "license")
                metadata = {"license": None if license_node is None else {"value": license_node.text, **license_node.attrib},
                            "nuspecSha256": hashlib.sha256(specs[0].read_bytes()).hexdigest()}
                break
            packages.append({"identity": key, "sha512": lib.get("sha512"), "metadata": metadata})
        native = sorted({asset for target in lock.get("targets", {}).values() for lib in target.values()
                         for asset, props in lib.get("runtimeTargets", {}).items() if props.get("assetType") == "native"})
        resolved.append({"project": project.relative_to(ROOT).as_posix(), "packages": packages,
                         "nativeRuntimeTargets": native, "assetFileSha256": hashlib.sha256(asset_file.read_bytes()).hexdigest()})
    external = []
    if args.webgpu_root:
        audio = args.webgpu_root / "KilnGod.WebGPU.ArcadeSample/wwwroot/motel-audio"
        for path in sorted(audio.glob("*")):
            if path.is_file(): external.append({"path": path.relative_to(args.webgpu_root).as_posix(), "bytes": path.stat().st_size, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    data = {"schemaVersion": 1, "git": {"head": command("git", "rev-parse", "HEAD"), "branch": command("git", "branch", "--show-current"),
            "status": command("git", "status", "--short")},
        "toolchain": {"dotnet": command("dotnet", "--info"), "xcode": command("xcodebuild", "-version"),
                      "macos": command("sw_vers"), "sdkPinPresent": (ROOT / "global.json").exists()},
        "counts": {"projects": len(projects), "solutions": len(solutions), "examples": len(list(ROOT.glob("Examples/*/*.csproj"))),
                   "sourceFiles": len(sources), "assets": len(assets), "missingCopyItemsAfterMSBuildUnescaping": len(missing)},
        "projects": project_rows, "solutions": solution_rows,
        "sourceManifestSha256": digest(sources), "sourceFiles": sources,
        "assetManifestSha256": digest(assets), "assets": assets,
        "externalWebGpuAssets": external, "resolvedDependencies": resolved, "missingCopyItems": missing,
        "inventoryToolSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
    output = ROOT / args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(data, indent=2) + "\n")
    print(json.dumps({"counts": data["counts"], "sourceManifestSha256": data["sourceManifestSha256"],
                      "assetManifestSha256": data["assetManifestSha256"], "output": str(output)}, indent=2))


if __name__ == "__main__":
    main()
