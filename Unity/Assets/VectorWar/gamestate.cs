using Sirenix.Serialization;
using System;
using Unity.Collections;
using UnityEngine;

namespace VectorWar {
    using static VWConstants;

    [Serializable]
    public struct Bullet {
        public bool active;
        public Vector2 position;
        public Vector2 velocity;
    };

    [Serializable]
    public class Ship {
        public Vector2 position;
        public Vector2 velocity;
        public int radius;
        public float heading;
        public int health;
        public int speed;
        public int cooldown;
        public Bullet[] bullets = new Bullet[MAX_BULLETS];
        public int score;
    };

    [Serializable]
    public class GameState {
        public int _framenumber;
        public Ship[] _ships;

        public static event Action<string> OnLog = (string s) => { };

        [NonSerialized]
        public readonly Rect _bounds = new Rect(0, 0, 640, 480);

        public static NativeArray<byte> ToBytes(GameState gs) {
            var bytes = SerializationUtility.SerializeValue(gs, DataFormat.Binary);
            return new NativeArray<byte>(bytes, Allocator.Persistent);
        }

        public static GameState FromBytes(NativeArray<byte> bytes) {
            return SerializationUtility.DeserializeValue<GameState>(bytes.ToArray(), DataFormat.Binary);
        }

        static float degtorad(float deg) {
            return PI * deg / 180;
        }

        static float distance(Vector2 lhs, Vector2 rhs) {
            float x = rhs.x - lhs.x;
            float y = rhs.y - lhs.y;
            return Mathf.Sqrt(x * x + y * y);
        }

        /*
         * InitGameState --
         *
         * Initialize our game state.
         */

        public void Init(int num_players) {
            var w = _bounds.xMax - _bounds.xMin;
            var h = _bounds.yMax - _bounds.yMin;
            var r = h / 4;

            _framenumber = 0;
            _ships = new Ship[num_players];
            for (int i = 0; i < _ships.Length; i++) {
                _ships[i] = new Ship();
                int heading = i * 360 / num_players;
                float cost, sint, theta;

                theta = (float)heading * PI / 180;
                cost = Mathf.Cos(theta);
                sint = Mathf.Sin(theta);

                _ships[i].position.x = (w / 2) + r * cost;
                _ships[i].position.y = (h / 2) + r * sint;
                _ships[i].heading = (heading + 180) % 360;
                _ships[i].health = STARTING_HEALTH;
                _ships[i].radius = SHIP_RADIUS;
            }
        }

        public void GetShipAI(int i, out float heading, out float thrust, out int fire) {
            heading = (_ships[i].heading + 5) % 360;
            thrust = 0;
            fire = 0;
        }

        public void ParseShipInputs(ulong inputs, int i, out float heading, out float thrust, out int fire) {
            Ship ship = _ships[i];

            OnLog($"parsing ship {i} inputs: {inputs}.\n");

            if ((inputs & INPUT_ROTATE_RIGHT) != 0) {
                heading = (ship.heading - ROTATE_INCREMENT) % 360;
            }
            else if ((inputs & INPUT_ROTATE_LEFT) != 0) {
                heading = (ship.heading + ROTATE_INCREMENT + 360) % 360;
            }
            else {
                heading = ship.heading;
            }

            if ((inputs & INPUT_THRUST) != 0) {
                thrust = SHIP_THRUST;
            }
            else if ((inputs & INPUT_BREAK) != 0) {
                thrust = -SHIP_THRUST;
            }
            else {
                thrust = 0;
            }
            fire = (int)(inputs & INPUT_FIRE);
        }

        public void MoveShip(int index, float heading, float thrust, int fire) {
            Ship ship = _ships[index];

            OnLog($"calculation of new ship coordinates: (thrust:{thrust} heading:{heading}).\n");

            ship.heading = heading;

            if (ship.cooldown == 0) {
                if (fire != 0) {
                    OnLog("firing bullet.\n");
                    for (int i = 0; i < ship.bullets.Length; i++) {
                        float dx = Mathf.Cos(degtorad(ship.heading));
                        float dy = Mathf.Sin(degtorad(ship.heading));
                        if (!ship.bullets[i].active) {
                            ship.bullets[i].active = true;
                            ship.bullets[i].position.x = ship.position.x + (ship.radius * dx);
                            ship.bullets[i].position.y = ship.position.y + (ship.radius * dy);
                            ship.bullets[i].velocity.x = ship.velocity.x + (BULLET_SPEED * dx);
                            ship.bullets[i].velocity.y = ship.velocity.y + (BULLET_SPEED * dy);
                            ship.cooldown = BULLET_COOLDOWN;
                            break;
                        }
                    }
                }
            }

            if (thrust != 0) {
                float dx = thrust * Mathf.Cos(degtorad(heading));
                float dy = thrust * Mathf.Sin(degtorad(heading));

                ship.velocity.x += dx;
                ship.velocity.y += dy;
                float mag = Mathf.Sqrt(ship.velocity.x * ship.velocity.x +
                                 ship.velocity.y * ship.velocity.y);
                if (mag > SHIP_MAX_THRUST) {
                    ship.velocity.x = (ship.velocity.x * SHIP_MAX_THRUST) / mag;
                    ship.velocity.y = (ship.velocity.y * SHIP_MAX_THRUST) / mag;
                }
            }
            OnLog($"new ship velocity: (dx:{ship.velocity.x} dy:{ship.velocity.y}).\n");

            ship.position.x += ship.velocity.x;
            ship.position.y += ship.velocity.y;
            OnLog($"new ship position: (dx:{ship.position.x} dy:{ship.position.y}).\n");

            if (ship.position.x - ship.radius < _bounds.xMin ||
                ship.position.x + ship.radius > _bounds.xMax) {
                ship.velocity.x *= -1;
                ship.position.x += (ship.velocity.x * 2);
            }
            if (ship.position.y - ship.radius < _bounds.yMin ||
                ship.position.y + ship.radius > _bounds.yMax) {
                ship.velocity.y *= -1;
                ship.position.y += (ship.velocity.y * 2);
            }
            for (int i = 0; i < ship.bullets.Length; i++) {
                if (ship.bullets[i].active) {
                    ship.bullets[i].position.x += ship.bullets[i].velocity.x;
                    ship.bullets[i].position.y += ship.bullets[i].velocity.y;
                    if (ship.bullets[i].position.x < _bounds.xMin ||
                        ship.bullets[i].position.y < _bounds.yMin ||
                        ship.bullets[i].position.x > _bounds.xMax ||
                        ship.bullets[i].position.y > _bounds.yMax) {
                        ship.bullets[i].active = false;
                    }
                    else {
                        for (int j = 0; j < _ships.Length; j++) {
                            var other = _ships[j];
                            if (distance(ship.bullets[i].position, other.position) < other.radius) {
                                ship.score++;
                                other.health -= BULLET_DAMAGE;
                                ship.bullets[i].active = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Update(ulong[] inputs, int disconnect_flags) {
            _framenumber++;
            for (int i = 0; i < _ships.Length; i++) {
                float thrust, heading;
                int fire;

                if ((disconnect_flags & (1 << i)) != 0) {
                    GetShipAI(i, out heading, out thrust, out fire);
                }
                else {
                    ParseShipInputs(inputs[i], i, out heading, out thrust, out fire);
                }
                MoveShip(i, heading, thrust, fire);

                if (_ships[i].cooldown != 0) {
                    _ships[i].cooldown--;
                }
            }
        }
    }
}
