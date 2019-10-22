using System;
using UnityEngine;

public class GGPOPerformance : MonoBehaviour {
    const int MAX_GRAPH_SIZE = 4096;
    const int MAX_PLAYERS = 4;
    int _last_text_update_time;
    int _num_players;
    int _first_graph_index = 0;
    int _graph_size = 0;
    int[,] _ping_graph = new int[MAX_PLAYERS, MAX_GRAPH_SIZE];
    int[,] _local_fairness_graph = new int[MAX_PLAYERS, MAX_GRAPH_SIZE];
    int[,] _remote_fairness_graph = new int[MAX_PLAYERS, MAX_GRAPH_SIZE];
    int[] _fairness_graph = new int[MAX_GRAPH_SIZE];
    int[] _remote_queue_graph = new int[MAX_GRAPH_SIZE];
    int[] _send_queue_graph = new int[MAX_GRAPH_SIZE];

    int IDC_NETWORK_LAG = 0;
    int IDC_FRAME_LAG = 1;
    int IDC_BANDWIDTH = 2;
    int IDC_LOCAL_AHEAD = 3;
    int IDC_REMOTE_AHEAD = 4;

    public void ggpoutil_perfmon_init() {
        throw new NotImplementedException();
    }

    public void ggpoutil_perfmon_update(int ggpo, int[] players, int num_players) {
        int i;

        _num_players = num_players;

        if (_graph_size < MAX_GRAPH_SIZE) {
            i = _graph_size++;
        }
        else {
            i = _first_graph_index;
            _first_graph_index = (_first_graph_index + 1) % MAX_GRAPH_SIZE;
        }

        for (int j = 0; j < num_players; j++) {
            GGPO.ggpo_get_network_stats(ggpo, players[j], out var stats);

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
            _ping_graph[j, i] = stats.ping;

            /*
             * Frame Advantage
             */
            _local_fairness_graph[j, i] = stats.local_frames_behind;
            _remote_fairness_graph[j, i] = stats.remote_frames_behind;
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

            int now = (int)(Time.time * 1000f);
            if (stats != null && now > _last_text_update_time + 500) {
                SetWindowTextA(IDC_NETWORK_LAG, $"{stats.ping} ms");
                SetWindowTextA(IDC_FRAME_LAG, $"{((stats.ping != 0) ? stats.ping * 60f / 1000f : 0f)} frames");
                SetWindowTextA(IDC_BANDWIDTH, $"{stats.kbps_sent / 8f} kilobytes/sec");
                SetWindowTextA(IDC_LOCAL_AHEAD, $"{stats.local_frames_behind} frames");
                SetWindowTextA(IDC_REMOTE_AHEAD, $"{stats.remote_frames_behind} frames");
                _last_text_update_time = now;
            }
        }

        //InvalidateRect(GetDlgItem(_dialog, IDC_FAIRNESS_GRAPH), NULL, FALSE);
        // InvalidateRect(GetDlgItem(_dialog, IDC_NETWORK_GRAPH), NULL, FALSE);
    }

    private void SetWindowTextA(int iDC_NETWORK_LAG, string v) {
        throw new NotImplementedException();
    }
}
