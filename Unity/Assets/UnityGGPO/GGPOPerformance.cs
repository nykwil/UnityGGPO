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

    const int IDC_NETWORK_LAG = 0;
    const int IDC_FRAME_LAG = 1;
    const int IDC_BANDWIDTH = 2;
    const int IDC_LOCAL_AHEAD = 3;
    const int IDC_REMOTE_AHEAD = 4;

    public void ggpoutil_perfmon_init() {
    }

    public void ggpoutil_perfmon_update(IntPtr ggpo, int[] players, int num_players) {
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
            var result = GGPO.UggGetNetworkStats(ggpo, players[j],
                out int send_queue_len,
                out int recv_queue_len,
                out int ping,
                out int kbps_sent,
                out int local_frames_behind,
                out int remote_frames_behind);

            Debug.Assert(GGPO.SUCCEEDED(result));

            /*
             * Random graphs
             */
            if (j == 0) {
                _remote_queue_graph[i] = recv_queue_len;
                _send_queue_graph[i] = send_queue_len;
            }

            /*
             * Ping
             */
            _ping_graph[j, i] = ping;

            /*
             * Frame Advantage
             */
            _local_fairness_graph[j, i] = local_frames_behind;
            _remote_fairness_graph[j, i] = remote_frames_behind;
            if (local_frames_behind < 0 && remote_frames_behind < 0) {
                /*
                 * Both think it's unfair (which, ironically, is fair).  Scale both and subtrace.
                 */
                _fairness_graph[i] = Mathf.Abs(Mathf.Abs(local_frames_behind) - Mathf.Abs(remote_frames_behind));
            }
            else if (local_frames_behind > 0 && remote_frames_behind > 0) {
                /*
                 * Impossible!  Unless the network has negative transmit time.  Odd....
                 */
                _fairness_graph[i] = 0;
            }
            else {
                /*
                 * They disagree.  Add.
                 */
                _fairness_graph[i] = Mathf.Abs(local_frames_behind) + Mathf.Abs(remote_frames_behind);
            }

            int now = Helper.TimeGetTime();
            if (now > _last_text_update_time + 500) {
                SetWindowTextA(IDC_NETWORK_LAG, $"{ping} ms");
                SetWindowTextA(IDC_FRAME_LAG, $"{((ping != 0) ? ping * 60f / 1000f : 0f)} frames");
                SetWindowTextA(IDC_BANDWIDTH, $"{kbps_sent / 8f} kilobytes/sec");
                SetWindowTextA(IDC_LOCAL_AHEAD, $"{local_frames_behind} frames");
                SetWindowTextA(IDC_REMOTE_AHEAD, $"{remote_frames_behind} frames");
                _last_text_update_time = now;
            }
        }

        //InvalidateRect(GetDlgItem(_dialog, IDC_FAIRNESS_GRAPH), NULL, FALSE);
        // InvalidateRect(GetDlgItem(_dialog, IDC_NETWORK_GRAPH), NULL, FALSE);
    }

    private void SetWindowTextA(int iDC_NETWORK_LAG, string v) {
    }
}
