using UnityEngine;

namespace VectorWar {

    public class VectorWarRenderer : MonoBehaviour {
        public VectorWarRunner vwr;
        public ShipView shipPrefab;
        public Transform bulletPrefab;
        public ShipView[] shipViews;
        public Transform[][] bulletLists;

        public string status;
        public string periodicChecksum;
        public string nowChecksum;

        private void Init() {
            var shipGss = vwr.gs._ships;
            shipViews = new ShipView[shipGss.Length];
            bulletLists = new Transform[shipGss.Length][];

            for (int i = 0; i < shipGss.Length; ++i) {
                shipViews[i] = Instantiate(shipPrefab, transform);
                bulletLists[i] = new Transform[shipGss[i].bullets.Length];
                for (int j = 0; j < bulletLists.Length; ++j) {
                    bulletLists[i][j] = Instantiate(bulletPrefab, transform);
                }
            }
        }

        void Update() {
            if (vwr.Running) {
                status = vwr.ngs.status;
                periodicChecksum = RenderChecksum(vwr.ngs.periodic);
                nowChecksum = RenderChecksum(vwr.ngs.now);

                var shipsGss = vwr.gs._ships;
                if (shipViews.Length != shipsGss.Length) {
                    Init();
                }
                for (int i = 0; i < shipsGss.Length; ++i) {
                    UpdateShipView(shipViews[i], shipsGss[i], bulletLists[i]);
                    DrawConnectState(shipViews[i], vwr.ngs.players[i]);
                }
            }
        }

        private void UpdateShipView(ShipView shipView, Ship shipGs, Transform[] bulletList) {
            shipView.Populate(shipGs);
            for (int j = 0; j < bulletLists.Length; ++j) {
                bulletList[j].position = shipGs.bullets[j].position;
                bulletList[j].gameObject.SetActive(shipGs.bullets[j].active);
            }
        }

        readonly string[] statusStrings = {
            "Connecting...",
            "Synchronizing...",
            "",
            "Disconnected."};

        void DrawConnectState(ShipView view, PlayerConnectionInfo info) {
            view.status = "";
            view.progress = -1;
            switch (info.state) {
                case PlayerConnectState.Connecting:
                    view.status = (info.type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) ? "Local Player" : "Connecting...";
                    break;

                case PlayerConnectState.Synchronizing:
                    view.progress = info.connect_progress;
                    view.status = (info.type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) ? "Local Player" : "Synchronizing...";
                    break;

                case PlayerConnectState.Disconnected:
                    view.status = "Disconnected";
                    break;

                case PlayerConnectState.Disconnecting:
                    view.status = "Waiting for player...";
                    view.progress = (Helper.TimeGetTime() - info.disconnect_start) * 100 / info.disconnect_timeout;
                    break;
            }
        }

        string RenderChecksum(NonGameState.ChecksumInfo info) {
            return string.Format("Frame: {0} Checksum: {1}", info.framenumber, info.checksum); // %04d  %08x
        }
    }
}
