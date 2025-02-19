﻿// <copyright file="ForwardData.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace AdvancedSharpAdbClient.Models
{
    /// <summary>
    /// Contains information about port forwarding configured by the Android Debug Bridge.
    /// </summary>
    public class ForwardData : IEquatable<ForwardData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardData"/> class.
        /// </summary>
        public ForwardData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardData"/> class.
        /// </summary>
        /// <param name="serialNumber">The serial number of the device for which the port forwarding is configured.</param>
        /// <param name="local">The <see cref="string"/> that represents the local (PC) endpoint.</param>
        /// <param name="remote">The <see cref="string"/> that represents the remote (device) endpoint.</param>
        public ForwardData(string serialNumber, string local, string remote)
        {
            SerialNumber = serialNumber;
            Local = local;
            Remote = remote;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardData"/> class by parsing a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> value to parse.</param>
        public ForwardData(string value)
        {
            string[] parts = value.Split(' ');
            SerialNumber = parts[0];
            Local = parts[1];
            Remote = parts[2];
        }

        /// <summary>
        /// Gets or sets the serial number of the device for which the port forwarding is configured.
        /// </summary>
        public string SerialNumber { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets a <see cref="string"/> that represents the local (PC) endpoint.
        /// </summary>
        public string Local { get; init; } = string.Empty;

        /// <summary>
        /// Gets a <see cref="ForwardSpec"/> that represents the local (PC) endpoint.
        /// </summary>
        public ForwardSpec LocalSpec => new(Local);

        /// <summary>
        /// Gets or sets a <see cref="string"/> that represents the remote (device) endpoint.
        /// </summary>
        public string Remote { get; init; } = string.Empty;

        /// <summary>
        /// Gets a <see cref="ForwardSpec"/> that represents the remote (device) endpoint.
        /// </summary>
        public ForwardSpec RemoteSpec => new(Remote);

        /// <summary>
        /// Creates a new instance of the <seealso cref="ForwardData"/> class by parsing a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> value to parse.</param>
        /// <returns>A <see cref="ForwardData"/> object that represents the port forwarding information contained in <paramref name="value"/>.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static ForwardData? FromString(string? value) => value == null ? null : new ForwardData(value);

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as ForwardData);

        /// <inheritdoc/>
        public bool Equals([NotNullWhen(true)] ForwardData? other) =>
            other is not null
                && SerialNumber == other.SerialNumber
                && Local == other.Local
                && Remote == other.Remote;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(SerialNumber, Local, Remote);

        /// <inheritdoc/>
        public override string ToString() => $"{SerialNumber} {Local} {Remote}";
    }
}
