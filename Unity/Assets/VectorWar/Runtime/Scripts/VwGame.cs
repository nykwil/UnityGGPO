using System;
using System.Buffers.Binary;
using Nykwil.GekkoNet.Session;
namespace VectorWar {
    public static class VWConstants {
        public const int MAX_BULLETS = 30;
        public const int INPUT_THRUST = 1, INPUT_BREAK = 2, INPUT_ROTATE_LEFT = 4, INPUT_ROTATE_RIGHT = 8, INPUT_FIRE = 16, INPUT_BOMB = 32;
        public const int STARTING_HEALTH = 100, ROTATE_INCREMENT = 3, SHIP_RADIUS = 15 * FixedMath.One;
        public const int SHIP_THRUST = 3932, SHIP_MAX_THRUST = 4 * FixedMath.One, BULLET_SPEED = 5 * FixedMath.One;
        public const int BULLET_COOLDOWN = 8, BULLET_DAMAGE = 10;
    }
    public struct Bullet {
        public bool active;
        public FixedVector position, velocity;
    }
    public sealed class Ship {
        public FixedVector position, velocity;
        public int heading, health, cooldown, score;
        public readonly Bullet[] bullets = new Bullet[VWConstants.MAX_BULLETS];
    }
    public sealed class VwGame : IGame {
        public const int SnapshotVersion = 1;
        const int Magic = 0x31575756, HeaderBytes = 16, ShipFieldBytes = 32, BulletBytes = 17;
        const int ShipBytes = ShipFieldBytes + VWConstants.MAX_BULLETS * BulletBytes;
        const int Width = 640 * FixedMath.One, Height = 480 * FixedMath.One;
        public readonly Ship[] Ships;
        public int PlayerCount => Ships.Length;
        public int FrameNumber { get; private set; }
        public int SnapshotSize => HeaderBytes + PlayerCount * ShipBytes;
        public VwGame(int playerCount) {
            if (playerCount < 2 || playerCount > 4) throw new ArgumentOutOfRangeException(nameof(playerCount));
            Ships = new Ship[playerCount];
            for (int i = 0; i < playerCount; i++) {
                int angle = i * 360 / playerCount;
                Ships[i] = new Ship {
                    position = new FixedVector(Width / 2 + 120 * TrigTable.Cos(angle), Height / 2 + 120 * TrigTable.Sin(angle)),
                    heading = (angle + 180) % 360, health = VWConstants.STARTING_HEALTH
                };
            }
        }
        public void AdvanceFrame(ReadOnlySpan<long> inputs) {
            if (inputs.Length != PlayerCount) throw new ArgumentException("Input count must match players.");
            FrameNumber++;
            for (int i = 0; i < Ships.Length; i++) {
                var ship = Ships[i];
                long input = inputs[i];
                if ((input & VWConstants.INPUT_ROTATE_RIGHT) != 0) ship.heading = (ship.heading + 360 - VWConstants.ROTATE_INCREMENT) % 360;
                else if ((input & VWConstants.INPUT_ROTATE_LEFT) != 0) ship.heading = (ship.heading + VWConstants.ROTATE_INCREMENT) % 360;
                int dx = TrigTable.Cos(ship.heading), dy = TrigTable.Sin(ship.heading);
                if (ship.cooldown == 0 && (input & VWConstants.INPUT_FIRE) != 0) {
                    for (int b = 0; b < ship.bullets.Length; b++) {
                        if (ship.bullets[b].active) continue;
                        ship.bullets[b] = new Bullet { active = true,
                            position = new FixedVector(ship.position.x + FixedMath.Mul(VWConstants.SHIP_RADIUS, dx), ship.position.y + FixedMath.Mul(VWConstants.SHIP_RADIUS, dy)),
                            velocity = new FixedVector(ship.velocity.x + FixedMath.Mul(VWConstants.BULLET_SPEED, dx), ship.velocity.y + FixedMath.Mul(VWConstants.BULLET_SPEED, dy)) };
                        ship.cooldown = VWConstants.BULLET_COOLDOWN; break;
                    }
                }
                int thrust = (input & VWConstants.INPUT_THRUST) != 0 ? VWConstants.SHIP_THRUST :
                    (input & VWConstants.INPUT_BREAK) != 0 ? -VWConstants.SHIP_THRUST : 0;
                if (thrust != 0) {
                    ship.velocity.x += FixedMath.Mul(thrust, dx); ship.velocity.y += FixedMath.Mul(thrust, dy);
                    long magnitude = FixedMath.Sqrt(ship.velocity.SqrMagnitude);
                    if (magnitude > VWConstants.SHIP_MAX_THRUST) {
                        ship.velocity.x = (int)((long)ship.velocity.x * VWConstants.SHIP_MAX_THRUST / magnitude);
                        ship.velocity.y = (int)((long)ship.velocity.y * VWConstants.SHIP_MAX_THRUST / magnitude);
                    }
                }
                ship.position.x += ship.velocity.x; ship.position.y += ship.velocity.y;
                if (ship.position.x < VWConstants.SHIP_RADIUS || ship.position.x > Width - VWConstants.SHIP_RADIUS) {
                    ship.velocity.x = -ship.velocity.x; ship.position.x += ship.velocity.x * 2;
                }
                if (ship.position.y < VWConstants.SHIP_RADIUS || ship.position.y > Height - VWConstants.SHIP_RADIUS) {
                    ship.velocity.y = -ship.velocity.y; ship.position.y += ship.velocity.y * 2;
                }
                for (int b = 0; b < ship.bullets.Length; b++) {
                    ref var bullet = ref ship.bullets[b];
                    if (!bullet.active) continue;
                    bullet.position.x += bullet.velocity.x; bullet.position.y += bullet.velocity.y;
                    if (bullet.position.x < 0 || bullet.position.x > Width || bullet.position.y < 0 || bullet.position.y > Height) bullet.active = false;
                    else for (int j = 0; j < Ships.Length; j++) {
                        if (FixedVector.SqrDistance(bullet.position, Ships[j].position) < (long)VWConstants.SHIP_RADIUS * VWConstants.SHIP_RADIUS) {
                            ship.score++; Ships[j].health -= VWConstants.BULLET_DAMAGE; bullet.active = false; break;
                        }
                    }
                }
                if (ship.cooldown > 0) ship.cooldown--;
            }
        }
        static void Put(Span<byte> bytes, ref int offset, int value) {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(offset, 4), value); offset += 4;
        }
        static int Get(ReadOnlySpan<byte> bytes, ref int offset) {
            int value = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, 4)); offset += 4; return value;
        }
        public void SaveState(Span<byte> destination) {
            if (destination.Length != SnapshotSize) throw new ArgumentException("Snapshot length mismatch.");
            int offset = 0;
            Put(destination, ref offset, Magic); Put(destination, ref offset, SnapshotVersion);
            Put(destination, ref offset, PlayerCount); Put(destination, ref offset, FrameNumber);
            foreach (var ship in Ships) {
                Put(destination, ref offset, ship.position.x); Put(destination, ref offset, ship.position.y);
                Put(destination, ref offset, ship.velocity.x); Put(destination, ref offset, ship.velocity.y);
                Put(destination, ref offset, ship.heading); Put(destination, ref offset, ship.health);
                Put(destination, ref offset, ship.cooldown); Put(destination, ref offset, ship.score);
                foreach (var bullet in ship.bullets) {
                    destination[offset++] = bullet.active ? (byte)1 : (byte)0;
                    Put(destination, ref offset, bullet.position.x); Put(destination, ref offset, bullet.position.y);
                    Put(destination, ref offset, bullet.velocity.x); Put(destination, ref offset, bullet.velocity.y);
                }
            }
        }
        public void LoadState(ReadOnlySpan<byte> source) {
            if (source.Length != SnapshotSize) throw new ArgumentException("Snapshot length mismatch.");
            int offset = 0;
            if (Get(source, ref offset) != Magic || Get(source, ref offset) != SnapshotVersion || Get(source, ref offset) != PlayerCount)
                throw new ArgumentException("Incompatible VectorWar snapshot.");
            int frame = Get(source, ref offset);
            // Walk the same field order as SaveState, checking headings, cooldowns and
            // canonical booleans before changing any state.
            for (int i = 0, check = offset; i < PlayerCount; i++) {
                Get(source, ref check); Get(source, ref check); Get(source, ref check); Get(source, ref check); // position, velocity
                int heading = Get(source, ref check); Get(source, ref check); // health
                int cooldown = Get(source, ref check); Get(source, ref check); // score
                if (heading < 0 || heading >= 360 || cooldown < 0 || cooldown >= VWConstants.BULLET_COOLDOWN)
                    throw new ArgumentException("Malformed ship state.");
                for (int b = 0; b < VWConstants.MAX_BULLETS; b++, check += BulletBytes)
                    if (source[check] > 1) throw new ArgumentException("Malformed bullet state.");
            }
            FrameNumber = frame;
            foreach (var ship in Ships) {
                ship.position = new FixedVector(Get(source, ref offset), Get(source, ref offset));
                ship.velocity = new FixedVector(Get(source, ref offset), Get(source, ref offset));
                ship.heading = Get(source, ref offset); ship.health = Get(source, ref offset);
                ship.cooldown = Get(source, ref offset); ship.score = Get(source, ref offset);
                for (int b = 0; b < ship.bullets.Length; b++) {
                    ship.bullets[b].active = source[offset++] != 0;
                    ship.bullets[b].position = new FixedVector(Get(source, ref offset), Get(source, ref offset));
                    ship.bullets[b].velocity = new FixedVector(Get(source, ref offset), Get(source, ref offset));
                }
            }
        }
    }
}
