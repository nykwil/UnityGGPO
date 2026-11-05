# GekkoNet for Unity and SharedGame

Rollback netcode for Unity built on the pinned upstream [GekkoNet](https://github.com/HeatXD/GekkoNet) C API. Write a deterministic game against a small contract; SharedGame runs it locally, peer-to-peer, as a spectator or under a rollback stress test. The integration is **game → runner → manager → view**.

| Package | Purpose | Dependencies |
|---|---|---|
| **GekkoNet for Unity** (`com.nykwil.gekkonet`) | Native bindings and the Windows x64 plugin, plus the engine-independent session core: game/input contracts, `GekkoRunner`, session options, 60 Hz pacing and diagnostics data | None |
| **SharedGame** (`com.nykwil.sharedgame`) | Unity session lifecycle (`GameManager`), local runner, validation harness; optional UI Toolkit session and diagnostics panels in separate assemblies | GekkoNet for Unity |

The repository also contains **VectorWar**, a fixed-point example game, and two importable SharedGame samples: **Rollback Duel** (minimal complete integration) and **Unity Transport** (a NetworkDriver-backed packet adapter).

## Requirements and platform support

| | Supported |
|---|---|
| Unity | 6.5 (6000.5) or newer; the example project is pinned to 6000.6.0f1 |
| Native plugin | Windows x64 only (requires the Microsoft Visual C++ x64 runtime) |
| Scripting backends | Mono and IL2CPP (validated on Windows) |
| Networking | Direct, reachable numeric IPv4 UDP endpoints; 2–4 players, up to 16 spectators |

Matchmaking, relay, NAT traversal, replays, runahead controls and other platform binaries are not provided. Tests and validation runs are described in [validation](docs/VALIDATION.md).

## Installation

Add both packages to your project's `Packages/manifest.json`. Git dependencies are not resolved transitively, so list GekkoNet explicitly; append `#<tag-or-commit>` to pin a version:

```json
"dependencies": {
  "com.nykwil.gekkonet": "https://github.com/nykwil/UnityGGPO.git?path=/Unity/Packages/com.nykwil.gekkonet",
  "com.nykwil.sharedgame": "https://github.com/nykwil/UnityGGPO.git?path=/Unity/Packages/SharedGame"
}
```

Alternatively, in Package Manager choose **Install package from disk** for `Unity/Packages/com.nykwil.gekkonet/package.json`, then `Unity/Packages/SharedGame/package.json`.

A game that drives `GekkoRunner` itself can install only `com.nykwil.gekkonet`: its `Nykwil.GekkoNet.Session` assembly has no UnityEngine references. With SharedGame, the `Runtime/UI` and `Runtime/Diagnostics` folders are optional; the core assembly references neither. The diagnostics panel depends on the basic UI for its visibility toggle.

## Getting started

1. Import the **Rollback Duel** sample from SharedGame's Samples tab and choose **GekkoNet → Create SharedGame Rollback Duel Scene** for a working scene.
2. Implement `IGame` and `ILocalInputSource` for your game and subclass `GameManager`. The [SharedGame guide](Unity/Packages/SharedGame/README.md) walks through a complete minimal game, determinism rules, session options, lifecycle and the optional UI.
3. Start a session from the optional session UI or from code with `GameManager.StartSession(new SessionOptions { ... })`.

Upgrading from the original UnityGGPO packages? See the [migration guide](docs/API-MIGRATION.md).

## Sessions

Choose local, peer, spectator or stress mode. Every peer must use the same ordered 2–4-player roster; array order defines stable simulation indices. Choose the local player index and local controller separately. Player 0 is the default spectator host. Enter spectator endpoints separately from the roster and give each spectator its own listening port. The host registers spectators after player startup, so absent viewers cannot block the game.

Defaults are two input-delay frames, eight prediction frames and a five-second disconnect timeout. Disconnects use native synchronized zero inputs; reconnecting a player starts a fresh session. Spectators use native snapshot synchronization, including late joins.

## VectorWar example

Open the `Unity` directory with Unity 6000.6.0f1 and play the VectorWar scene. Controller 0 uses arrows, Right Ctrl (fire) and Right Shift (bomb); controller 1 uses WASD, F (fire) and G (bomb). The bomb bit is kept from the original mappings but has no gameplay effect. Controllers 2/3 or missing keyboards supply neutral input; remote peers normally select controller 0. **GekkoNet → Validate VectorWar Assets** audits the scene's references.

## Building the native library

Initialize the pinned submodule, then run `./build_gekkonet.ps1`. The script selects Visual Studio's bundled CMake when needed, applies the checked-in [disconnect fix](gekkonet-disconnect.patch) to an isolated source copy and records source, patch, toolchain and binary provenance. The upstream checkout stays clean, and two builds produced identical DLL bytes. Third-party licenses ship beside the native package.

## Validation

Run `dotnet run --project Validation/SharedGame.Validation.csproj -c Release` with .NET 10 for interop, snapshot, rollback-stress, lifecycle and allocation checks, and `./scripts/check-packages.ps1` for package hygiene (committed `.meta` files, native binary provenance, submodule pin and version consistency). The [Validate workflow](.github/workflows/validate.yml) runs both on Windows, plus the native ABI layout check and four-player, spectator, player-leave and impaired-network sessions. Unity test scripts and further network/proxy commands are listed in [validation](docs/VALIDATION.md).

## License

MIT; see [LICENSE](LICENSE). The bundled GekkoNet library, Asio and zpp_bits are distributed under their own licenses in the GekkoNet package.
