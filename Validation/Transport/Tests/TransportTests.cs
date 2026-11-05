using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Nykwil.GekkoNet;
using SharedGame.Samples.UnityTransport;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using Nykwil.GekkoNet.Session;

public sealed unsafe class TransportTests {
    static SessionOptions Options(int port) => new SessionOptions {
        Mode = SessionMode.Peer,
        Players = new[] { new SessionEndpoint { Port = port }, new SessionEndpoint { Port = port + 1 } }
    };
    [Test] public void StartupFailureAndRepeatedDisposeReleaseAdapterOwnership() {
        for(int cycle = 0; cycle < 3; cycle++) {
            using(var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
                socket.ExclusiveAddressUse = true; socket.Bind(new IPEndPoint(IPAddress.Any, 35200));
                LogAssert.Expect(LogType.Error, new Regex("Failed to bind UDP socket because the address is already in use.*"));
                Assert.Throws<InvalidOperationException>(() => new UnityTransportAdapter(Options(35200)));
            }
            var adapter = new UnityTransportAdapter(Options(35200));
            Assert.Throws<InvalidOperationException>(() => new UnityTransportAdapter(Options(35210)));
            adapter.Dispose(); adapter.Dispose();
            using(var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                socket.Bind(new IPEndPoint(IPAddress.Any, 35200));
        }
    }
    [TestCase(4), TestCase(12000)] public void NativeCallbacksExchangeDatagramsAndPreserveSenderAddress(int payloadLength) {
        var settings = new NetworkSettings();
        settings.WithFragmentationStageParameters(payloadCapacity: 65536);
        var raw = NetworkDriver.Create(settings); settings.Dispose();
        var pipeline = raw.CreatePipeline(typeof(FragmentationPipelineStage));
        try {
            Assert.That(raw.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(35301)), Is.Zero);
            Assert.That(raw.Listen(), Is.Zero);
            using(var adapter = new UnityTransportAdapter(Options(35300))) {
                var api = *(Native.NetworkAdapter*)adapter.NativeAdapter;
                var receive = Marshal.GetDelegateForFunctionPointer<Native.ReceiveData>(api.Receive);
                var send = Marshal.GetDelegateForFunctionPointer<Native.SendData>(api.Send);
                var free = Marshal.GetDelegateForFunctionPointer<Native.FreeData>(api.Free);
                bool sent = false, replied = false, complete = false;
                var timer = System.Diagnostics.Stopwatch.StartNew();
                NetworkConnection connection = default;
                while(!complete && timer.Elapsed.TotalSeconds < 10) {
                    adapter.Poll();
                    int count; var packets = receive(&count);
                    if(count > 0) {
                        Assert.That(count, Is.EqualTo(1));
                        Assert.That(new ReadOnlySpan<byte>(packets[0]->Address.Data,(int)packets[0]->Address.Size).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("127.0.0.1:35301")), Is.True);
                        Assert.That(packets[0]->Length, Is.EqualTo(payloadLength));
                        for(int b=0;b<payloadLength;b++) Assert.That(((byte*)packets[0]->Data)[b],Is.EqualTo(unchecked((byte)(b*17))));
                        send(&packets[0]->Address,(byte*)packets[0]->Data,payloadLength); replied = true;
                        free(packets[0]->Address.Data);free(packets[0]->Data);free(packets[0]);
                    }
                    adapter.Flush();
                    raw.ScheduleUpdate().Complete();
                    if(!connection.IsCreated) connection = raw.Accept();
                    if(connection.IsCreated && !sent) {
                        if(raw.BeginSend(pipeline,connection,out var writer,payloadLength) == 0) {
                            for(int b=0;b<payloadLength;b++)writer.WriteByte(unchecked((byte)(b*17)));
                            raw.EndSend(writer);sent=true;
                        }
                    }
                    NetworkEvent.Type type;
                    while((type=raw.PopEvent(out _,out var reader))!=NetworkEvent.Type.Empty)
                        if(type == NetworkEvent.Type.Data){
                            Assert.That(reader.Length,Is.EqualTo(payloadLength));
                            for(int b=0;b<payloadLength;b++)Assert.That(reader.ReadByte(),Is.EqualTo(unchecked((byte)(b*17))));
                            complete=true;
                        }
                    raw.ScheduleFlushSend().Complete();
                }
                Assert.That(sent && replied && complete, Is.True, "UTP handshake/datagram exchange did not complete.");
                Assert.That(adapter.DroppedPackets, Is.Zero);
            }
        } finally { raw.Dispose(); }
    }
    [Test] public void CallbackErrorsReturnToManagedOwnerInsteadOfCrossingNativeBoundary() {
        using(var adapter = new UnityTransportAdapter(Options(35400))) {
            var api = *(Native.NetworkAdapter*)adapter.NativeAdapter;
            var send = Marshal.GetDelegateForFunctionPointer<Native.SendData>(api.Send);
            send(null,null,1500);
            Assert.Throws<InvalidOperationException>(()=>adapter.Flush());
        }
        using(var next = new UnityTransportAdapter(Options(35400))) Assert.That(next.NativeAdapter,Is.Not.EqualTo(IntPtr.Zero));
    }
}
