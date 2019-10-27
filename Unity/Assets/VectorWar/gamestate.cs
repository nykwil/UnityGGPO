using Sirenix.Serialization;
using System;
using Unity.Collections;
using UnityEngine;
using static Constants;

[Serializable]
public struct Position {
    public float x;
    public float y;
};

[Serializable]
public struct Velocity {
    public float dx;
    public float dy;
};

[Serializable]
public struct Bullet {
    public bool active;
    public Position position;
    public Velocity velocity;
};

[Serializable]
public class Ship {
    public Position position;
    public Velocity velocity;
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
    public Rect _bounds;
    public int _num_ships;
    public Ship[] _ships = new Ship[MAX_SHIPS];
    public int ggpo;

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

    static float distance(Position lhs, Position rhs) {
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
        _num_ships = num_players;
        for (int i = 0; i < _num_ships; i++) {
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

    public void PostInit(int ggpo) {
        this.ggpo = ggpo;
    }

    public void GetShipAI(int i, out float heading, out float thrust, out int fire) {
        heading = (_ships[i].heading + 5) % 360;
        thrust = 0;
        fire = 0;
    }

    public void ParseShipInputs(ulong inputs, int i, out float heading, out float thrust, out int fire) {
        Ship ship = _ships[i];

        GGPO.DllLog(ggpo, $"parsing ship {i} inputs: {inputs}.\n");

        if ((inputs & INPUT_ROTATE_RIGHT) != 0) {
            heading = (ship.heading + ROTATE_INCREMENT) % 360;
        }
        else if ((inputs & INPUT_ROTATE_LEFT) != 0) {
            heading = (ship.heading - ROTATE_INCREMENT + 360) % 360;
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

        GGPO.DllLog(ggpo, $"calculation of new ship coordinates: (thrust:{thrust} heading:{heading}).\n");

        ship.heading = heading;

        if (ship.cooldown == 0) {
            if (fire != 0) {
                GGPO.DllLog(ggpo, "firing bullet.\n");
                for (int i = 0; i < MAX_BULLETS; i++) {
                    float dx = Mathf.Cos(degtorad(ship.heading));
                    float dy = Mathf.Sin(degtorad(ship.heading));
                    if (!ship.bullets[i].active) {
                        ship.bullets[i].active = true;
                        ship.bullets[i].position.x = ship.position.x + (ship.radius * dx);
                        ship.bullets[i].position.y = ship.position.y + (ship.radius * dy);
                        ship.bullets[i].velocity.dx = ship.velocity.dx + (BULLET_SPEED * dx);
                        ship.bullets[i].velocity.dy = ship.velocity.dy + (BULLET_SPEED * dy);
                        ship.cooldown = BULLET_COOLDOWN;
                        break;
                    }
                }
            }
        }

        if (thrust != 0) {
            float dx = thrust * Mathf.Cos(degtorad(heading));
            float dy = thrust * Mathf.Sin(degtorad(heading));

            ship.velocity.dx += dx;
            ship.velocity.dy += dy;
            float mag = Mathf.Sqrt(ship.velocity.dx * ship.velocity.dx +
                             ship.velocity.dy * ship.velocity.dy);
            if (mag > SHIP_MAX_THRUST) {
                ship.velocity.dx = (ship.velocity.dx * SHIP_MAX_THRUST) / mag;
                ship.velocity.dy = (ship.velocity.dy * SHIP_MAX_THRUST) / mag;
            }
        }
        GGPO.DllLog(ggpo, $"new ship velocity: (dx:{ship.velocity.dx} dy:{ship.velocity.dy}).\n");

        ship.position.x += ship.velocity.dx;
        ship.position.y += ship.velocity.dy;
        GGPO.DllLog(ggpo, $"new ship position: (dx:{ship.position.x} dy:{ship.position.y}).\n");

        if (ship.position.x - ship.radius < _bounds.xMin ||
            ship.position.x + ship.radius > _bounds.xMax) {
            ship.velocity.dx *= -1;
            ship.position.x += (ship.velocity.dx * 2);
        }
        if (ship.position.y - ship.radius < _bounds.yMin ||
            ship.position.y + ship.radius > _bounds.yMax) {
            ship.velocity.dy *= -1;
            ship.position.y += (ship.velocity.dy * 2);
        }
        for (int i = 0; i < MAX_BULLETS; i++) {
            Bullet bullet = ship.bullets[i];
            if (bullet.active) {
                bullet.position.x += bullet.velocity.dx;
                bullet.position.y += bullet.velocity.dy;
                if (bullet.position.x < _bounds.xMin ||
                    bullet.position.y < _bounds.yMin ||
                    bullet.position.x > _bounds.xMax ||
                    bullet.position.y > _bounds.yMax) {
                    bullet.active = false;
                }
                else {
                    for (int j = 0; j < _num_ships; j++) {
                        Ship other = _ships[j];
                        if (distance(bullet.position, other.position) < other.radius) {
                            ship.score++;
                            other.health -= BULLET_DAMAGE;
                            bullet.active = false;
                            break;
                        }
                    }
                }
            }
        }
    }

    public void Update(ulong[] inputs, int disconnect_flags) {
        _framenumber++;
        for (int i = 0; i < _num_ships; i++) {
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
