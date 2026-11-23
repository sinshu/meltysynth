#!/usr/bin/env python3
"""Run the unfiltered portable numerical baseline; report optional external work separately."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""): h.update(block)
    return h.hexdigest()


def digest(rows):
    return hashlib.sha256(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--assets", type=Path)
    parser.add_argument("--references", type=Path)
    parser.add_argument("--external-manifest", type=Path)
    parser.add_argument("--require-external", action="store_true")
    parser.add_argument("--output-dir", default="bin/validation/regression-baseline")
    args = parser.parse_args()
    local_dotnet = ROOT / "bin/dotnet" / ("dotnet.exe" if os.name == "nt" else "dotnet")
    dotnet = str(local_dotnet) if local_dotnet.is_file() else "dotnet"
    output = ROOT / args.output_dir
    output.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((ROOT / "MeltySynthTest/Baselines/fixtures.json").read_text())
    references = (args.references or Path(os.environ.get("MELTYSYNTH_REFERENCE_DATA", ROOT / "MeltySynthTest/ReferenceData"))).resolve()
    assets = (args.assets or Path(os.environ.get("MELTYSYNTH_TEST_ASSETS", ROOT / "MeltySynthTest/ExternalAssets"))).resolve()
    result = {"schemaVersion": 1, "modes": [], "externalAssets": [], "referenceGroups": [],
              "externalBaselinePinned": False, "selfContainedAccepted": False, "externalAccepted": False}
    try:
        for expected in manifest["referenceGroups"]:
            files = [p for p in sorted((references / expected["folder"]).rglob("*")) if p.is_file() and p.suffix in (".csv", ".tsv", ".wav")]
            rows = [{"path": p.relative_to(references).as_posix(), "bytes": p.stat().st_size, "sha256": sha(p)} for p in files]
            actual = {"folder": expected["folder"], "files": len(rows), "bytes": sum(x["bytes"] for x in rows), "sha256": digest(rows)}
            result["referenceGroups"].append(actual)
            if actual != expected: raise ValueError("Reference corpus identity changed: " + expected["folder"])
        pins = {}
        if args.external_manifest:
            pins = {r["name"]: r for r in json.loads(args.external_manifest.read_text())["assets"]}
        for name in manifest["externalAssets"]:
            path = assets / name
            actual = {"name": name, "available": path.is_file(), "sha256": sha(path) if path.is_file() else None}
            pin = pins.get(name, {})
            actual["pinned"] = bool(pin.get("sha256") and pin.get("provenance"))
            if actual["available"] and pin.get("sha256") and pin["sha256"].lower() != actual["sha256"]:
                raise ValueError("External fixture identity mismatch: " + name)
            result["externalAssets"].append(actual)
        result["externalBaselinePinned"] = all(x["available"] and x["pinned"] for x in result["externalAssets"])
        if args.require_external and not result["externalBaselinePinned"]:
            raise ValueError("External acceptance requires all eight permitted files and their SHA-256/provenance manifest.")
        env = os.environ.copy()
        env["MELTYSYNTH_TEST_ASSETS"] = str(assets)
        if args.references: env["MELTYSYNTH_REFERENCE_DATA"] = str(references)
        result["head"] = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        result["sdk"] = subprocess.check_output([dotnet, "--version"], cwd=ROOT, text=True).strip()
        sources = [{"path": p.relative_to(ROOT).as_posix(), "sha256": sha(p)} for p in sorted((ROOT / "MeltySynth/src").glob("*.cs"))]
        result["productionSourceManifestSha256"] = digest(sources)
        result["synthesisBaselineSha256"] = sha(ROOT / "MeltySynthTest/Baselines/synthesis.json")
        for mode in ("project-reference", "source-integration"):
            # The two modes share restore state, so run them sequentially.
            trx = output / (mode + ".trx")
            if trx.exists(): trx.unlink()
            command = [dotnet, "test", "MeltySynthTest/MeltySynthTest.csproj", "-c", "Release", "-warnaserror",
                       "--logger", "trx;LogFileName=" + trx.name, "--results-directory", str(output)]
            if mode == "source-integration": command.append("-p:SourceIntegration=true")
            with (output / (mode + ".log")).open("w") as log:
                run = subprocess.run(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=180)
            if not trx.is_file(): raise ValueError(mode + ": no test receipt (build/discovery failure)")
            tree = ET.parse(trx)
            definitions = {t.get("id"): t.find("t:TestMethod", NS) for t in tree.findall(".//t:UnitTest", NS)}
            counts = {"selfPassed": 0, "externalPassed": 0, "unavailable": [], "failed": []}
            for test in tree.findall(".//t:UnitTestResult", NS):
                method = definitions[test.get("testId")]
                cls = method.get("className", "").split(".")[-1]
                external = cls in manifest["externalClasses"] or (cls == "MidiFileTest" and method.get("name", "").startswith("ReadTest"))
                reason = test.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
                outcome = test.get("outcome")
                if outcome == "Passed": counts["externalPassed" if external else "selfPassed"] += 1
                elif external and outcome == "NotExecuted" and reason.startswith("Unavailable external"):
                    counts["unavailable"].append({"class": cls, "name": test.get("testName"), "reason": reason})
                else: counts["failed"].append({"class": cls, "name": test.get("testName"), "outcome": outcome, "reason": reason})
            record = {"mode": mode, "exitCode": run.returncode, **counts}
            result["modes"].append(record)
            print(json.dumps({"mode": mode, "selfPassed": counts["selfPassed"], "externalPassed": counts["externalPassed"], "unavailable": len(counts["unavailable"]), "failed": len(counts["failed"])}), flush=True)
            if run.returncode or counts["failed"] or counts["selfPassed"] < manifest["minimumSelfContainedCases"]:
                raise ValueError(mode + ": numerical failure, unexpected skip or insufficient self-contained discovery")
        result["selfContainedAccepted"] = True
        result["externalAccepted"] = result["externalBaselinePinned"] and all(not m["unavailable"] and m["externalPassed"] > 0 for m in result["modes"])
        result["disposition"] = "self-contained accepted; external accepted" if result["externalAccepted"] else "self-contained accepted; external unavailable or unpinned"
        code = 0
    except (ValueError, OSError, KeyError, ET.ParseError, subprocess.SubprocessError) as error:
        result["error"] = str(error)
        print(str(error), file=sys.stderr)
        code = 1
    (output / "summary.json").write_text(json.dumps(result, indent=2) + "\n")
    return code


if __name__ == "__main__":
    sys.exit(main())
