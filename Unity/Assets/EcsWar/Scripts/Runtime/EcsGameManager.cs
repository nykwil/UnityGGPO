using SharedGame;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityGGPO;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public EcsSceneInfo ecsSceneInfo;
        private IGameRunner runner;

        private List<NativeArray<byte>> states = new List<NativeArray<byte>>();
        private List<int> stateFrames = new List<int>();

        public override void StartLocalGame() {
            runner = new LocalRunner(new EcsGame(ecsSceneInfo));
            StartGame(runner);
        }

        public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            runner = new GGPORunner("ecsgame", new EcsGame(ecsSceneInfo), perfPanel);
            ((GGPORunner)runner).Init(connections, playerIndex);
            StartGame(runner);
        }

        private int tick;

        protected override void Update() {
            if (IsRunning) {
                ++tick;

                if (tick % 3 == 0) {
                    RestoreLast();
                }
                else {
                    Save();
                }
            }
            base.Update();
        }

        public void Save() {
            while (states.Count > 8) {
                runner.Game.FreeBytes(states[0]);
                states.RemoveAt(0);
                stateFrames.RemoveAt(0);
            }
            states.Add(runner.Game.ToBytes());
            stateFrames.Add(targetFrame);
        }

        private void RestoreLast() {
            runner.Game.FromBytes(states[states.Count - 1]);
            targetFrame = stateFrames[states.Count - 1];
        }

        private void OnGUI() {
            GUILayout.BeginVertical();
            for (int index = 0; index < states.Count; ++index) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restore " + index)) {
                    runner.Game.FromBytes(states[index]);
                }
                if (GUILayout.Button("Delete " + index)) {
                    runner.Game.FreeBytes(states[index]);
                    states.RemoveAt(index);
                    stateFrames.RemoveAt(index);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Save")) {
                states.Add(runner.Game.ToBytes());
                stateFrames.Add(targetFrame);
            }
        }
    }
}