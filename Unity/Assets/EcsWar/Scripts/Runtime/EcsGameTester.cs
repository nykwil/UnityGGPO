using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EcsWar {

    public class EcsGameTester : MonoBehaviour {
        public bool tick;
        public bool alwaysTick = true;
        public EcsSceneInfo ecsSceneInfo;

        private List<NativeArray<byte>> states;
        private EcsGame game;

        private void Awake() {
            game = new EcsGame(ecsSceneInfo);
            states = new List<NativeArray<byte>>();
        }

        private long[] inputs = new long[2];

        private void FixedUpdate() {
            if (alwaysTick || tick) {
                tick = false;
                inputs[0] = game.ReadInputs(0);
                inputs[1] = game.ReadInputs(1);
                game.Update(inputs, 0);
            }
        }

        private void OnGUI() {
            GUILayout.BeginVertical();
            for (int index = 0; index < states.Count; ++index) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restore " + index)) {
                    game.FromBytes(states[index]);
                }
                if (GUILayout.Button("Delete " + index)) {
                    game.FreeBytes(states[index]);
                    states.RemoveAt(index);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Save")) {
                states.Add(game.ToBytes());
            }
            alwaysTick = GUILayout.Toggle(alwaysTick, "Always Tick");
            if (GUILayout.Button("Tick")) {
                tick = true;
            }
        }

        private void OnDestroy() {
            foreach (var v in states) {
                game.FreeBytes(v);
            }
        }
    }
}