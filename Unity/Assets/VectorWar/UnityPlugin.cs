#define CS
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class UnityPlugin : MonoBehaviour {
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool BeginGameDelegate(string name);
#else
    typedef bool (* BeginGameDelegate) (const char* name);
#endif

#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool AdvanceFrameDelegate(int flags);
#else
    typedef bool (* AdvanceFrameDelegate) (int flags);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool LoadGameStateDelegate(byte[] buffer, int length);
#else
	typedef bool (* LoadGameStateDelegate) (unsigned char* buffer, int length);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FreeBufferDelegate(byte[] buffer, int length);
#else
    typedef void (* FreeBufferDelegate) (unsigned char* buffer, int length);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool LogGameStateDelegate(string filename, byte[] buffer, int length);
#else
    typedef bool (* LogGameStateDelegate) (const char* filename, unsigned char* buffer, int length);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool SaveGameStateDelegate(ref byte[] buffer, ref int length, ref int checksum, int frame);
#else
    typedef bool (* SaveGameStateDelegate) (unsigned char*& buffer, int& length, int& checksum, int frame);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventConnectedToPeerDelegate(int connected_player);
#else
    typedef bool (* OnEventConnectedToPeerDelegate) (int connected_player);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total);
#else
	typedef bool (* OnEventSynchronizingWithPeerDelegate) (int synchronizing_player, int synchronizing_count, int synchronizing_total);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventSynchronizedWithPeerDelegate(int synchronizing_player);
#else
	typedef bool (* OnEventSynchronizedWithPeerDelegate) (int synchronizing_player);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventRunningDelegate();
#else
	typedef bool (* OnEventRunningDelegate) ();
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);
#else
	typedef bool (* OnEventConnectionInterruptedDelegate) (int connection_interrupted_player, int connection_interrupted_disconnect_timeout);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventConnectionResumedDelegate(int connection_resumed_player);
#else
	typedef bool (* OnEventConnectionResumedDelegate) (int connection_resumed_player);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventDisconnectedFromPeerDelegate(int disconnected_player);
#else
	typedef bool (* OnEventDisconnectedFromPeerDelegate) (int disconnected_player);
#endif
#if CS

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead);
#else
	typedef bool (* OnEventEventcodeTimesyncDelegate) (int timesync_frames_ahead);
#endif

    private delegate void SimpleCallback();
    private SimpleCallback _recurring_callback_holder;

    private delegate void DataCallback(IntPtr buffer, IntPtr length);
    private DataCallback _recurring_data_push_holder;

    [DllImport("UnityPlugin")]
    private static extern void test_begin_game(BeginGameDelegate begin_game);

    [DllImport("UnityPlugin")]
    private static extern void reply_during_call(SimpleCallback callback);

    [DllImport("UnityPlugin")]
    private static extern void set_recurring_reply(int objectHash, SimpleCallback callback);

    [DllImport("UnityPlugin")]
    private static extern void poll_data(int objectHash, out IntPtr buffer, out IntPtr length);

    [DllImport("UnityPlugin")]
    private static extern void set_recurring_data_push(int objectHash, DataCallback callback);

    void Awake() {
        reply_during_call(_onetime_callback);
        test_begin_game(Callback);
    }

    private bool Callback(string name) {
        Debug.Log(name);
        return true;
    }

    void _onetime_callback() {
        Debug.Log("one-time callback on " + gameObject);
    }

    void _recurring_callback() {
        Debug.Log("recurring on " + gameObject + " called at " + Time.time);
    }

    void _recurring_data_push(IntPtr in_buffer, IntPtr in_length) {
        Debug.Log("recurring data push on " + gameObject + " called at " + Time.time);
        int length = in_length.ToInt32();
        byte[] buffer = new byte[length];
        Marshal.Copy(in_buffer, buffer, 0, length);
        Debug.Log("transferred " + length + " bytes into C# as " + buffer);
    }

    /*
        void Awake() {
            Debug.Log("testing callbacks on object:" + gameObject.GetHashCode());

            // during the course of this call into the plugin, it will call back the
            // _onetime_callback() function. because the plugin call/marshaling layer retains a
            // reference to the SimpleCallback delegate, it can not be freed or GC'd during the call
            // all of the following syntaxes work:
            // 1. reply_during_call(new SimpleCallback(this._onetime_callback));
            // 2. reply_during_call(this._onetime_callback);
            // 3. below, the most clear form
            reply_during_call(_onetime_callback);

            // to pass a delegate that is called after the call into the plugin completes, either
            // later or repeatedly later, you have to retain the delegate, which you can only do by
            // holding it in a wider-scope, in this case in a private member/ivar. the marshaling
            // layer will add a hold/addref/retain to the delegate during the call and release it on
            // return, so unless you are holding on to it the plugin would be left with an invalid
            // pointer as soon as GC runs. it's worth understanding that the delegate is effectively
            // two "objects": a managed object in C# which may move due to GC which is holding onto a
            // fixed (possibly unmanaged - that's runtime-dependent) function-pointer which is what
            // the plugin can refer to. GC may move the managed C# object around, but the function
            // pointer in the native-code plugin remains immobile.
            _recurring_callback_holder = new SimpleCallback(_recurring_callback);
            set_recurring_reply(gameObject.GetHashCode(), _recurring_callback_holder);

            // this is a one-time data-polling call to pull bytes from native/unmanaged code. the
            // difficulty with a poll API is that the plugin cannot allocate memory for the caller
            // easily unless both sides agree how it is going to be released. a polling API can be
            // useful if you have a native data structure or very long-lived buffer associated with
            // the specific object instance that Unity can depend on not moving or disappearing.
            // alternately you can force the caller to deallocate the memory returned from the poll.
            // as with any real-time system, though, you want to make sure you are minimizing data
            // copies (and conversions), so think through the memory lifetimes carefully.
            IntPtr buffer = IntPtr.Zero, length = IntPtr.Zero;
            poll_data(gameObject.GetHashCode(), out buffer, out length);
            if (buffer != IntPtr.Zero && length != IntPtr.Zero) {
                int len = length.ToInt32();
                byte[] data = new byte[len];
                Marshal.Copy(buffer, data, 0, len);
                Debug.Log("polled " + len + " bytesinto C# as " + data);
            }

            // this recurring callback, like the SimpleCallback, must be held longer than the
            // duration of the call. this example is to demonstrate a common requirement: passing
            // data between a native/unmanaged plugin C#. in this case the native plugin pushes raw
            // bytes of data into a C# byte[], which is a nice way to control the lifetime of the
            // data on the plugin's side - by the time the callback returns the plugin knows Unity/C#
            // has no further access, they should have copied what they needed. this form of "push"
            // API's where the native side buffers data and pushes it to Unity/C# and the C# side
            // also buffers and uses and frees it on its own schedule is a pretty safe design which
            // can minimize the total number of data copies and problems with memory leaks.
            _recurring_data_push_holder = new DataCallback(_recurring_data_push);
            set_recurring_data_push(gameObject.GetHashCode(), _recurring_data_push_holder);
        }

        void OnDestroy() {
            set_recurring_reply(gameObject.GetHashCode(), null);
            _recurring_callback_holder = null;
            set_recurring_data_push(gameObject.GetHashCode(), null);
            _recurring_data_push_holder = null;
        }

        void OnApplicationQuit() {
            set_recurring_reply(gameObject.GetHashCode(), null);
            _recurring_callback_holder = null;
            set_recurring_data_push(gameObject.GetHashCode(), null);
            _recurring_data_push_holder = null;
        }
        */
}
