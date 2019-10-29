public class BitAccess {

    public struct Section {
        public int pos;
        public int length;
        public ulong mask;

        public Section(int pos, int length) {
            this.pos = pos;
            this.length = length;
            this.mask = GetMask(pos, length);
        }
    }

    public static Section GetSection(ulong maxValue) {
        return new Section(GetSize(maxValue), 0);
    }

    public static Section GetSection(ulong maxValue, Section prev) {
        return new Section(GetSize(maxValue), prev.pos + prev.length);
    }

    static public int GetSize(ulong value) {
        var i = 1;
        while ((1ul << i) <= value) {
            ++i;
        }
        return i;
    }

    static ulong GetMask(int start, int length) {
        return ((1ul << length) - 1) << start;
    }

    public ulong Get(int start, int length) {
        var mask = GetMask(start, length);
        var d = data & mask;
        return d >> start;
    }

    public ulong Get(Section s) {
        return Get(s.pos, s.length);
    }

    public void Set(int start, int length, int value) {
        var nmask = ~GetMask(start, length);
        data = (data & nmask) | ((ulong)value << start);
    }

    public void Set(Section s, int value) {
        Set(value, s.pos, s.length);
    }

    public ulong data;
}
