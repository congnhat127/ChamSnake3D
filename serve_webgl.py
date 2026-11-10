import http.server
import socketserver
import os
import sys

PORT = 8080
DIRECTORY = os.path.abspath("Builds/WebGL_Netlify")

class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        super().end_headers()

Handler.extensions_map.update({
    '.wasm': 'application/wasm',
    '.data': 'application/octet-stream',
    '.js': 'application/javascript',
    '.symbols.json': 'application/json',
})

if not os.path.exists(DIRECTORY):
    os.makedirs(DIRECTORY, exist_ok=True)

print(f"Serving WebGL at http://0.0.0.0:{PORT} from {DIRECTORY}")
sys.stdout.flush()

socketserver.TCPServer.allow_reuse_address = True
with socketserver.TCPServer(("0.0.0.0", PORT), Handler) as httpd:
    httpd.serve_forever()

