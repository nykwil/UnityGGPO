# GekkoNet for Unity

Direct P/Invoke bindings to the pinned upstream GekkoNet C API, native statistics types and a Windows x64 DLL. This package has no UI, gameplay or Unity package dependencies. SharedGame provides session ownership, runners and optional UI; its importable Rollback Duel sample demonstrates integration.

## Ownership and ABI

Create a session, configure it, add actors in stable player-index order and consume every returned save/load/advance event in order. Write snapshot length and FNV-1a checksum through save output pointers. Returned pointers are native-owned and must not survive an update. Use one creating thread.

The built-in UDP adapter owns a process-wide socket. Allow one network session per process. Destroy the session before the adapter, including every failed startup. Calling gekko_default_adapter a second time replaces the previous socket; SharedGame prevents that misuse.

Bindings cover players, spectators, stress sessions, disconnects, timeouts, input delay, prediction configuration and statistics. Replay/custom transport APIs and runahead controls are outside this release scope. C bool fields use bytes; bool return values use I1 marshalling.

Validation/native_abi.cpp checks native sizes, offsets, unions, enums and bool representation against the layouts tested by managed CoreChecks. These checks are Windows x64 specific.

## Native build

Run build_gekkonet.ps1 from the repository using Visual Studio C++ tools and CMake supporting the selected generator. The script initializes the pinned submodule, builds the direct upstream shared library and records source revision, header/binary SHA-256 and toolchain provenance. The Microsoft Visual C++ x64 runtime is required.

The pinned revision is 45dac7e50fd3ac2d5c94be1fafa6f0d3214a9cea. The reproducible build applies gekkonet-disconnect.patch to a copy of that source. This makes disconnection terminal for the session: delayed handshake replies cannot revive departed actors and cause collateral disconnect claims. The C API and ABI remain unchanged; build-provenance.json records the patch and DLL hashes. The BSD, Asio and zpp_bits licenses are included.

## Compatibility

Unity package API target: 6000.5+ (matching package.json). Tested core/interop code: Unity 6000.6.0f1 and 7000.0.0a12 on Windows x64. The latter applies only to that alpha. See the repository's docs/VALIDATION.md for tests, limitations and release blockers. No other platform binaries are shipped or promised.
