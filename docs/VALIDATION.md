# Validation

Every check below runs on Windows x64, the only platform with a shipped native binary. CI (`.github/workflows/validate.yml`) runs package hygiene, the native ABI check, the core harness and the .NET network runs on every push.

## Test suites

| Suite | Where | What it guards |
| --- | --- | --- |
| Core harness | `Validation/Program.cs` (.NET 10) | P/Invoke struct layouts and C booleans, snapshot round trip and malformed-data rejection, option validation, socket release after failed and repeated sessions, desync reporting (throwing, event and game-supplied checksums), 100,000 rollback-stress updates matching a straight simulation, zero allocations on the warmed hot path |
| Core tests in Unity | `Unity/Assets/Tests/EditMode/CoreTests.cs` | The same `CoreChecks`, run on Unity's Mono runtime |
| Session lifecycle | `Unity/Assets/Tests/PlayMode/SessionLifecycleTests.cs` | `GameManager` restart, disable and scene unload release the native session and its port |
| Core-only install | `Validation/CorePackageTests.cs` | The GekkoNet package's session core runs with no SharedGame, VectorWar, UI or Input System |
| Native ABI | `Validation/native_abi.cpp` | Compile-time layout checks of the native structs against `gekkonet.h` |
| Unity Transport sample | `Validation/Transport/Tests` | Adapter datagram exchange (including fragmented payloads), sender address, socket ownership and cleanup, callback error containment |

Single-process tests catch non-determinism inside the simulation: state missing from snapshots, uninitialised fields, order-dependent logic. Differences between machines, runtimes or compilers show up only when separate processes compare checksums, which is what the network runs below do: every peer must report the same checksum at the same frame, and the native session reports any desync it detects.

## Commands

```powershell
dotnet run --project Validation/SharedGame.Validation.csproj -c Release   # core harness
./scripts/check-packages.ps1                                              # metas, native provenance, versions

# Unity test runs in an isolated copy under build/ (the main project is not modified)
./scripts/validate-unity.ps1 -Name edit
./scripts/validate-unity.ps1 -Name play -PlayMode
./scripts/validate-unity.ps1 -Name core -CoreOnly

# Multi-process .NET peers through a fixed-seed UDP proxy
./scripts/run-network-validation.ps1 -Players 4 -Spectator -LateJoin -BasePort 30100
./scripts/run-network-validation.ps1 -Players 4 -LeavePlayer 3 -BasePort 31100
./scripts/run-network-validation.ps1 -Players 2 -Impaired -BasePort 29900

# Built VectorWar players (build first with -executeMethod VectorWarBuild.BuildMono / BuildIL2CPP)
./scripts/run-player-validation.ps1 -Players 4 -CrossBackend -Spectator -LateJoin -BasePort 32700
./scripts/run-player-validation.ps1 -Players 2 -CrossBackend -Impaired -BasePort 32900

# Unity Transport sample
./scripts/build-transport-example.ps1 -Backend Mono
./scripts/run-transport-validation.ps1 -Players 4 -Spectator -LateJoin
```

Use distinct base ports per run so a failed run's ports and logs stay inspectable. Build outputs go to the ignored `build/` folder. `-CrossBackend` mixes Mono and IL2CPP players in one session, the closest local check of cross-runtime determinism.

## Not covered

- Platforms other than Windows x64.
- Visual review of the session UI, diagnostics panel and overlays (no automated UI tests).
- Long-running memory, CPU and bandwidth baselines.
