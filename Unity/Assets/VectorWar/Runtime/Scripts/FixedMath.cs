namespace VectorWar {
    // Simulation values are signed Q16.16; all products use 64-bit intermediates.
    public static class FixedMath {
        public const int One = 65536;
        public static int Mul(int a, int b) => (int)((long)a * b / One);
        public static float ToFloat(int value) => value / (float)One;
        public static long Sqrt(long value) {
            long result = 0, bit = 1L << 62;
            while (bit > value) bit >>= 2;
            while (bit != 0) {
                if (value >= result + bit) { value -= result + bit; result = (result >> 1) + bit; }
                else result >>= 1;
                bit >>= 2;
            }
            return result;
        }
    }
    public struct FixedVector {
        public int x, y;
        public FixedVector(int x, int y) { this.x = x; this.y = y; }
        public long SqrMagnitude => (long)x * x + (long)y * y;
        public static long SqrDistance(FixedVector a, FixedVector b) {
            long dx = (long)a.x - b.x, dy = (long)a.y - b.y;
            return dx * dx + dy * dy;
        }
    }
}
