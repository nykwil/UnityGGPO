using NUnit.Framework;
// Runs the shared CoreChecks inside the Unity runtime; CI runs the same checks under .NET (Validation/Program.cs).
public sealed class CoreTests {
    [Test] public void NativeLayoutsAndBooleans() => CoreChecks.Layouts();
    [Test] public void SnapshotsAreCompleteAndValidated() => CoreChecks.Snapshots();
    [Test] public void ConfigurationIsValidated() => CoreChecks.Options();
    [Test] public void SaveStateOnlyInNativeSaves() => CoreChecks.SaveDiscipline();
    [Test] public void DesyncReportingAndGameChecksums() => CoreChecks.Desync();
    [Test] public void FailedAndRepeatedSessionsReleaseSockets() => CoreChecks.Lifecycle();
    [Test] public void RollbackStressMatchesStraightSimulation() => CoreChecks.Stress();
    [Test] public void HotPathDoesNotAllocate() => Assert.That(CoreChecks.Allocations(), Is.Zero);
}
