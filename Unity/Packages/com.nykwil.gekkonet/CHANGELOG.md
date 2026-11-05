# Changelog
All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - Unreleased
### Added
- P/Invoke bindings for the GekkoNet C API pinned at 45dac7e50fd3ac2d5c94be1fafa6f0d3214a9cea: sessions, players, spectators, stress sessions, disconnects, timeouts, input delay, prediction window and statistics.
- Windows x64 native library built reproducibly with the session-terminal disconnect patch, with build provenance.
- `Nykwil.GekkoNet.Session` assembly and namespace, moved from SharedGame and free of UnityEngine references: game and input contracts, `GekkoRunner`, `SessionOptions`, `GameInfo`, `SessionDiagnostics`, `SnapshotChecksum` and `SessionScheduler`.
- `ISnapshotChecksum`, letting a game supply the checksum peers compare for desync detection.
- `SessionOptions.ThrowOnDesync`, `GekkoRunner.Desynced` and `SessionDiagnostics.LastDesync` for handling desyncs without an exception.
- `SessionOptions.SavedStateDepth`, the number of recent saves a load can restore.
- `GekkoRunner.SampleDiagnostics` reports the frame and checksum of the latest native save and never calls `SaveState` itself.
- `IGameRunner.FramesAhead` documented as the whole local-minus-remote frame gap (the native "halved" comment notwithstanding), so pacing code does not halve it again.
