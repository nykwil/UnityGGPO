# Changelog
All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - Unreleased
### Changed
- The session core (`IGame` and the other contracts, `GekkoRunner`, `SessionOptions`, `GameInfo`, `SessionDiagnostics`, `SnapshotChecksum`, `SessionScheduler`) moved to `com.nykwil.gekkonet` as the `Nykwil.GekkoNet.Session` assembly and namespace. Add `using Nykwil.GekkoNet.Session;` and a `Nykwil.GekkoNet.Session` assembly reference where these types are used. `GameManager`, `LocalRunner` and the UI stay in `SharedGame`.
- Diagnostics samples in network sessions show the latest native save's frame and checksum instead of saving the presented state; `SessionDiagnostics.SampleState` is removed.
- `SessionOptions.Copy()` is public.
- `ILocalInputSource.ReadInput(controllerId, frame)` receives the latest simulated frame.
- `IGameRunner.DisconnectPlayer` returns whether the local session ended; `GameManager` stops the session when it does.
- `GameManager.DisconnectPlayer` ignores players whose `CanDisconnect` is false, and `OnError` fires even when stopping a failed session throws.
- `SessionEndpoint.LoopbackRoster` rejects a proxy port unless the roster has exactly two players.
- `IGameRunner` adds `FramesAhead` (live pacing input) and `EndFrame()` (one transport flush per presentation update).
- Snapshot checksums hash 64-bit little-endian words; values differ from 0.2.0 builds, so all peers must run the same version.
- Runner timings accumulate per presentation update and reset in `Poll()`.
- `GameManager.OnStateChanged` fires only on updates that simulated at least one frame.

### Added
- `GameManager.CreateTransport` hook for custom network transports (returning null keeps the built-in UDP adapter).
- `GameManager.InputOverride` for scripted input drivers.
- `SharedGameUI` menu toggle: an always-visible, pointer-only Menu/Hide menu button, a `menuKey` shortcut (F1 by default; uses the Input System when installed, otherwise the legacy Input Manager), and `MenuVisible`, `SetMenuVisible`, `ToggleMenu` and `MenuVisibilityChanged` for scripts. Errors and stopped sessions show the menu again.
- `SessionOptions.ValidateFor`, `IsRemotePlayer`, `HostsSpectators`, `RemoteActors()` and `SessionEndpoint.LoopbackRoster`.
- `PlayerConnectionInfo.CanDisconnect`, set by runners and used by the session UI.
- `SessionValidationHarness` base class for command-line player validation.
- `SharedGameEditorUtility` (editor only) for panel settings, asset paths and UI test helpers.

### Removed
- `IGameView`, `GameInfo.SpectatorPaused` and the `SessionDiagnostics` rollback pass-through properties; read `Diagnostics.Current.Rollback` instead.

## [0.2.0] - Unreleased
### Changed
- Renamed from `unityggpo.sharedgame` to `com.nykwil.sharedgame` and rebuilt on GekkoNet: instance-owned sessions, span-based snapshots, local/peer/spectator/stress runners, bounded 60 Hz scheduling and optional UI Toolkit session and diagnostics panels.

## [1.0.1] - 2022-01-04 (`unityggpo.sharedgame`)
### Changed
- Changed the structure to match UPM standards
- Added the profiling prefab and code from the UnityGGPO plugin.
