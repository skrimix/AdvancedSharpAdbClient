﻿// <copyright file="TcpSocket.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace AdvancedSharpAdbClient
{
    /// <summary>
    /// Implements the <see cref="ITcpSocket"/> interface using the standard <see cref="System.Net.Sockets.Socket"/> class.
    /// </summary>
    public partial class TcpSocket : ITcpSocket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TcpSocket"/> class.
        /// </summary>
        public TcpSocket() => Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            SendTimeout = 10000,
            ReceiveTimeout = 10000
        };

        /// <summary>
        /// The underlying socket that manages the connection.
        /// </summary>
        public Socket Socket { get; protected set; }

        /// <summary>
        /// The <see cref="System.Net.EndPoint"/> at which the socket is listening.
        /// </summary>
        public EndPoint? EndPoint { get; protected set; }

        /// <inheritdoc/>
        public bool Connected => Socket.Connected;

        /// <inheritdoc/>
        public int ReceiveBufferSize
        {
            get => Socket.ReceiveBufferSize;
            set => Socket.ReceiveBufferSize = value;
        }

        /// <inheritdoc/>
        [MemberNotNull(nameof(EndPoint))]
        public virtual void Connect(EndPoint endPoint)
        {
            if (endPoint is not (IPEndPoint or DnsEndPoint))
            {
                throw new NotSupportedException("Only TCP endpoints are supported");
            }

            EndPoint = endPoint;
            Socket.Connect(endPoint);
            Socket.Blocking = true;
        }

        /// <inheritdoc/>
        public virtual void Reconnect(bool isForce = false)
        {
            if (isForce || !Socket.Connected)
            {
                Socket.Dispose();
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Connect(EndPoint!);
            }
            else
            {
                // Already connected - nothing to do.
                return;
            }
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Socket.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public virtual void Close() => Socket.Close();

        /// <inheritdoc/>
        public virtual int Send(byte[] buffer, SocketFlags socketFlags) =>
            Socket.Send(buffer, socketFlags);

        /// <inheritdoc/>
        public virtual int Send(byte[] buffer, int size, SocketFlags socketFlags) =>
            Socket.Send(buffer, size, socketFlags);

        /// <inheritdoc/>
        public virtual int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags) =>
            Socket.Send(buffer, offset, size, socketFlags);

        /// <inheritdoc/>
        public virtual int Receive(byte[] buffer, SocketFlags socketFlags) =>
            Socket.Receive(buffer, socketFlags);

        /// <inheritdoc/>
        public virtual int Receive(byte[] buffer, int size, SocketFlags socketFlags) =>
            Socket.Receive(buffer, size, socketFlags);

        /// <inheritdoc/>
        public virtual int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags) =>
            Socket.Receive(buffer, offset, size, socketFlags);

#if HAS_BUFFERS
        /// <inheritdoc/>
        public virtual int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags) =>
            Socket.Send(buffer, socketFlags);

        /// <inheritdoc/>
        public virtual int Receive(Span<byte> buffer, SocketFlags socketFlags) =>
            Socket.Receive(buffer, socketFlags);
#endif

        /// <inheritdoc/>
        public virtual Stream GetStream() => new NetworkStream(Socket);
    }
}
