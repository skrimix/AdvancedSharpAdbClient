﻿// <copyright file="FileStatistics.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace AdvancedSharpAdbClient.Models
{
    /// <summary>
    /// Contains information about a file on the remote device.
    /// </summary>
    public struct FileStatistics : IEquatable<FileStatistics>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStatistics"/> struct.
        /// </summary>
        public FileStatistics() { }

        /// <summary>
        /// Gets or sets the path of the file.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="UnixFileType"/> attributes of the file.
        /// </summary>
        public UnixFileType FileType { get; init; }

        /// <summary>
        /// Gets or sets the total file size, in bytes.
        /// </summary>
        public int Size { get; init; }

        /// <summary>
        /// Gets or sets the time of last modification.
        /// </summary>
        public DateTimeOffset Time { get; init; }

        /// <inheritdoc/>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is FileStatistics other && Equals(other);

        /// <inheritdoc/>
        public readonly bool Equals(FileStatistics other) =>
            Path == other.Path
            && FileType == other.FileType
            && Size == other.Size
            && Time == other.Time;

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(Path, FileType, Size, Time);

        /// <inheritdoc/>
        public override readonly string ToString() => StringExtensions.Join("\t", FileType, Time, FileType, Path);

        /// <summary>
        /// Tests whether two <see cref='FileStatistics'/> objects are equally.
        /// </summary>
        /// <param name="left">The <see cref='FileStatistics'/> structure that is to the left of the equality operator.</param>
        /// <param name="right">The <see cref='FileStatistics'/> structure that is to the right of the equality operator.</param>
        /// <returns>This operator returns <see langword="true"/> if the two <see cref="FileStatistics"/> structures are equally; otherwise <see langword="false"/>.</returns>
        public static bool operator ==(FileStatistics left, FileStatistics right) => left.Equals(right);

        /// <summary>
        /// Tests whether two <see cref='FileStatistics'/> objects are different.
        /// </summary>
        /// <param name="left">The <see cref='FileStatistics'/> structure that is to the left of the inequality operator.</param>
        /// <param name="right">The <see cref='FileStatistics'/> structure that is to the right of the inequality operator.</param>
        /// <returns>This operator returns <see langword="true"/> if the two <see cref="FileStatistics"/> structures are unequally; otherwise <see langword="false"/>.</returns>
        public static bool operator !=(FileStatistics left, FileStatistics right) => !left.Equals(right);
    }
}
