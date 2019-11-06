namespace VectorWar {
    using static VWConstants;

    public static class VWConstants {
        public const int MAX_SHIPS = 4;
        public const int MAX_PLAYERS = 64;
        public const int VK_UP = 0;
        public const int VK_DOWN = 0;
        public const int VK_LEFT = 0;
        public const int VK_RIGHT = 0;

        public const int INPUT_THRUST = (1 << 0);
        public const int INPUT_BREAK = (1 << 1);
        public const int INPUT_ROTATE_LEFT = (1 << 2);
        public const int INPUT_ROTATE_RIGHT = (1 << 3);
        public const int INPUT_FIRE = (1 << 4);
        public const int INPUT_BOMB = (1 << 5);
        public const int MAX_BULLETS = 30;

        public const float PI = 3.1415926f;
        public const int STARTING_HEALTH = 100;
        public const int ROTATE_INCREMENT = 3;
        public const int SHIP_RADIUS = 15;
        public const int SHIP_WIDTH = 8;
        public const int SHIP_TUCK = 3;
        public const float SHIP_THRUST = 0.06f;
        public const float SHIP_MAX_THRUST = 4.0f;
        public const float SHIP_BREAK_SPEED = 0.6f;
        public const float BULLET_SPEED = 5f;
        public const int BULLET_COOLDOWN = 8;
        public const int BULLET_DAMAGE = 10;
    }

    /*
     * nongamestate.h --
     *
     * These are other pieces of information not related to the state
     * of the game which are useful to carry around.  They are not
     * included in the GameState class because they specifically
     * should not be rolled back.
     */

    public enum PlayerConnectState {
        Connecting = 0,
        Synchronizing,
        Running,
        Disconnected,
        Disconnecting,
    };

    public struct PlayerConnectionInfo {
        public GGPOPlayerType type;
        public int handle;
        public PlayerConnectState state;
        public int connect_progress;
        public int disconnect_timeout;
        public int disconnect_start;
        public int controllerId;
    };

    public class NonGameState {
        public PlayerConnectionInfo[] players;
        public string status;

        public ChecksumInfo now;
        public ChecksumInfo periodic;

        public struct ChecksumInfo {
            public int framenumber;
            public int checksum;
        };

        public void SetConnectState(int handle, PlayerConnectState state) {
            for (int i = 0; i < players.Length; i++) {
                if (players[i].handle == handle) {
                    players[i].connect_progress = 0;
                    players[i].state = state;
                    break;
                }
            }
        }

        public void SetDisconnectTimeout(int handle, int now, int timeout) {
            for (int i = 0; i < players.Length; i++) {
                if (players[i].handle == handle) {
                    players[i].disconnect_start = now;
                    players[i].disconnect_timeout = timeout;
                    players[i].state = PlayerConnectState.Disconnecting;
                    break;
                }
            }
        }

        public void SetConnectState(PlayerConnectState state) {
            for (int i = 0; i < players.Length; i++) {
                players[i].state = state;
            }
        }

        public void UpdateConnectProgress(int handle, int progress) {
            for (int i = 0; i < players.Length; i++) {
                if (players[i].handle == handle) {
                    players[i].connect_progress = progress;
                    break;
                }
            }
        }
    }
}
