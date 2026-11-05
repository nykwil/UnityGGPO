using System;
using System.IO;
using SharedGame;
using SharedGame.Samples.UnityTransport;
using SharedGame.Validation;
using UnityEngine;
public sealed class TransportValidationPlayer : SessionValidationHarness {
    public UnityTransportDuel manager;
    [Serializable] public sealed class Config {
        public int Players = 2, LocalPlayer, BasePort = 35000, ProxyPort;
        public bool Spectator;
        public string Output;
    }
    Config config;
    protected override GameManager Manager => manager;
    protected override string CommandLineFlag => "--transport-validation";
    protected override double TimeoutSeconds => 60;
    protected override string OutputPath => config?.Output;
    protected override SessionOptions Configure(string path) {
        config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
        return new SessionOptions {
            Mode = config.Spectator ? SessionMode.Spectator : SessionMode.Peer,
            Players = SessionEndpoint.LoopbackRoster(config.Players, config.LocalPlayer, config.BasePort, config.ProxyPort),
            LocalPlayerIndex = config.LocalPlayer, SpectatorListenPort = config.BasePort + 10,
            Spectators = new[] { new SessionEndpoint { Port = config.BasePort + 10 } }
        };
    }
    public override long ReadInput(int controller, int frame) => frame < 400 ? (frame / 15 + config.LocalPlayer) % 3 - 1 : 0;
    protected override void Observe(IGame game) {
        RecordChecksum(game, 600, 720);
        if (game.FrameNumber >= (config.Spectator ? 720 : 840)) Finish(null);
    }
}
