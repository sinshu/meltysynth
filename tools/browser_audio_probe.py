#!/usr/bin/env python3
"""Exercise the actual pre-rendered Motel Web Audio module in isolated headless Edge."""
import argparse
import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import queue
import subprocess
import threading
import uuid

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--webgpu-root', required=True, type=Path)
    parser.add_argument('--edge', required=True, type=Path)
    args = parser.parse_args()
    wwwroot = args.webgpu_root / 'KilnGod.WebGPU.ArcadeSample/wwwroot'
    output = ROOT / 'bin/validation/modernization-closeout/browser-playback'; output.mkdir(parents=True, exist_ok=True)
    attempt = output / ('attempt-' + uuid.uuid4().hex[:8]); attempt.mkdir()
    received = queue.Queue(maxsize=1)
    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            name = self.path.split('?',1)[0]
            if name == '/': content = (ROOT / 'tools/BrowserAudioProbe.html').read_bytes(); kind = 'text/html'
            elif name == '/engine/motel-audio.js': content = (wwwroot / 'motel-audio.js').read_bytes(); kind = 'application/javascript'
            elif name.startswith('/engine/motel-audio/') and Path(name).name in {'neon-mile.wav', 'motel-fx-jump.wav', 'motel-fx-setback.wav', 'motel-fx-cash.wav', 'motel-fx-arrival.wav', 'motel-fx-finish.wav'}:
                content = (wwwroot / 'motel-audio' / Path(name).name).read_bytes(); kind = 'audio/wav'
            else: self.send_error(404); return
            self.send_response(200); self.send_header('Content-Type', kind); self.send_header('Content-Length', str(len(content))); self.end_headers(); self.wfile.write(content)
        def do_POST(self):
            if self.path != '/result': self.send_error(404); return
            size = int(self.headers.get('Content-Length', '0'))
            if size > 65536: self.send_error(413); return
            received.put_nowait(json.loads(self.rfile.read(size))); self.send_response(200); self.end_headers()
        def log_message(self, *_): pass
    server = ThreadingHTTPServer(('127.0.0.1', 0), Handler); worker = threading.Thread(target=server.serve_forever, daemon=True); worker.start()
    process = None
    result = {'accepted': False, 'moduleSha256': hashlib.sha256((wwwroot / 'motel-audio.js').read_bytes()).hexdigest()}
    try:
        with (attempt / 'edge.log').open('w') as log:
            process = subprocess.Popen([str(args.edge), '--headless=new', '--no-first-run', '--no-default-browser-check', '--autoplay-policy=no-user-gesture-required',
                '--user-data-dir=' + str(attempt / 'edge-profile'), '--disk-cache-dir=' + str(attempt / 'edge-cache'), 'http://127.0.0.1:' + str(server.server_port)], stdout=log, stderr=subprocess.STDOUT)
            result['playback'] = received.get(timeout=75)
            result['accepted'] = result['playback']['accepted']
            assert result['accepted'], result['playback']
            print(json.dumps(result['playback']), flush=True)
    finally:
        if process:
            process.terminate()
            try: process.wait(timeout=10)
            except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=5)
        server.shutdown(); server.server_close(); worker.join(timeout=3)
        (attempt / 'summary.json').write_text(json.dumps(result, indent=2)+'\n')
        result['attemptDirectory'] = str(attempt.relative_to(ROOT))
        (output / 'summary.json').write_text(json.dumps(result, indent=2)+'\n')


if __name__ == '__main__':
    main()
