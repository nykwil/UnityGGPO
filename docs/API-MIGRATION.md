# API migration

| Previous API | Replacement |
| --- | --- |
| GGPORunner / static GGPOSession | Instance-owned GekkoRunner |
| Game contracts and runner in namespace SharedGame | Namespace Nykwil.GekkoNet.Session in com.nykwil.gekkonet (assembly Nykwil.GekkoNet.Session); GameManager, LocalRunner and the UI stay in SharedGame |
| IGame.ToBytes / FromBytes / FreeBytes | SnapshotSize / SaveState(Span<byte>) / LoadState(ReadOnlySpan<byte>) |
| IGame.Update(inputs, disconnectFlags) | AdvanceFrame(ReadOnlySpan<long>); native neutral inputs handle disconnects |
| IGame.ReadInputs | Separate ILocalInputSource.ReadInput |
| IGame.GetHashCode / Checksum | FNV-1a over the complete serialized snapshot, or ISnapshotChecksum when saved bytes are not comparable between peers |
| GameManager.StartLocalGame / StartGGPOGame | StartSession(SessionOptions) |
| GameManager.Shutdown | StopSession |
| Five UpdateType modes | One bounded SessionScheduler |
| GameManager.Instance lookup | Explicit serialized GameManager references |
| Legacy connection/performance Canvas prefabs | PanelRenderer + editable UXML/USS in separate optional assemblies |
| Per-ship Canvas connection status | Projected UI Toolkit labels; centralized HUD connection progress |

Implement IGame with a fresh initial FrameNumber of 0 and 2–4 players. Snapshots must include frame counters, random seeds and all projectiles. Serialize integers explicitly in little-endian order; do not serialize object references, UI, devices or transforms. VectorWar snapshot version 1 uses Q16.16 positions/velocities and integer-degree headings.

Supply game and input factories in a GameManager subclass. Use the same ordered player roster on all peers. LocalPlayerIndex identifies the network player; LocalControllerId chooses the local input mapping independently. Spectators use a separate list and a host index, defaulting to player 0.

Subscribe views to OnStateChanged for the corrected final simulation state. Subscribe to OnRunningChanged(false) for cleanup. Restart creates a new game and runner. Disable/destroy/application exit stop automatically. Sessions and their native pointers are instance owned and thread confined.

The session core (Nykwil.GekkoNet.Session) has no UnityEngine references and can be used without SharedGame. SharedGame's core references only the GekkoNet assemblies. Optional UI and diagnostics can be omitted. VectorWar alone references the Input System.

Legacy source, prefabs and binaries have been removed after the editor asset migration and reference audit. The replacement packages run independently of the old plugin. Native disconnects are terminal for that session; restart to reconnect a player. Disconnecting the local network player stops and disposes its session.

The current minimum is Unity 6.5 (6000.5). SharedGame UI, diagnostics, VectorWar labels and both samples use PanelRenderer. Script fields named document/gameDocument/overlayDocument now reference PanelRenderer components. The VectorWar scene in this repository is already migrated; **GekkoNet → Migrate VectorWar UI Toolkit Assets** re-wires its PanelRenderer references if needed. Recreate sample scenes with their existing scene-factory menus.

Do not query rootVisualElement from OnEnable. Register a versioned UI reload callback, bind controls from the provided VisualElement, and unregister the callback and handlers when disabled. A changed UI version replaces bindings; re-enabling existing content must not add duplicate handlers. No Unity 6.0 compatibility layer is provided.
