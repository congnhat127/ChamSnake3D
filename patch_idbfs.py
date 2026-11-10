import os

path = 'Builds/WebGL_Netlify/Build/WebGL_Netlify.framework.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

target = 'injectIndexedDBToAutomaticallyPersist(){IDBFS.queuePersist'
polyfill = 'injectIndexedDBToAutomaticallyPersist(){if(typeof IDBFS==="undefined"){var dummyIDBFS={mount:function(m){return MEMFS.mount(m)},syncfs:function(m,p,c){if(c)c(null)},queuePersist:function(m){}};if(typeof window!=="undefined")window.IDBFS=dummyIDBFS;if(typeof globalThis!=="undefined")globalThis.IDBFS=dummyIDBFS;if(typeof self!=="undefined")self.IDBFS=dummyIDBFS;IDBFS=dummyIDBFS;}IDBFS.queuePersist'

if target in content:
    content = content.replace(target, polyfill, 1)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("SUCCESS: Patched IDBFS polyfill in WebGL_Netlify.framework.js")
else:
    print("Target already patched or not found")

