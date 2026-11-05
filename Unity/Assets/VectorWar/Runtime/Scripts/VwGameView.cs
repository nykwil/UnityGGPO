using System;
using SharedGame;
using UnityEngine;
using UnityEngine.UIElements;
namespace VectorWar {
    public sealed class VwGameView : MonoBehaviour {
        public GameManager manager;
        public VwShipView shipPrefab;
        public Transform bulletPrefab;
        public PanelRenderer overlayDocument;
        public Camera worldCamera;
        VwShipView[] shipViews = Array.Empty<VwShipView>();
        Transform[][] bullets;
        Label[] labels;
        Vector2[] labelPositions;
        int[] labelHealth, labelScore;
        VwGame shownGame;
        VisualElement overlayRoot;
        int overlayVersion = -1;
        void OnEnable() {
            manager.OnStateChanged += Present;
            manager.OnRunningChanged += RunningChanged;
            overlayDocument.RegisterUIReloadCallback(OnUIReload);
            if (manager.IsRunning) Present();
        }
        void OnDisable() {
            manager.OnStateChanged -= Present;
            manager.OnRunningChanged -= RunningChanged;
            overlayDocument.UnregisterUIReloadCallback(OnUIReload);
            overlayRoot = null;
            Clear();
        }
        void OnUIReload(PanelRenderer renderer, VisualElement root, int version) {
            if (overlayRoot == root && overlayVersion == version) return;
            ClearLabels(); overlayRoot = root; overlayVersion = version;
            if (manager.IsRunning) Present();
        }
        void ClearLabels() {
            if (labels != null) foreach (var label in labels) label.RemoveFromHierarchy();
            labels = null; labelPositions = null; labelHealth = labelScore = null;
        }
        void RunningChanged(bool running) { if (!running) Clear(); }
        void Clear() {
            // Destroy is deferred to the end of the frame; hide first so a restart never shows both sets.
            foreach (var view in shipViews) if (view != null) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
            if (bullets != null) foreach (var list in bullets) foreach (var bullet in list)
                if (bullet != null) { bullet.gameObject.SetActive(false); Destroy(bullet.gameObject); }
            ClearLabels(); bullets = null; shipViews = Array.Empty<VwShipView>(); shownGame = null;
        }
        void Initialize(VwGame game) {
            Clear(); shownGame = game;
            shipViews = new VwShipView[game.PlayerCount];
            bullets = new Transform[game.PlayerCount][];
            for (int i = 0; i < game.PlayerCount; i++) {
                shipViews[i] = Instantiate(shipPrefab, transform);
                bullets[i] = new Transform[VWConstants.MAX_BULLETS];
                for (int b = 0; b < bullets[i].Length; b++) {
                    bullets[i][b] = Instantiate(bulletPrefab, transform);
                    bullets[i][b].gameObject.SetActive(false);
                }
            }
        }
        void CreateLabels(int count) {
            labels = new Label[count];
            labelPositions = new Vector2[count];
            labelHealth = new int[count];
            labelScore = new int[count];
            for (int i = 0; i < count; i++) {
                labelHealth[i] = labelScore[i] = int.MinValue;
                labels[i] = new Label();
                labels[i].AddToClassList("player-label");
                labels[i].pickingMode = PickingMode.Ignore;
                overlayRoot.Q("playerLabels").Add(labels[i]);
            }
        }
        void Present() {
            if (manager.Runner == null) return;
            var game = (VwGame)manager.Runner.Game;
            if (!ReferenceEquals(shownGame, game)) Initialize(game);
            if (overlayRoot != null && labels == null) CreateLabels(game.PlayerCount);
            for (int i = 0; i < game.PlayerCount; i++) {
                var ship = game.Ships[i]; shipViews[i].Populate(ship);
                for (int b = 0; b < bullets[i].Length; b++) {
                    var state = ship.bullets[b]; var view = bullets[i][b];
                    if (view.gameObject.activeSelf != state.active) view.gameObject.SetActive(state.active);
                    if (!state.active) continue;
                    view.position = new Vector3(FixedMath.ToFloat(state.position.x), FixedMath.ToFloat(state.position.y));
                    if (state.velocity.x != 0 || state.velocity.y != 0)
                        view.rotation = Quaternion.LookRotation(new Vector3(FixedMath.ToFloat(state.velocity.x), FixedMath.ToFloat(state.velocity.y)), Vector3.up);
                }
                if (labels == null) continue;
                // Rebuild the text only when it changes; it is the view's only per-frame allocation.
                if (ship.health != labelHealth[i] || ship.score != labelScore[i]) {
                    labelHealth[i] = ship.health; labelScore[i] = ship.score;
                    labels[i].text = "P" + i.ToString() + " · HP " + ship.health.ToString() + " · Score " + ship.score.ToString();
                }
                var panel = overlayRoot.panel;
                if (panel != null) {
                    var position = RuntimePanelUtils.CameraTransformWorldToPanel(panel, shipViews[i].transform.position, worldCamera);
                    position.y += 22;
                    // Translate is transform-only, so moving labels does not trigger a layout pass.
                    if (position != labelPositions[i]) {
                        labelPositions[i] = position;
                        labels[i].style.translate = new Translate(position.x, position.y);
                    }
                }
            }
        }
    }
}
