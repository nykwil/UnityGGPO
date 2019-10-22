using System;
using UnityEngine;

public struct InputInfo {
}

public struct BitAccess {

    public struct Section {
        public int pos;
        public int length;
        public ulong mask;

        public Section(int pos, int length) {
            this.pos = pos;
            this.length = length;
            this.mask = BitAccess.GetMask(pos, length);
        }
    }

    public static Section GetSection(ulong maxValue, Section prev) {
        return new Section(GetSize(maxValue), prev.pos + prev.length);
    }

    public static Section GetSection(ulong maxValue) {
        return new Section(GetSize(maxValue), 0);
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

public class Test : MonoBehaviour {

    public static string ToBitsString(byte value) {
        return Convert.ToString(value, 2).PadLeft(8, '0');
    }

    public static void Wr(string s, ulong bits) {
        Console.WriteLine(s + "=" + Convert.ToString((long)bits, toBase: 2).PadLeft(64, '0'));
    }

    public void Start() {
        // Wr("1", 0b1 << 1 - 1); long a = 0b_0000_1100; Wr("s", a);
        var b = new BitAccess();
        // Console.WriteLine("4:" + b.GetSize(4));
        var b1 = BitAccess.GetSection(1);
        var b2 = BitAccess.GetSection(1, b1);
        var b3 = BitAccess.GetSection(1, b2);
        var b4 = BitAccess.GetSection(1, b3);
        var a5 = BitAccess.GetSection(360, b4);
        var a6 = BitAccess.GetSection(360, a5);
        b.data = 0b_1111_1101;
        b.Set(b1, 1);
        b.Set(b2, 0);
        b.Set(b3, 1);
        b.Set(b4, 1);
        b.Set(a5, 320);
        b.Set(a6, 14);
        Wr("a", b.data);
        Console.WriteLine(b.Get(b1));
        Console.WriteLine(b.Get(b2));
        Console.WriteLine(b.Get(b3));
        Console.WriteLine(b.Get(b4));
        Console.WriteLine(b.Get(a5));
        Console.WriteLine(b.Get(a6));
    }
}
