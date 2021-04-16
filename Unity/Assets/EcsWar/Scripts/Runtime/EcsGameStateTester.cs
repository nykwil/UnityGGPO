using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EcsWar {

    public class EcsGameStateTester : MonoBehaviour {
        public bool tick;
        public bool alwaysTick = true;
        public EcsSceneInfo ecsSceneInfo;

        private List<NativeArray<byte>> states;
        private EcsGameState gameState;

        private void Awake() {
            gameState = new EcsGameState(ecsSceneInfo);
            states = new List<NativeArray<byte>>();
        }

        private long[] inputs = new long[2];

        private void FixedUpdate() {
            if (alwaysTick || tick) {
                tick = false;
                inputs[0] = gameState.ReadInputs(0);
                inputs[1] = gameState.ReadInputs(1);
                gameState.Update(inputs, 0);
            }
        }

        private void OnGUI() {
            GUILayout.BeginVertical();
            for (int index = 0; index < states.Count; ++index) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restore " + index)) {
                    gameState.FromBytes(states[index]);
                }
                if (GUILayout.Button("Delete " + index)) {
                    gameState.FreeBytes(states[index]);
                    states.RemoveAt(index);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Save")) {
                states.Add(gameState.ToBytes());
            }
            alwaysTick = GUILayout.Toggle(alwaysTick, "Always Tick");
            if (GUILayout.Button("Tick")) {
                tick = true;
            }
        }

        private void OnDestroy() {
            foreach (var v in states) {
                gameState.FreeBytes(v);
            }
        }
    }
}