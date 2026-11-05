using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AOT;
using Nykwil.GekkoNet;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Nykwil.GekkoNet.Session;

namespace SharedGame.Samples.UnityTransport {
    /// <summary>Unreliable UTP packets carrying GekkoNet's own protocol, without an NGO layer.</summary>
    public sealed unsafe class UnityTransportAdapter : IGekkoNetworkTransport {
        // Received packets are packed back to back; one fragmented datagram may use up to MaxPacketBytes.
        const int MaxPackets = 128, MaxPacketBytes = 65536, ArenaBytes = 256 * 1024;
        // The pinned C adapter API has no user-data argument. Static reverse-P/Invoke
        // callbacks therefore support one instance of this sample adapter per process.
        static UnityTransportAdapter active;
        static readonly Native.SendData send = Send;
        static readonly Native.ReceiveData receive = Receive;
        static readonly Native.FreeData free = Free;
        struct Peer {
            public NetworkEndpoint Endpoint;
            public NetworkConnection Connection;
            public byte[] Address;
            public IntPtr AddressPointer;
        }
        readonly int ownerThread = Environment.CurrentManagedThreadId;
        NetworkDriver driver;
        NetworkPipeline pipeline;
        Peer[] peers = Array.Empty<Peer>();
        IntPtr adapter, results, pointers, packets;
        int received, arenaUsed;
        bool disposed;
        Exception callbackError;
        public IntPtr NativeAdapter => adapter;
        public int DroppedPackets { get; private set; }
        public static IGekkoNetworkTransport Create(SessionOptions options) => new UnityTransportAdapter(options);

        public UnityTransportAdapter(SessionOptions options) {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate(1);
            if (options.Mode != SessionMode.Peer && options.Mode != SessionMode.Spectator)
                throw new ArgumentException("Unity Transport is used for peer and spectator sessions.");
            if (Interlocked.CompareExchange(ref active, this, null) != null)
                throw new InvalidOperationException("The Unity Transport sample supports one adapter per process.");
            try {
                var actors = options.RemoteActors();
                peers = new Peer[actors.Length];
                int port = options.Mode == SessionMode.Spectator ? options.SpectatorListenPort : options.Players[options.LocalPlayerIndex].Port;
                var settings = new NetworkSettings();
                try {
                    settings.WithNetworkConfigParameters(disconnectTimeoutMS: options.DisconnectTimeoutMs + 3000,
                        connectTimeoutMS: 100, maxConnectAttempts: 100, maxMessageSize: 1400);
                    settings.WithFragmentationStageParameters(payloadCapacity: MaxPacketBytes);
                    driver = NetworkDriver.Create(settings);
                } finally { settings.Dispose(); }
                // GekkoNet can batch more than one MTU of unacknowledged inputs.
                // Fragmentation preserves that datagram without adding ordered reliability.
                pipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage));
                if (driver.Bind(NetworkEndpoint.AnyIpv4.WithPort((ushort)port)) != 0)
                    throw new InvalidOperationException("Unity Transport cannot bind UDP port " + port + ".");
                if (driver.Listen() != 0) throw new InvalidOperationException("Unity Transport cannot listen.");
                // Spectators and lower-indexed peers dial; hosts and higher-indexed peers accept.
                for (int i = 0; i < actors.Length; i++)
                    AddPeer(i, actors[i].Endpoint, !actors[i].IsSpectator && (options.Mode == SessionMode.Spectator || options.LocalPlayerIndex < actors[i].PlayerIndex));
                results = Marshal.AllocHGlobal(MaxPackets * sizeof(Native.NetworkResult));
                pointers = Marshal.AllocHGlobal(MaxPackets * IntPtr.Size);
                packets = Marshal.AllocHGlobal(ArenaBytes);
                adapter = Marshal.AllocHGlobal(sizeof(Native.NetworkAdapter));
                *(Native.NetworkAdapter*)adapter = new Native.NetworkAdapter {
                    Send = Marshal.GetFunctionPointerForDelegate(send), Receive = Marshal.GetFunctionPointerForDelegate(receive),
                    Free = Marshal.GetFunctionPointerForDelegate(free)
                };
                for (int i = 0; i < MaxPackets; i++) ((Native.NetworkResult**)pointers)[i] = (Native.NetworkResult*)results + i;
            } catch { Dispose(); throw; }
        }
        void AddPeer(int index, SessionEndpoint endpoint, bool connect) {
            ref var peer = ref peers[index];
            peer.Endpoint = NetworkEndpoint.Parse(endpoint.Address, (ushort)endpoint.Port);
            peer.Address = Encoding.ASCII.GetBytes(endpoint.Validate());
            peer.AddressPointer = Marshal.AllocHGlobal(peer.Address.Length);
            Marshal.Copy(peer.Address, 0, peer.AddressPointer, peer.Address.Length);
            if (connect) peer.Connection = driver.Connect(peer.Endpoint);
        }
        void CheckAlive() {
            if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Use Unity Transport on its creating thread.");
            if (disposed) throw new ObjectDisposedException(nameof(UnityTransportAdapter));
            if (callbackError != null) throw new InvalidOperationException("Unity Transport callback failed.", callbackError);
        }
        public void Poll() {
            CheckAlive();
            // Complete only this driver's scheduled work; no sleeps or connection waits.
            driver.ScheduleUpdate().Complete();
            for (int accepted = 0; accepted < 32; accepted++) {
                var connection = driver.Accept();
                if (!connection.IsCreated) break;
                int peer = Find(driver.GetRemoteEndpoint(connection));
                if (peer < 0 || peers[peer].Connection.IsCreated) driver.Disconnect(connection);
                else peers[peer].Connection = connection;
            }
            // All results must have been consumed by the preceding native Poll.
            if (received != 0) throw new InvalidOperationException("Unconsumed transport receive batch.");
            arenaUsed = 0;
            for (int events = 0; events < 1024; events++) {
                var type = driver.PopEvent(out var connection, out var reader);
                if (type == NetworkEvent.Type.Empty) break;
                if (type != NetworkEvent.Type.Data) continue;
                int peer = Find(connection);
                if (peer < 0 || reader.Length > MaxPacketBytes || received == MaxPackets || arenaUsed + reader.Length > ArenaBytes) { DroppedPackets++; continue; }
                byte* data = (byte*)packets + arenaUsed;
                arenaUsed += reader.Length;
                reader.ReadBytesUnsafe(data, reader.Length);
                ((Native.NetworkResult*)results)[received++] = new Native.NetworkResult {
                    Address = new Native.Address { Data = (void*)peers[peer].AddressPointer, Size = (uint)peers[peer].Address.Length },
                    Length = (uint)reader.Length, Data = data
                };
            }
        }
        int Find(NetworkEndpoint endpoint) {
            for (int i = 0; i < peers.Length; i++) if (peers[i].Endpoint == endpoint) return i;
            return -1;
        }
        int Find(NetworkConnection connection) {
            for (int i = 0; i < peers.Length; i++) if (peers[i].Connection == connection) return i;
            return -1;
        }
        public void Flush() { CheckAlive(); driver.ScheduleFlushSend().Complete(); }
        void SendPacket(Native.Address* address, byte* data, int length) {
            if (address == null || address->Data == null || address->Size > 64 || length < 0 || length > MaxPacketBytes)
                throw new InvalidOperationException("Invalid GekkoNet transport packet.");
            var bytes = new ReadOnlySpan<byte>(address->Data, (int)address->Size);
            for (int i = 0; i < peers.Length; i++) {
                ref var peer = ref peers[i];
                if (!bytes.SequenceEqual(peer.Address)) continue;
                if (!peer.Connection.IsCreated || driver.GetConnectionState(peer.Connection) != NetworkConnection.State.Connected) return;
                // Unreliable fragmentation; GekkoNet already retransmits its protocol.
                if (driver.BeginSend(pipeline, peer.Connection, out var writer, length) != 0) { DroppedPackets++; return; }
                writer.WriteBytesUnsafe(data, length);
                if (driver.EndSend(writer) < 0) DroppedPackets++;
                return;
            }
            throw new InvalidOperationException("GekkoNet sent to an endpoint outside the configured roster.");
        }
        [MonoPInvokeCallback(typeof(Native.SendData))]
        static void Send(Native.Address* address, byte* data, int length) {
            var instance = active;
            try { instance?.SendPacket(address, data, length); }
            catch (Exception error) { if (instance != null) instance.callbackError = error; }
        }
        [MonoPInvokeCallback(typeof(Native.ReceiveData))]
        static Native.NetworkResult** Receive(int* count) {
            var instance = active;
            *count = instance?.received ?? 0;
            if (instance == null) return null;
            instance.received = 0;
            return (Native.NetworkResult**)instance.pointers;
        }
        [MonoPInvokeCallback(typeof(Native.FreeData))]
        static void Free(void* data) {
            // Native calls free for the address, payload and result. These are bounded
            // pooled buffers owned by this adapter, reused only after native Poll returns.
        }
        public void Dispose() {
            if (disposed) return;
            if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Dispose on the creating thread.");
            disposed = true;
            try { if (driver.IsCreated) driver.Dispose(); }
            finally {
                Marshal.FreeHGlobal(adapter); Marshal.FreeHGlobal(results);
                Marshal.FreeHGlobal(pointers); Marshal.FreeHGlobal(packets);
                foreach (var peer in peers) Marshal.FreeHGlobal(peer.AddressPointer);
                adapter = results = pointers = packets = IntPtr.Zero;
                Interlocked.CompareExchange(ref active, null, this);
            }
        }
    }
}
