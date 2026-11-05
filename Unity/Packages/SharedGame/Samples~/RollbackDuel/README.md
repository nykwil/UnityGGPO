# Rollback Duel quick start

Import this sample from SharedGame in Package Manager. Run **GekkoNet → Create SharedGame Rollback Duel Scene** and enter Play mode. The generated scene stores explicit references to editable UXML/USS, a PanelSettings asset, and the shared session UI.

Use local play or stress mode to start immediately. Hold either movement button. For a network session, use the same ordered roster in two processes and different local player indices. For a spectator, register its endpoint on player 0, then use spectator mode with a separate listening port.

The sample implements IGame and ILocalInputSource. All session ownership, networking, pacing and connection UI come from SharedGame. It does not depend on VectorWar, Collections, Logging, Burst or Input System.
