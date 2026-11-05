using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using SharedGame.UI;
using Nykwil.GekkoNet.Session;
namespace SharedGame.Diagnostics {
    public sealed class DiagnosticsPanel : MonoBehaviour {
        public GameManager manager;
        public SharedGameUI controls;
        public PanelRenderer document;
        Label text;
        HistoryGraph graph;
        VisualElement root;
        int uiVersion = -1;
        void OnEnable() {
            controls.DiagnosticsVisibilityChanged += SetVisible;
            manager.OnDiagnostics += Refresh;
            manager.OnRunningChanged += RunningChanged;
            document.RegisterUIReloadCallback(OnUIReload);
        }
        void OnUIReload(PanelRenderer renderer, VisualElement element, int version) {
            if (graph != null && root == element && uiVersion == version) return;
            graph?.RemoveFromHierarchy(); root = element; uiVersion = version;
            text = root.Q<Label>("metrics");
            graph = new HistoryGraph(); graph.AddToClassList("graph");
            root.Q("graphHost").Add(graph);
            SetVisible(controls.DiagnosticsVisible);
            if (manager.IsRunning) Refresh(manager.Runner.Diagnostics); else RunningChanged(false);
        }
        void OnDisable() {
            controls.DiagnosticsVisibilityChanged -= SetVisible;
            manager.OnDiagnostics -= Refresh;
            manager.OnRunningChanged -= RunningChanged;
            document.UnregisterUIReloadCallback(OnUIReload);
            graph?.RemoveFromHierarchy();
            graph = null; text = null; root = null;
        }
        void RunningChanged(bool running) {
            if (!running && graph != null) { graph.Data = null; text.text = "Session stopped"; graph.MarkDirtyRepaint(); }
        }
        void SetVisible(bool visible) { if (root != null) root.Q("diagnostics").EnableInClassList("hidden", !visible); }
        void Refresh(SessionDiagnostics data) {
            if (graph == null) return;
            graph.Data = data;
            if (!controls.DiagnosticsVisible) return;
            var sample = data.Current;
            var builder = new StringBuilder(384);
            builder.AppendLine($"Frame {sample.Frame} · checksum {sample.Checksum:X8} (last save)");
            builder.AppendLine($"Simulation/network update CPU: {sample.SimulationMs:F3}/{sample.NetworkMs:F3} ms");
            if (data.NativeMetricsAvailable) {
                builder.AppendLine($"Advantage {sample.FramesAhead:F1}f · spectator buffered {sample.SpectatorBufferedFrames}f");
            } else builder.AppendLine("Native network metrics: unavailable");
            if (data.RollbackMetricsAvailable) {
                builder.AppendLine($"Rollback count {sample.Rollback.TotalCount}");
                builder.AppendLine($"Depth last/avg/max {sample.Rollback.LastFrames}/{sample.Rollback.AverageFrames:F1}/{sample.Rollback.MaxFrames}");
                builder.AppendLine($"Stalls {sample.Rollback.TotalStalls}");
            } else builder.AppendLine("Native rollback metrics: unavailable");
            for (int i = 0; i < data.Network.Length; i++) {
                if (!data.NetworkAvailable[i]) { builder.AppendLine($"P{i} ping/jitter/bandwidth: unavailable"); continue; }
                var network = data.Network[i];
                builder.AppendLine($"P{i}: ping {network.AveragePing:F1}ms · jitter {network.Jitter:F1}ms");
                builder.AppendLine($"  TX/RX {network.KbSent:F2}/{network.KbReceived:F2} KB/s");
            }
            builder.AppendLine($"Desyncs {data.DesyncCount} · history {data.Count}/{SessionDiagnostics.HistoryCapacity}");
            text.text = builder.ToString(); graph.MarkDirtyRepaint();
        }
        sealed class HistoryGraph : VisualElement {
            public SessionDiagnostics Data;
            public HistoryGraph() { generateVisualContent += Draw; }
            void Draw(MeshGenerationContext context) {
                if (Data == null || Data.Count < 2) return;
                var painter = context.painter2D;
                painter.strokeColor = new Color(.3f, .9f, .8f); painter.lineWidth = 2;
                painter.BeginPath();
                for (int i = 0; i < Data.Count; i++) {
                    int index = (Data.Next - Data.Count + i + SessionDiagnostics.HistoryCapacity) % SessionDiagnostics.HistoryCapacity;
                    float value = Mathf.Clamp01((float)Data.History[index].SimulationMs / 16.667f);
                    var point = new Vector2(i * contentRect.width / (Data.Count - 1), (1 - value) * contentRect.height);
                    if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
                }
                painter.Stroke();
            }
        }
    }
}
