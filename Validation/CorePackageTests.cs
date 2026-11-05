using System;
using System.Buffers.Binary;
using NUnit.Framework;
using Nykwil.GekkoNet.Session;
/// <summary>Runs in a project with only com.nykwil.gekkonet installed: no SharedGame, VectorWar, UI or Input System.</summary>
public sealed class CorePackageTests {
    sealed class CounterGame : IGame {
        public int PlayerCount => 2;
        public int FrameNumber { get; private set; }
        public int SnapshotSize => 4;
        public void SaveState(Span<byte> destination) => BinaryPrimitives.WriteInt32LittleEndian(destination, FrameNumber);
        public void LoadState(ReadOnlySpan<byte> source) => FrameNumber = BinaryPrimitives.ReadInt32LittleEndian(source);
        public void AdvanceFrame(ReadOnlySpan<long> inputs) => FrameNumber++;
    }
    sealed class Neutral : ILocalInputSource { public long ReadInput(int controller, int frame) => 0; }
    [Test] public void SessionCoreRunsWithoutSharedGame() {
        var options = new SessionOptions { Mode = SessionMode.Stress };
        using (var stress = new GekkoRunner(new CounterGame(), new Neutral(), options)) {
            for (int i = 0; i < 1000; i++) stress.Tick();
            Assert.That(stress.Game.FrameNumber, Is.EqualTo(1000));
            Assert.That(stress.Diagnostics.DesyncCount, Is.Zero);
        }
        Assert.That(options.SavedStateDepth, Is.EqualTo(options.StressDistance + 2));
    }
}
