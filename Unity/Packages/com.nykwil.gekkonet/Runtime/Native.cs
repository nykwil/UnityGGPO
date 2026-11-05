using System;
using System.Runtime.InteropServices;

namespace Nykwil.GekkoNet
{
    // Layouts mirror GekkoLib/include/gekkonet.h. C bools occupy one byte.
    public static unsafe class Native
    {
        const string Library = "GekkoNet";
        public enum SessionType { Game, Stress, Spectate, Replay }
        public enum PlayerType { Local, Remote, Spectator }
        public enum GameEventType { Empty = -1, Advance, Save, Load }
        public enum SessionEventType { Empty = -1, PlayerSyncing, PlayerConnected, PlayerDisconnected, Started, SpectatorPaused, SpectatorUnpaused, DesyncDetected, ReplayFinished, ReplayDesync }
        [StructLayout(LayoutKind.Sequential)]
        public struct Config
        {
            public byte Players, Spectators, PredictionWindow;
            public uint SpectatorDelay, InputSize, StateSize;
            public byte LimitedSaving, DesyncDetection;
            public uint CheckDistance;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Address { public void* Data; public uint Size; }
        [StructLayout(LayoutKind.Sequential)]
        public struct NetworkResult { public Address Address; public uint Length; public void* Data; }
        [StructLayout(LayoutKind.Sequential)]
        public struct NetworkAdapter { public IntPtr Send, Receive, Free; }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SendData(Address* address, byte* data, int length);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NetworkResult** ReceiveData(int* count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeData(void* data);
        [StructLayout(LayoutKind.Sequential)]
        public struct Advance { public int Frame; public uint InputLength; public byte* Inputs; public byte RollingBack, RunningAhead; }
        [StructLayout(LayoutKind.Sequential)]
        public struct Save { public int Frame; public byte Portable; public uint* Checksum; public uint* Length; public byte* State; }
        [StructLayout(LayoutKind.Sequential)]
        public struct Load { public int Frame; public uint Length; public byte* State; }
        [StructLayout(LayoutKind.Explicit)]
        public struct GameData
        {
            [FieldOffset(0)] public Advance Advance;
            [FieldOffset(0)] public Save Save;
            [FieldOffset(0)] public Load Load;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct GameEvent { public int Type; public GameData Data; }
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct SessionData
        {
            [FieldOffset(0)] public int Handle;
            [FieldOffset(4)] public byte Current;
            [FieldOffset(5)] public byte Maximum;
            [FieldOffset(4)] public uint LocalChecksum;
            [FieldOffset(8)] public uint RemoteChecksum;
            [FieldOffset(12)] public int RemoteHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SessionEvent { public int Type; public SessionData Data; }
        [StructLayout(LayoutKind.Sequential)]
        public struct NetworkStats { public float KbSent, KbReceived; public ushort LastPing; public float AveragePing, Jitter; }
        [StructLayout(LayoutKind.Sequential)]
        public struct RollbackStats { public uint TotalCount, LastFrames; public float AverageFrames; public uint MaxFrames, TotalStalls; }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool gekko_create(ref IntPtr session, SessionType type);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool gekko_destroy(ref IntPtr session);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_start(IntPtr session, ref Config config);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr gekko_default_adapter(ushort port);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool gekko_default_adapter_destroy();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool gekko_disconnect_actor(IntPtr session, int actor);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_set_disconnect_timeout(IntPtr session, uint timeout);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_set_input_prediction_window(IntPtr session, byte window);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern int gekko_spectator_buffered_frames(IntPtr session);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_net_adapter_set(IntPtr session, IntPtr adapter);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern int gekko_add_actor(IntPtr session, PlayerType type, Address* address);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_add_local_input(IntPtr session, int player, void* input);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_set_local_delay(IntPtr session, int player, byte delay);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern GameEvent** gekko_update_session(IntPtr session, out int count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern SessionEvent** gekko_session_events(IntPtr session, out int count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_network_poll(IntPtr session);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern float gekko_frames_ahead(IntPtr session);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_rollback_stats(IntPtr session, out RollbackStats stats);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] public static extern void gekko_network_stats(IntPtr session, int player, out NetworkStats stats);
    }
}
