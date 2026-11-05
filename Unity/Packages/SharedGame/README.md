# SharedGame

SharedGame owns game contracts, runners, pacing and session lifecycle. It has no dependency on VectorWar, Collections, Logging, Burst or Input System. It depends on GekkoNet for Unity.

## Contract

```csharp
public interface IGame {
    int PlayerCount { get; }
    int FrameNumber { get; }
    int SnapshotSize { get; }
    void SaveState(Span<byte> destination);
    void LoadState(ReadOnlySpan<byte> source);
    void AdvanceFrame(ReadOnlySpan<long> inputs);
}
public interface ILocalInputSource {
    // frame is the latest simulated frame when the input is sampled.
    long ReadInput(int controllerId, int frame);
}
```

Use a fixed-size, versioned little-endian snapshot containing every simulation variable. The destination belongs to the caller; write exactly SnapshotSize bytes. Do not retain native event pointers or snapshot spans. Reject incompatible snapshots before modifying state. Input bits are game-defined 64-bit integers. Keep input devices and presentation effects out of simulation state.

### A complete minimal game

```csharp
using System;
using System.Buffers.Binary;
using Nykwil.GekkoNet.Session;
using SharedGame;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SliderGame : IGame {
    const int Version = 1;
    readonly int[] positions;
    public SliderGame(int players) { positions = new int[players]; }
    public int PlayerCount => positions.Length;
    public int FrameNumber { get; private set; }
    public int SnapshotSize => 8 + 4 * PlayerCount; // version, frame, positions
    public int Position(int player) => positions[player];

    public void AdvanceFrame(ReadOnlySpan<long> inputs) {
        FrameNumber++;
        for (int i = 0; i < positions.Length; i++) positions[i] += (int)inputs[i];
    }
    public void SaveState(Span<byte> destination) {
        BinaryPrimitives.WriteInt32LittleEndian(destination, Version);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(4), FrameNumber);
        for (int i = 0; i < positions.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(8 + 4 * i), positions[i]);
    }
    public void LoadState(ReadOnlySpan<byte> source) {
        if (source.Length != SnapshotSize || BinaryPrimitives.ReadInt32LittleEndian(source) != Version)
            throw new ArgumentException("Incompatible snapshot.");
        FrameNumber = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4));
        for (int i = 0; i < positions.Length; i++)
            positions[i] = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(8 + 4 * i));
    }
}

public sealed class SliderInput : ILocalInputSource {
    public long ReadInput(int controllerId, int frame) {
        var keyboard = Keyboard.current;
        if (keyboard == null) return 0;
        return (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
    }
}

public sealed class SliderManager : GameManager {
    protected override IGame CreateGame(int players) => new SliderGame(players);
    protected override ILocalInputSource CreateInputSource() => new SliderInput();
}

public sealed class SliderView : MonoBehaviour {
    public SliderManager manager;
    public Transform[] markers;
    void OnEnable() => manager.OnStateChanged += Present;
    void OnDisable() => manager.OnStateChanged -= Present;
    void Present() {
        var game = (SliderGame)manager.Runner.Game;
        for (int i = 0; i < game.PlayerCount && i < markers.Length; i++)
            markers[i].localPosition = new Vector3(game.Position(i) * 0.01f, i);
    }
}
```

Start a session without any UI:

```csharp
manager.StartSession(new SessionOptions {
    Mode = SessionMode.Peer,
    Players = new[] {
        new SessionEndpoint { Address = "127.0.0.1", Port = 7000 },
        new SessionEndpoint { Address = "127.0.0.1", Port = 7001 }
    },
    LocalPlayerIndex = 0
});
manager.StopSession();
```

Import Rollback Duel for the same pattern with the session UI wired up.

### Determinism rules

Every peer must produce bit-identical state from the same inputs:

- Simulate with integers or fixed-point math. Avoid `float`/`double`, `Mathf`, `Time.deltaTime` and `UnityEngine.Random` in `AdvanceFrame`; seed any RNG from simulation state and save it in the snapshot.
- Iterate in a stable order. Avoid `Dictionary`/`HashSet` enumeration order, `GetHashCode` and reference identity in simulation logic.
- Put every value that affects future frames in the snapshot, and nothing presentation-only. `SessionDiagnostics` checksums and native desync detection catch omissions.
- Read inputs only from `ILocalInputSource`, never from devices inside `AdvanceFrame`.
- Run the rollback stress mode (`SessionMode.Stress`) during development; it re-simulates frames and reports desyncs.
- If saved bytes are not comparable between peers, implement `ISnapshotChecksum`; set `SessionOptions.ThrowOnDesync = false` to handle desyncs through `GekkoRunner.Desynced` instead of an exception. See the GekkoNet package README.

### Events

Subscribe a view to `OnStateChanged` and read `Runner.Game` after all update events and rollback correction. Clear presentation on `OnRunningChanged(false)`; reinitialize even if a restart uses the same player count. `OnInit` announces each new game. `OnError` and `LastError` expose runtime failures. `RestartSession` creates a fresh game from a copied configuration.

## Lifecycle

A manager creates and disposes its runner on the Unity main thread. Stop, disable, destroy, scene unload, domain reload and application exit dispose the session before the UDP adapter. Failed construction performs the same cleanup. Repeated stop is safe.

The built-in UDP adapter supports one network session per process. A second GekkoRunner fails without touching the first socket. Stress and local sessions do not own an adapter. Raw Native users must follow the same ownership rule themselves.

The scheduler runs at 60 Hz with at most four catch-up ticks per Unity update, dropping excess backlog after a long pause. It never sleeps. Input delay, prediction and timeout defaults are 2 frames, 8 frames and 5000 ms.

Register spectator endpoints separately from Players. SpectatorHostIndex defaults to 0. Registration is deferred past the player startup barrier; native state transfer synchronizes late spectators. A spectator exposes host connection status; other players' connection status is unavailable through the spectator API.

## Optional UI

Unity 6.5 (6000.5) is the minimum supported version. `Runtime/UI` contains `Nykwil.SharedGame.UI`, Session.uxml and Session.uss. Assign its manager and PanelRenderer explicitly. `Runtime/Diagnostics` contains a separate assembly and editable panel assets. Assign the same manager and basic controls. Panels require a PanelSettings asset and theme.

The session menu can be hidden during play. The Menu/Hide menu button stays on screen and never takes keyboard focus, `menuKey` (F1 by default; set None to disable) toggles it from the keyboard, and scripts can call `SetMenuVisible`/`ToggleMenu` or listen to `MenuVisibilityChanged`. Untick the serialized "Menu Visible" to start hidden. The menu reappears automatically when an error is shown or the session stops.

Controllers register versioned UI reload callbacks in OnEnable and unregister them in OnDisable. Reloading a UXML tree detaches old controls and binds the new tree; disabling/re-enabling retained content does not duplicate handlers. Diagnostics and projected player labels rebuild their UI while the simulation continues. The old Unity 6.0/UIDocument path is not retained.

The core can be installed with both optional folders omitted. Diagnostics data is collected every 100 ms regardless of panel visibility. The history ring has 300 entries (30 seconds). The panel displays ping, jitter, bandwidth, frame advantage, rollback count/depth, stalls, buffering, checksums and update CPU time, with a simulation-time graph. Unavailable native metrics are labelled unavailable. In network sessions the panel's frame and checksum are those of the latest native save, which can be a predicted frame; native desync detection compares confirmed state. Sampling diagnostics never calls `SaveState`.

UI strings/layout and view spawning allocate separately from the allocation-free simulation/input/snapshot path. Measure them separately.

## Samples and validation

Import **Rollback Duel**, then choose **GekkoNet → Create SharedGame Rollback Duel Scene**. The scene factory writes explicit asset references.

For an optional custom transport example, also install Unity Transport and import the **Unity Transport** sample. Its scene uses the same game and connection UI with a NetworkDriver-backed adapter. The core package has no Transport/Collections dependency. [Sample instructions](Samples~/UnityTransport/README.md) explain installation and the callback ownership model. Game-specific managers override GameManager.CreateTransport to supply one (returning null keeps the built-in UDP adapter); the runner owns the resulting transport. GameManager.InputOverride replaces CreateInputSource for scripted drivers such as validation players, and SessionValidationHarness provides the shared command-line validation flow.

See [repository validation](https://github.com/nykwil/UnityGGPO/blob/master/docs/VALIDATION.md) for the test suites and how to run them.
