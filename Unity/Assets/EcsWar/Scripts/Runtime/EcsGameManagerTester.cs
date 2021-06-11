using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EcsWar {

    public class EcsGameManagerTester : MonoBehaviour {
        private List<NativeArray<byte>> states = new List<NativeArray<byte>>();

        public EcsGameManager gameManager;

        public void SaveLast() {
            while (states.Count > 8) {
                gameManager.runner.Game.FreeBytes(states[0]);
                states.RemoveAt(0);
            }
            states.Add(gameManager.runner.Game.ToBytes());
        }

        public void RestoreLast() {
            gameManager.runner.Game.FromBytes(states[states.Count - 1]);
        }

        private void OnGUI() {
            GUILayout.BeginVertical();
            for (int index = 0; index < states.Count; ++index) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restore " + index)) {
                    gameManager.runner.Game.FromBytes(states[index]);
                }
                if (GUILayout.Button("Delete " + index)) {
                    gameManager.runner.Game.FreeBytes(states[index]);
                    states.RemoveAt(index);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Save")) {
                states.Add(gameManager.runner.Game.ToBytes());
            }
        }
    }
}