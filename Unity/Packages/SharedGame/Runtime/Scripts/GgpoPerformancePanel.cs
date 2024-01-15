using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;

namespace SharedGame {

    [ExecuteInEditMode]
    public class GgpoPerformancePanel : MonoBehaviour, IPerfUpdate {
        public bool show;

        public Rect fairnessRect;
        public Rect networkRect;
        public Rect labelRect;

        public bool drawGraphs;
        public bool drawGraphBackground;

        public bool resetTimers;
        public bool stallHack;
        public bool random;

        public int _num_players = 2;
        public int step = 1;
        public int textUpdateMs = 500;
        public int pointsToDraw = 500;
        public int stallFrames = 3;

        private const int MAX_GRAPH_SIZE = 4096;
        private const int MAX_FAIRNESS = 20;
        private const int MAX_PLAYERS = 4;

        private string networkLag;
        private string frameLag;
        private string bandwidth;
        private string localAhead;
        private string remoteAhead;

        private int _last_text_update_time;
        private int _first_graph_index = 0;
        private int _graph_size = 0;
        private int[][] _ping_graph;
        private int[][] _local_fairness_graph;
        private int[][] _remote_fairness_graph;
        private int[] _fairness_graph;
        private int[] _remote_queue_graph;
        private int[] _send_queue_graph;

        private Color[] _player_colours = new Color[] { Color.red, Color.blue, Color.green, Color.magenta };
        private Color[] _other_colours = new Color[] { Color.yellow, Color.cyan, Color.white, Color.grey };
        private Color _background_colour = Color.gray;

        private List<Vector2> pt = new List<Vector2>();

        public void Setup() {
            _fairness_graph = new int[MAX_GRAPH_SIZE];
            _remote_queue_graph = new int[MAX_GRAPH_SIZE];
            _send_queue_graph = new int[MAX_GRAPH_SIZE];

            _ping_graph = new int[MAX_PLAYERS][];
            _local_fairness_graph = new int[MAX_PLAYERS][];
            _remote_fairness_graph = new int[MAX_PLAYERS][];

            for (int i = 0; i < MAX_PLAYERS; ++i) {
                _ping_graph[i] = new int[MAX_GRAPH_SIZE];
                _local_fairness_graph[i] = new int[MAX_GRAPH_SIZE];
                _remote_fairness_graph[i] = new int[MAX_GRAPH_SIZE];
            }
        }

        private void DrawGraph(Rect di, Color pen, int[] graph, int count, int min, int max) {
            pt.Clear();

            for (int i = 0; i < count; i += step) {
                float y = Mathf.InverseLerp(min, max, graph[Utils.nmod(_first_graph_index - i, MAX_GRAPH_SIZE)]);

                pt.Add(new Vector2(Mathf.Lerp(di.xMin, di.xMax, (float)i / count),
                    Mathf.Lerp(di.yMin, di.yMax, y)));
            }

            GUIHelper.DrawLinesFast(pt.ToArray(), pen);
        }

        private void DrawGrid(Rect di) {
            if (drawGraphBackground) {
                GUIHelper.DrawRect(di, _background_colour);
            }
        }

        private void draw_network_graph_control(Rect di) {
            GUI.Label(new Rect(new Vector2(di.xMin, di.yMin - 20f), new Vector2(di.width, 50f)), "Network");

            DrawGrid(di);
            for (int i = 0; i < _num_players; i++) {
                DrawGraph(di, _player_colours[i], _ping_graph[i], pointsToDraw, 0, 500);
            }
            DrawGraph(di, _other_colours[0], _remote_queue_graph, pointsToDraw, 0, 14);
            DrawGraph(di, _other_colours[1], _send_queue_graph, pointsToDraw, 0, 14);
        }

        private void draw_fairness_graph_control(Rect di) {
            GUI.Label(new Rect(new Vector2(di.xMin, di.yMin - 20f), new Vector2(di.width, 50f)), "Fairness");

            DrawGrid(di);
            for (int i = 0; i < _num_players; i++) {
                DrawGraph(di, _player_colours[i], _remote_fairness_graph[i], pointsToDraw, -MAX_FAIRNESS, MAX_FAIRNESS);
            }
            DrawGraph(di, _other_colours[0], _fairness_graph, pointsToDraw, -MAX_FAIRNESS, MAX_FAIRNESS);
        }

        public void ggpoutil_perfmon_update(GGPONetworkStats[] statss) {
            int num_players = statss.Length;

            _first_graph_index = (_first_graph_index + 1) % MAX_GRAPH_SIZE;

            for (int j = 0; j < num_players; j++) {
                UpdateStats(_first_graph_index, j, statss[j]);
            }
        }

        private void UpdateStats(int i, int j, GGPONetworkStats stats) {
            /*
             * Random graphs
             */
            if (j == 0) {
                _remote_queue_graph[i] = stats.recv_queue_len;
                _send_queue_graph[i] = stats.send_queue_len;
            }

            /*
             * Ping
             */
            _ping_graph[j][i] = stats.ping;

            /*
             * Frame Advantage
             */
            _local_fairness_graph[j][i] = stats.local_frames_behind;
            _remote_fairness_graph[j][i] = stats.remote_frames_behind;
            if (stats.local_frames_behind < 0 && stats.remote_frames_behind < 0) {
                /*
                 * Both think it's unfair (which, ironically, is fair).  Scale both and subtrace.
                 */
                _fairness_graph[i] = Mathf.Abs(Mathf.Abs(stats.local_frames_behind) - Mathf.Abs(stats.remote_frames_behind));
            }
            else if (stats.local_frames_behind > 0 && stats.remote_frames_behind > 0) {
                /*
                 * Impossible!  Unless the network has negative transmit time.  Odd....
                 */
                _fairness_graph[i] = 0;
            }
            else {
                /*
                 * They disagree.  Add.
                 */
                _fairness_graph[i] = Mathf.Abs(stats.local_frames_behind) + Mathf.Abs(stats.remote_frames_behind);
            }

            int now = Utils.TimeGetTime();
            if (now > _last_text_update_time + textUpdateMs) {
                networkLag = $"Network Lag: {stats.ping} ms";
                frameLag = $"Frame Lag: {((stats.ping != 0) ? stats.ping * 60f / 1000f : 0f)} frames";
                bandwidth = $"Bandwidth: {stats.kbps_sent / 8f} kilobytes/sec";
                localAhead = $"Local Ahead: {stats.local_frames_behind} frames";
                remoteAhead = $"Remote Ahead: {stats.remote_frames_behind} frames";
                _last_text_update_time = now;
            }
        }

        private void RandomGraph(int[] graph, int size, int min, int max) {
            for (int i = 0; i < size; ++i) {
                graph[i] = UnityEngine.Random.Range(min, max);
            }
        }
        private void Update() {
            if (stallHack) {
                stallHack = false;
                int ms = (int)(1000f * stallFrames / 60f);
                Utils.Sleep(ms);
            }
            if (resetTimers) {
                resetTimers = false;
                GameManager.Instance.ResetTimers();
            }

            if (random) {
                random = false;
                Setup();

                _graph_size = MAX_GRAPH_SIZE;
                for (int i = 0; i < MAX_PLAYERS; ++i) {
                    _graph_size = MAX_GRAPH_SIZE;
                    RandomGraph(_ping_graph[i], _graph_size, 0, 500);
                    RandomGraph(_local_fairness_graph[i], _graph_size, -MAX_FAIRNESS, MAX_FAIRNESS);
                    RandomGraph(_remote_fairness_graph[i], _graph_size, -MAX_FAIRNESS, MAX_FAIRNESS);
                }

                RandomGraph(_remote_queue_graph, _graph_size, 0, 14);
                RandomGraph(_send_queue_graph, _graph_size, 0, 14);
                RandomGraph(_fairness_graph, _graph_size, -MAX_FAIRNESS, MAX_FAIRNESS);
            }

        }

        public void OnGUI() {
            var matrix = GUI.matrix;

            if (Screen.width > 0 && Screen.height > 0) {
                var ratio = Screen.width / 800f;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ratio, ratio, 1.0f));
            }

            if (show && _remote_queue_graph != null && _remote_queue_graph.Length == MAX_GRAPH_SIZE) {
                GUILayout.BeginArea(labelRect);
                GUILayout.Label(networkLag);
                GUILayout.Label(frameLag);
                GUILayout.Label(bandwidth);
                GUILayout.Label(localAhead);
                GUILayout.Label(remoteAhead);
                GUILayout.EndArea();

                if (drawGraphs) {
                    draw_fairness_graph_control(fairnessRect);
                    draw_network_graph_control(networkRect);
                }
            }

            GUI.matrix = matrix;
        }
    }
}