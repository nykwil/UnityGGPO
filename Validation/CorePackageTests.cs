using System;
using System.Buffers.Binary;
using NUnit.Framework;
using SharedGame;
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
    [Test] public void CoreRunsWithoutExampleOrUI() {
        using (var local = new LocalRunner(new CounterGame(), new Neutral())) {
            for (int i = 0; i < 1000; i++) local.Tick();
            Assert.That(local.Game.FrameNumber, Is.EqualTo(1000));
        }
        using (var stress = new GekkoRunner(new CounterGame(), new Neutral(), new SessionOptions { Mode = SessionMode.Stress })) {
            for (int i = 0; i < 1000; i++) stress.Tick();
            Assert.That(stress.Game.FrameNumber, Is.EqualTo(1000));
        }
    }
}
