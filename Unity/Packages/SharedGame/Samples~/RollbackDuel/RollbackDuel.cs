using System;
using System.Buffers.Binary;
using SharedGame;
using UnityEngine;
using UnityEngine.UIElements;
namespace SharedGame.Samples {
    public class RollbackDuel : GameManager, ILocalInputSource {
        public PanelRenderer gameDocument;
        int direction;
        Label positions;
        double nextPresentation;
        Button left, right;
        protected virtual void OnEnable() {
            OnStateChanged += Present; OnRunningChanged += RunningChanged;
            gameDocument.RegisterUIReloadCallback(OnUIReload);
        }
        void OnUIReload(PanelRenderer renderer, VisualElement root, int version) {
            Bind(left, false); Bind(right, false); direction = 0;
            positions = root.Q<Label>("positions");
            left = root.Q<Button>("left"); right = root.Q<Button>("right");
            Bind(left, true); Bind(right, true);
            nextPresentation = 0;
            if (IsRunning) Present(); else RunningChanged(false);
        }
        void Bind(Button button, bool bind) {
            if (button == null) return;
            if (bind) {
                button.RegisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
            } else {
                button.UnregisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
                button.UnregisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
                button.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
            }
        }
        void PointerDown(PointerDownEvent e) { direction = e.currentTarget == left ? -1 : 1; ((Button)e.currentTarget).CapturePointer(e.pointerId); }
        void PointerUp(PointerUpEvent e) { direction = 0; ((Button)e.currentTarget).ReleasePointer(e.pointerId); }
        void CaptureOut(PointerCaptureOutEvent e) => direction = 0;
        protected override void OnDisable() { gameDocument.UnregisterUIReloadCallback(OnUIReload); Bind(left, false); Bind(right, false); left = right = null; positions = null; OnStateChanged -= Present; OnRunningChanged -= RunningChanged; direction = 0; base.OnDisable(); }
        void RunningChanged(bool running) { direction = 0; if (!running && positions != null) positions.text = "Session stopped"; }
        void Present() {
            if (Runner == null || positions == null || Time.unscaledTimeAsDouble < nextPresentation) return;
            nextPresentation = Time.unscaledTimeAsDouble + .1;
            var game = (DuelGame)Runner.Game;
            positions.text = $"Frame {game.FrameNumber}: {string.Join(" / ", game.Positions)}";
        }
        protected override IGame CreateGame(int count) => new DuelGame(count);
        protected override ILocalInputSource CreateInputSource() => this;
        public long ReadInput(int controllerId, int frame) => direction;
    }
    public sealed class DuelGame : IGame {
        public readonly int[] Positions;
        public int PlayerCount => Positions.Length;
        public int FrameNumber { get; private set; }
        public int SnapshotSize => 12 + PlayerCount * 4;
        public DuelGame(int count) { Positions = new int[count]; }
        public void AdvanceFrame(ReadOnlySpan<long> inputs) {
            if (inputs.Length != PlayerCount) throw new ArgumentException("Player count mismatch.");
            FrameNumber++;
            for (int i = 0; i < PlayerCount; i++) Positions[i] = (Positions[i] + (int)inputs[i] + 1000) % 1000;
        }
        public void SaveState(Span<byte> target) {
            if (target.Length != SnapshotSize) throw new ArgumentException("Snapshot size mismatch.");
            BinaryPrimitives.WriteInt32LittleEndian(target, 1);
            BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4), PlayerCount);
            BinaryPrimitives.WriteInt32LittleEndian(target.Slice(8), FrameNumber);
            for (int i = 0; i < PlayerCount; i++) BinaryPrimitives.WriteInt32LittleEndian(target.Slice(12 + i * 4), Positions[i]);
        }
        public void LoadState(ReadOnlySpan<byte> source) {
            if (source.Length != SnapshotSize || BinaryPrimitives.ReadInt32LittleEndian(source) != 1 ||
                BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4)) != PlayerCount) throw new ArgumentException("Incompatible snapshot.");
            FrameNumber = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(8));
            for (int i = 0; i < PlayerCount; i++) Positions[i] = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(12 + i * 4));
        }
    }
}
