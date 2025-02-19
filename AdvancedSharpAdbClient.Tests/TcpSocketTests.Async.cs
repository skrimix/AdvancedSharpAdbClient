﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;

namespace AdvancedSharpAdbClient.Tests
{
    public partial class TcpSocketTests
    {
        [Fact]
        public async void LifecycleAsyncTest()
        {
            using TcpSocket socket = new();
            Assert.False(socket.Connected);

            await socket.ConnectAsync(new DnsEndPoint("www.bing.com", 80));
            Assert.True(socket.Connected);

            byte[] data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\n\n");
            await socket.SendAsync(data, data.Length, SocketFlags.None);

            byte[] responseData = new byte[128];
            await socket.ReceiveAsync(responseData, responseData.Length, SocketFlags.None);

            _ = Encoding.ASCII.GetString(responseData);
        }

        [Fact]
        public async void LifecycleAsyncMemoryTest()
        {
            using TcpSocket socket = new();
            Assert.False(socket.Connected);

            await socket.ConnectAsync(new DnsEndPoint("www.bing.com", 80));
            Assert.True(socket.Connected);

            ReadOnlyMemory<byte> data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\n\n");
            await socket.SendAsync(data, SocketFlags.None);

            byte[] responseData = new byte[128];
            await socket.ReceiveAsync(responseData.AsMemory(), SocketFlags.None);

            _ = Encoding.ASCII.GetString(responseData);
        }

        /// <summary>
        /// Tests the <see cref="TcpSocket.ReconnectAsync(CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void ReconnectAsyncTest()
        {
            using TcpSocket socket = new();
            Assert.False(socket.Connected);

            await socket.ConnectAsync(new DnsEndPoint("www.bing.com", 80));
            Assert.True(socket.Connected);

            socket.Dispose();
            Assert.False(socket.Connected);

            await socket.ReconnectAsync();
            Assert.True(socket.Connected);
        }

        /// <summary>
        /// Tests the <see cref="TcpSocket.ConnectAsync(EndPoint, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void CreateUnsupportedSocketAsyncTest()
        {
            using TcpSocket socket = new();
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => socket.ConnectAsync(new CustomEndPoint()));
        }
    }
}
