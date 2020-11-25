using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace SharedGame {

    public static class Utils {

        public static T GetInterface<T>(GameObject inObj) where T : class {
            if (!typeof(T).IsInterface) {
                UnityEngine.Debug.LogError(typeof(T).ToString() + ": is not an actual interface!");
                return null;
            }

            return inObj.GetComponents<Component>().OfType<T>().FirstOrDefault();
        }
    }

    public class GameRunner : MonoBehaviour {
        public IGameView view;
        private IGame game;

        private float next;

        public static event Action<string> OnStatus;

        public static event Action<string> OnChecksum;

        public static event Action<string> OnLog;

        public static Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning => game != null;

        private void Awake() {
            view = Utils.GetInterface<IGameView>(gameObject);
            UnityEngine.Debug.Assert(view != null);
        }

        public static void LogCallback(string text) {
            OnLog?.Invoke(text);
        }

        public void Startup(IGame game) {
            this.game = game;
            game.Init();
        }

        public void DisconnectPlayer(int player) {
            if (game != null) {
                game.DisconnectPlayer(player);
            }
        }

        public void Shutdown() {
            if (game != null) {
                game.Shutdown();
                game = null;
            }
        }

        private void OnDestroy() {
            Shutdown();
        }

        private void Update() {
            if (game != null) {
                updateWatch.Start();
                var now = Time.time;
                var extraMs = Mathf.Max(0, (int)((next - now) * 1000f) - 1);
                game.Idle(extraMs);

                if (now >= next) {
                    game.RunFrame();
                    next = now + 1f / 60f;
                }
                updateWatch.Stop();

                string status = game.GetStatus(updateWatch);
                OnStatus?.Invoke(status);
                OnChecksum?.Invoke(RenderChecksum(game.ngs.periodic) + RenderChecksum(game.ngs.now));
                view.UpdateGameView(game);
            }
        }

        private string RenderChecksum(GameInfo.ChecksumInfo info) {
            return string.Format("f:{0} c:{1}", info.framenumber, info.checksum); // %04d  %08x
        }
    }
}