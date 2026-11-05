# Unity Transport example

This optional sample carries GekkoNet packets over Unity Transport's NetworkDriver. GekkoNet still owns synchronization, input delay, prediction, rollback, timeouts and spectator snapshots. Netcode for GameObjects is not required.

## Run it

1. Use Unity 6.5 or newer. Install GekkoNet for Unity and SharedGame, then install `com.unity.transport` through Package Manager. Unity 6.6 supplies its core Transport package.
2. Import **Rollback Duel**, then **Unity Transport**, from SharedGame's Samples tab. This sample extends Rollback Duel and reuses its game, controls, editable UXML/USS and the SharedGame connection screen.
3. Choose **GekkoNet → Create Unity Transport Rollback Duel Scene**. The editor tool assigns the documents and manager explicitly.
4. Build a Windows x64 player. Launch one process per player. Choose Peer mode, use the same ordered 2–4-player roster, and choose a different local player index in each process. For a local test use 127.0.0.1 with distinct ports, e.g. 7000 and 7001.
5. For a spectator, list its endpoint on the host (player 0 by default). Launch another process in Spectator mode with the same roster and its separate listening port. It can join after gameplay starts.

All participants must use this Transport example. Its wire protocol is different from the default UDP adapter. Local and stress modes use the existing runners without creating a Transport driver. Stop/restart uses the same SharedGame UI and creates a fresh session/driver.

The example enables Application.runInBackground so an unfocused peer keeps polling while you interact with another player window.

## Integration

`UnityTransportDuel` overrides GameManager.CreateTransport:

```csharp
protected override IGekkoNetworkTransport CreateTransport(SessionOptions options) => UnityTransportAdapter.Create(options);
```

The hook is called only for peer and spectator sessions and receives a validated, copied configuration. GekkoRunner owns the returned IGekkoNetworkTransport and destroys its native session before disposing the transport. Poll completes this driver's incoming job, then lets GekkoNet consume its packets. Flush sends native output once per presentation update (IGameRunner.EndFrame) and after disconnecting. No custom update strategy is needed.

The adapter binds/listens on the local roster port and talks to SessionOptions.RemoteActors(), the same roster GekkoRunner registers. Lower player indices initiate connections to higher indices, avoiding duplicate connections. Spectators initiate to their host; other endpoints are rejected. An unreliable fragmentation pipeline handles larger input-history datagrams while GekkoNet implements retransmission. Send pressure drops packets for GekkoNet to retransmit.

The pinned native adapter API has no user-data pointer. Static callbacks and rooted delegates therefore support **one UnityTransportAdapter per process**, including IL2CPP. Calls are confined to the creating thread. Exceptions are caught inside send callbacks and raised by the managed owner outside native code.

Receive storage is allocated once: 128 datagrams of up to 64 KiB (an 8 MiB payload pool), native result records, pointers and immutable address bytes. UTP packets are configured for a 1400-byte limit, excluding UDP/IP headers, and larger datagrams are reassembled by Transport. Native free callbacks acknowledge pooled records; the pool is released with the adapter. Processing is bounded to 1024 events and 32 accepted connections per poll. No native pointer is retained by game/view code.

This example uses direct reachable IPv4 UDP endpoints. Relay, matchmaking, NAT traversal, WebSockets and cross-platform native binaries are outside this example.

## Validation

Reusable repository commands:

```powershell
./scripts/build-transport-example.ps1 -Tests
./scripts/build-transport-example.ps1 -Tests -TestMode PlayMode
./scripts/build-transport-example.ps1 -Backend Mono
./scripts/build-transport-example.ps1 -Backend IL2CPP
./scripts/run-transport-validation.ps1 -Players 2 -BasePort 35000
./scripts/run-transport-validation.ps1 -Players 4 -Spectator -LateJoin -CrossBackend -BasePort 35100
./scripts/run-transport-validation.ps1 -Players 2 -Impaired -CrossBackend -BasePort 35500
```

Tests exercise native callback datagrams/address identity, expected occupied-port failure, exclusive sample ownership, repeated disposal/socket reuse and callback error containment. PlayMode verifies manager stop/disable cleanup. The sample UI uses PanelRenderer and requires Unity 6.5+. Build and multi-peer commands are in `docs/VALIDATION.md` in the repository.
