using SharedGame;
using System;
using UnityEngine;

namespace VectorWar {

    public class VwGameView : MonoBehaviour, IGameView {
        public VwShipView shipPrefab;
        public Transform bulletPrefab;

        private VwShipView[] shipViews = Array.Empty<VwShipView>();
        private Transform[][] bulletLists;
        private GameManager gameManager => GameManager.Instance;

        private void ResetView(VwGameState gs) {
            var shipGss = gs._ships;
            shipViews = new VwShipView[shipGss.Length];
            bulletLists = new Transform[shipGss.Length][];

            for (int i = 0; i < shipGss.Length; ++i) {
                shipViews[i] = Instantiate(shipPrefab, transform);
                bulletLists[i] = new Transform[shipGss[i].bullets.Length];
                for (int j = 0; j < bulletLists[i].Length; ++j) {
                    bulletLists[i][j] = Instantiate(bulletPrefab, transform);
                }
            }
        }

        public void UpdateGameView(IGame game) {
            var gs = (VwGameState)game.GameState;
            var ngs = game.GameInfo;

            var shipsGss = gs._ships;
            if (shipViews.Length != shipsGss.Length) {
                ResetView(gs);
            }
            for (int i = 0; i < shipsGss.Length; ++i) {
                shipViews[i].Populate(shipsGss[i], ngs.players[i]);
                UpdateBullets(shipsGss[i].bullets, bulletLists[i]);
            }
        }

        private void UpdateBullets(Bullet[] bullets, Transform[] bulletList) {
            for (int j = 0; j < bulletList.Length; ++j) {
                bulletList[j].position = bullets[j].position;
                bulletList[j].gameObject.SetActive(bullets[j].active);
            }
        }

        private void Update() {
            if (gameManager.IsRunning) {
                UpdateGameView(gameManager.Game);
            }
        }
    }
}