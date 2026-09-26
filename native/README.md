# native

The game's C++ code: GDExtensions for what the engine calls in its inner loops, where
a call into C# costs too much. Game logic stays in C#. The rule and the measurement
behind it are in `docs/engineering/diagnostics.md`.

| Extension | What it is | Loaded |
| --- | --- | --- |
| `packet_log` | `PacketLogPeer`: the server's ENet peer, wrapped to record every packet in and out | By the server at runtime, only with `--log-packets` (`NativePacketLog.cs`) |

## Building

```
.\scripts\native-build.ps1          # incremental
.\scripts\native-build.ps1 -Clean   # from scratch
```

It needs the Visual Studio Build Tools with the "Desktop development with C++"
workload. The script finds the install and uses its MSVC, CMake and Ninja, so none of
them has to be on PATH. The first build downloads godot-cpp and compiles its ~1,000
binding files (a few minutes); later builds only compile what changed.

Stop the server before a rebuild: Windows locks a DLL while it is loaded.

- `native/build/`: CMake's build folder, ignored by git.
- `native/bin/`: the built DLLs, ignored by git. Each machine builds its own.
- `native/.gdignore`: keeps the editor from scanning godot-cpp's sources.

## Versions

godot-cpp is pinned to `godot-4.5-stable` in `CMakeLists.txt`, and the bindings come
from godot-cpp's own 4.5 API. godot-cpp has no 4.7 branch, and an extension built for
4.5 loads in later 4.x engines. If an extension ever needs a 4.6+ API, dump it from the
engine (`--dump-extension-api --dump-gdextension-interface`) and point godot-cpp's
`GODOTCPP_CUSTOM_API_FILE` / `GODOTCPP_GDEXTENSION_DIR` at it.

Only a Windows x86-64 build is set up. Hosting the server on Linux needs a Linux build
and a `linux.*` line in the `.gdextension` file.

## Adding an extension

A new `add_library` in `CMakeLists.txt`, a `.gdextension` file next to
`packet_log.gdextension`, and a runtime `GDExtensionManager.LoadExtension` where it is
needed (or remove the folder's `.gdignore` rule for it, if clients need it at start).
