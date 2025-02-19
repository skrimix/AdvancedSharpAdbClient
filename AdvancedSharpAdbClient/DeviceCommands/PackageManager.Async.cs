﻿#if HAS_TASK
// <copyright file="PackageManager.Async.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AdvancedSharpAdbClient.DeviceCommands
{
    public partial class PackageManager
    {
        /// <summary>
        /// Refreshes the packages.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual Task RefreshPackagesAsync(CancellationToken cancellationToken = default)
        {
            ValidateDevice();

            StringBuilder requestBuilder = new StringBuilder().Append(ListFull);

            if (Arguments != null)
            {
                foreach (string argument in Arguments)
                {
                    _ = requestBuilder.AppendFormat(" {0}", argument);
                }
            }

            string cmd = requestBuilder.ToString();
            PackageManagerReceiver pmr = new(this);
            return AdbClient.ExecuteShellCommandAsync(Device, cmd, pmr, cancellationToken);
        }

        /// <summary>
        /// Installs an Android application on device.
        /// </summary>
        /// <param name="packageFilePath">The absolute file system path to file on local host to install.</param>
        /// <param name="arguments">The arguments to pass to <c>adb install</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallPackageAsync(string packageFilePath, params string[] arguments) =>
            InstallPackageAsync(packageFilePath, default, arguments);

        /// <summary>
        /// Installs an Android application on device.
        /// </summary>
        /// <param name="packageFilePath">The absolute file system path to file on local host to install.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>adb install</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallPackageAsync(string packageFilePath, CancellationToken cancellationToken, params string[] arguments)
        {
            ValidateDevice();

            void OnSyncProgressChanged(string? sender, SyncProgressChangedEventArgs args) =>
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(sender is null ? 1 : 0, 1, args.ProgressPercentage));

            string remoteFilePath = await SyncPackageToDeviceAsync(packageFilePath, OnSyncProgressChanged, cancellationToken).ConfigureAwait(false);

            await InstallRemotePackageAsync(remoteFilePath, cancellationToken, arguments).ConfigureAwait(false);

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(0, 1, PackageInstallProgressState.PostInstall));
            await RemoveRemotePackageAsync(remoteFilePath, cancellationToken).ConfigureAwait(false);
            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(1, 1, PackageInstallProgressState.PostInstall));

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Finished));
        }

        /// <summary>
        /// Installs the application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="remoteFilePath">absolute file path to package file on device.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallRemotePackageAsync(string remoteFilePath, params string[] arguments) =>
            InstallRemotePackageAsync(remoteFilePath, default, arguments);

        /// <summary>
        /// Installs the application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="remoteFilePath">absolute file path to package file on device.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallRemotePackageAsync(string remoteFilePath, CancellationToken cancellationToken, params string[] arguments)
        {
            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Installing));

            ValidateDevice();

            StringBuilder requestBuilder = new StringBuilder().Append("pm install");

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    _ = requestBuilder.AppendFormat(" {0}", argument);
                }
            }

            _ = requestBuilder.AppendFormat(" \"{0}\"", remoteFilePath);

            string cmd = requestBuilder.ToString();
            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, cmd, receiver, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }
        }

        /// <summary>
        /// Installs Android multiple application on device.
        /// </summary>
        /// <param name="basePackageFilePath">The absolute base app file system path to file on local host to install.</param>
        /// <param name="splitPackageFilePaths">The absolute split app file system paths to file on local host to install.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallMultiplePackageAsync(string basePackageFilePath, IEnumerable<string> splitPackageFilePaths, params string[] arguments) =>
            InstallMultiplePackageAsync(basePackageFilePath, splitPackageFilePaths, default, arguments);

        /// <summary>
        /// Installs Android multiple application on device.
        /// </summary>
        /// <param name="basePackageFilePath">The absolute base app file system path to file on local host to install.</param>
        /// <param name="splitPackageFilePaths">The absolute split app file system paths to file on local host to install.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallMultiplePackageAsync(string basePackageFilePath, IEnumerable<string> splitPackageFilePaths, CancellationToken cancellationToken, params string[] arguments)
        {
            ValidateDevice();

            int splitPackageFileCount = splitPackageFilePaths.Count();

            void OnMainSyncProgressChanged(string? sender, SyncProgressChangedEventArgs args) =>
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(sender is null ? 1 : 0, splitPackageFileCount + 1, args.ProgressPercentage / 2));

            string baseRemoteFilePath = await SyncPackageToDeviceAsync(basePackageFilePath, OnMainSyncProgressChanged, cancellationToken).ConfigureAwait(false);

            int progressCount = 1;
            Dictionary<string, double> progress = new(splitPackageFileCount);
            void OnSplitSyncProgressChanged(string? sender, SyncProgressChangedEventArgs args)
            {
                lock (progress)
                {
                    if (sender is null)
                    {
                        progressCount++;
                    }
                    else if (sender is string path)
                    {
                        // Note: The progress may be less than the previous progress when async.
                        if (progress.TryGetValue(path, out double oldValue)
                            && oldValue > args.ProgressPercentage)
                        {
                            return;
                        }
                        progress[path] = args.ProgressPercentage;
                    }
                    double present = (progress.Values.Select(x => x / splitPackageFileCount).Sum() + 100) / 2;
                    InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(progressCount, splitPackageFileCount + 1, present));
                }
            }

            string[] splitRemoteFilePaths = await splitPackageFilePaths.Select(x => SyncPackageToDeviceAsync(x, OnSplitSyncProgressChanged, cancellationToken)).ToArrayAsync().ConfigureAwait(false);

            if (splitRemoteFilePaths.Length < splitPackageFileCount)
            {
                throw new PackageInstallationException($"{nameof(SyncPackageToDeviceAsync)} failed. {splitPackageFileCount} should process but only {splitRemoteFilePaths.Length} processed.");
            }

            await InstallMultipleRemotePackageAsync(baseRemoteFilePath, splitRemoteFilePaths, cancellationToken, arguments);

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(0, splitRemoteFilePaths.Length + 1, PackageInstallProgressState.PostInstall));
            int count = 0;
            await Extensions.WhenAll(splitRemoteFilePaths.Select(async x =>
            {
                count++;
                await RemoveRemotePackageAsync(x, cancellationToken).ConfigureAwait(false);
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(count, splitRemoteFilePaths.Length + 1, PackageInstallProgressState.PostInstall));
            })).ConfigureAwait(false);

            if (count < splitRemoteFilePaths.Length)
            {
                throw new PackageInstallationException($"{nameof(RemoveRemotePackageAsync)} failed. {splitRemoteFilePaths.Length} should process but only {count} processed.");
            }

            await RemoveRemotePackageAsync(baseRemoteFilePath, cancellationToken).ConfigureAwait(false);
            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(++count, splitRemoteFilePaths.Length + 1, PackageInstallProgressState.PostInstall));

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Finished));
        }

        /// <summary>
        /// Installs Android multiple application on device.
        /// </summary>
        /// <param name="splitPackageFilePaths">The absolute split app file system paths to file on local host to install.</param>
        /// <param name="packageName">The absolute package name of the base app.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallMultiplePackageAsync(IEnumerable<string> splitPackageFilePaths, string packageName, params string[] arguments) =>
            InstallMultiplePackageAsync(splitPackageFilePaths, packageName, default, arguments);

        /// <summary>
        /// Installs Android multiple application on device.
        /// </summary>
        /// <param name="splitPackageFilePaths">The absolute split app file system paths to file on local host to install.</param>
        /// <param name="packageName">The absolute package name of the base app.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallMultiplePackageAsync(IEnumerable<string> splitPackageFilePaths, string packageName, CancellationToken cancellationToken, params string[] arguments)
        {
            ValidateDevice();

            int splitPackageFileCount = splitPackageFilePaths.Count();

            int progressCount = 0;
            Dictionary<string, double> progress = new(splitPackageFileCount);
            void OnSyncProgressChanged(string? sender, SyncProgressChangedEventArgs args)
            {
                lock (progress)
                {
                    if (sender is null)
                    {
                        progressCount++;
                    }
                    else if (sender is string path)
                    {
                        // Note: The progress may be less than the previous progress when async.
                        if (progress.TryGetValue(path, out double oldValue)
                            && oldValue > args.ProgressPercentage)
                        {
                            return;
                        }
                        progress[path] = args.ProgressPercentage;
                    }
                    double present = progress.Values.Select(x => x / splitPackageFileCount).Sum();
                    InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(progressCount, splitPackageFileCount, present));
                }
            }

            string[] splitRemoteFilePaths = await splitPackageFilePaths.Select(x => SyncPackageToDeviceAsync(x, OnSyncProgressChanged, cancellationToken)).ToArrayAsync().ConfigureAwait(false);

            if (splitRemoteFilePaths.Length < splitPackageFileCount)
            {
                throw new PackageInstallationException($"{nameof(SyncPackageToDeviceAsync)} failed. {splitPackageFileCount} should process but only {splitRemoteFilePaths.Length} processed.");
            }

            await InstallMultipleRemotePackageAsync(splitRemoteFilePaths, packageName, cancellationToken, arguments);

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(0, splitRemoteFilePaths.Length, PackageInstallProgressState.PostInstall));
            int count = 0;
            await Extensions.WhenAll(splitRemoteFilePaths.Select(async x =>
            {
                count++;
                await RemoveRemotePackageAsync(x, cancellationToken).ConfigureAwait(false);
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(count, splitRemoteFilePaths.Length, PackageInstallProgressState.PostInstall));
            })).ConfigureAwait(false);

            if (count < splitRemoteFilePaths.Length)
            {
                throw new PackageInstallationException($"{nameof(RemoveRemotePackageAsync)} failed. {splitRemoteFilePaths.Length} should process but only {count} processed.");
            }

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Finished));
        }

        /// <summary>
        /// Installs the multiple application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="baseRemoteFilePath">The absolute base app file path to package file on device.</param>
        /// <param name="splitRemoteFilePaths">The absolute split app file paths to package file on device.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallMultipleRemotePackageAsync(string baseRemoteFilePath, IEnumerable<string> splitRemoteFilePaths, params string[] arguments) =>
            InstallMultipleRemotePackageAsync(baseRemoteFilePath, splitRemoteFilePaths, default, arguments);

        /// <summary>
        /// Installs the multiple application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="baseRemoteFilePath">The absolute base app file path to package file on device.</param>
        /// <param name="splitRemoteFilePaths">The absolute split app file paths to package file on device.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallMultipleRemotePackageAsync(string baseRemoteFilePath, IEnumerable<string> splitRemoteFilePaths, CancellationToken cancellationToken, params string[] arguments)
        {
            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.CreateSession));

            ValidateDevice();

            string session = await CreateInstallSessionAsync(cancellationToken: cancellationToken, arguments: arguments).ConfigureAwait(false);

            int splitRemoteFileCount = splitRemoteFilePaths.Count();

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(0, splitRemoteFileCount + 1, PackageInstallProgressState.WriteSession));

            await WriteInstallSessionAsync(session, "base", baseRemoteFilePath, cancellationToken).ConfigureAwait(false);

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(1, splitRemoteFileCount + 1, PackageInstallProgressState.WriteSession));

            int count = 0;
            await Extensions.WhenAll(splitRemoteFilePaths.Select(async (splitRemoteFilePath) =>
            {
                await WriteInstallSessionAsync(session, $"split{count++}", splitRemoteFilePath, cancellationToken).ConfigureAwait(false);
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(count, splitRemoteFileCount + 1, PackageInstallProgressState.WriteSession));
            })).ConfigureAwait(false);

            if (count < splitRemoteFileCount)
            {
                throw new PackageInstallationException($"{nameof(WriteInstallSessionAsync)} failed. {splitRemoteFileCount} should process but only {count} processed.");
            }

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Installing));

            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, $"pm install-commit {session}", receiver, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }
        }

        /// <summary>
        /// Installs the multiple application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="splitRemoteFilePaths">The absolute split app file paths to package file on device.</param>
        /// <param name="packageName">The absolute package name of the base app.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task InstallMultipleRemotePackageAsync(IEnumerable<string> splitRemoteFilePaths, string packageName, params string[] arguments) =>
            InstallMultipleRemotePackageAsync(splitRemoteFilePaths, packageName, default, arguments);

        /// <summary>
        /// Installs the multiple application package that was pushed to a temporary location on the device.
        /// </summary>
        /// <param name="splitRemoteFilePaths">The absolute split app file paths to package file on device.</param>
        /// <param name="packageName">The absolute package name of the base app.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task InstallMultipleRemotePackageAsync(IEnumerable<string> splitRemoteFilePaths, string packageName, CancellationToken cancellationToken, params string[] arguments)
        {
            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.CreateSession));

            ValidateDevice();

            string session = await CreateInstallSessionAsync(packageName, cancellationToken, arguments).ConfigureAwait(false);

            int splitRemoteFileCount = splitRemoteFilePaths.Count();

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(0, splitRemoteFileCount, PackageInstallProgressState.WriteSession));

            int count = 0;
            await Extensions.WhenAll(splitRemoteFilePaths.Select(async (splitRemoteFilePath) =>
            {
                await WriteInstallSessionAsync(session, $"split{count++}", splitRemoteFilePath, cancellationToken).ConfigureAwait(false);
                InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(count, splitRemoteFileCount, PackageInstallProgressState.WriteSession));
            })).ConfigureAwait(false);

            if (count < splitRemoteFileCount)
            {
                throw new PackageInstallationException($"{nameof(WriteInstallSessionAsync)} failed. {splitRemoteFileCount} should process but only {count} processed.");
            }

            InstallProgressChanged?.Invoke(this, new InstallProgressEventArgs(PackageInstallProgressState.Installing));

            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, $"pm install-commit {session}", receiver, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }
        }

        /// <summary>
        /// Uninstalls a package from the device.
        /// </summary>
        /// <param name="packageName">The name of the package to uninstall.</param>
        /// <param name="arguments">The arguments to pass to <c>pm uninstall</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task UninstallPackageAsync(string packageName, params string[] arguments) =>
            UninstallPackageAsync(packageName, default, arguments);

        /// <summary>
        /// Uninstalls a package from the device.
        /// </summary>
        /// <param name="packageName">The name of the package to uninstall.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm uninstall</c>.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public virtual async Task UninstallPackageAsync(string packageName, CancellationToken cancellationToken, params string[] arguments)
        {
            ValidateDevice();

            StringBuilder requestBuilder = new StringBuilder().Append("pm uninstall");

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    _ = requestBuilder.AppendFormat(" {0}", argument);
                }
            }

            _ = requestBuilder.AppendFormat(" {0}", packageName);

            string cmd = requestBuilder.ToString();
            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, cmd, receiver, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }
        }

        /// <summary>
        /// Requests the version information from the device.
        /// </summary>
        /// <param name="packageName">The name of the package from which to get the application version.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which return the <see cref="VersionInfo"/> of target application.</returns>
        public virtual async Task<VersionInfo> GetVersionInfoAsync(string packageName, CancellationToken cancellationToken = default)
        {
            ValidateDevice();

            VersionInfoReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, $"dumpsys package {packageName}", receiver, cancellationToken).ConfigureAwait(false);
            return receiver.VersionInfo;
        }

        /// <summary>
        /// Pushes a file to device
        /// </summary>
        /// <param name="localFilePath">The absolute path to file on local host.</param>
        /// <param name="progress">An optional parameter which, when specified, returns progress notifications.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which return the destination path on device for file.</returns>
        /// <exception cref="IOException">If fatal error occurred when pushing file.</exception>
        protected virtual async Task<string> SyncPackageToDeviceAsync(string localFilePath, Action<string?, SyncProgressChangedEventArgs>? progress, CancellationToken cancellationToken = default)
        {
            progress?.Invoke(localFilePath, new SyncProgressChangedEventArgs(0, 100));

            ValidateDevice();

            try
            {
                string packageFileName = Path.GetFileName(localFilePath);

                // only root has access to /data/local/tmp/... not sure how adb does it then...
                // workitem: 16823
                // workitem: 19711
                string remoteFilePath = LinuxPath.Combine(TempInstallationDirectory, RandomString(16) + ".apk");

                logger.LogDebug("Uploading {0} onto device '{1}'", packageFileName, Device.Serial);

                using (ISyncService sync = syncServiceFactory(AdbClient, Device))
                {
                    void OnSyncProgressChanged(object? sender, SyncProgressChangedEventArgs args) => progress!(localFilePath, args);

                    if (progress != null)
                    {
                        sync.SyncProgressChanged -= OnSyncProgressChanged;
                        sync.SyncProgressChanged += OnSyncProgressChanged;
                    }

#if NETCOREAPP3_0_OR_GREATER
                    await
#endif
                    using FileStream stream = File.OpenRead(localFilePath);

                    logger.LogDebug("Uploading file onto device '{0}'", Device.Serial);

                    // As C# can't use octal, the octal literal 666 (rw-Permission) is here converted to decimal (438)
                    await sync.PushAsync(stream, remoteFilePath, 438, File.GetLastWriteTime(localFilePath), null, cancellationToken).ConfigureAwait(false);

                    sync.SyncProgressChanged -= OnSyncProgressChanged;
                }

                return remoteFilePath;
            }
            catch (IOException e)
            {
                logger.LogError(e, "Unable to open sync connection! reason: {0}", e.Message);
                throw;
            }
            finally
            {
                progress?.Invoke(null, new SyncProgressChangedEventArgs(100, 100));
            }
            
            string RandomString(int length)
            {
                var random = new Random();
                const string chars = "ancdefghijlkmnopqrstuvwxyz0123456789";
                return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }

        /// <summary>
        /// Remove a file from device.
        /// </summary>
        /// <param name="remoteFilePath">Path on device of file to remove.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        /// <exception cref="IOException">If file removal failed.</exception>
        protected virtual async Task RemoveRemotePackageAsync(string remoteFilePath, CancellationToken cancellationToken = default)
        {
            // now we delete the app we synced
            try
            {
                await AdbClient.ExecuteShellCommandAsync(Device, $"rm \"{remoteFilePath}\"", cancellationToken).ConfigureAwait(false);
            }
            catch (IOException e)
            {
                logger.LogError(e, "Failed to delete temporary package: {0}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Like "install", but starts an install session.
        /// </summary>
        /// <param name="packageName">The absolute package name of the base app.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <param name="arguments">The arguments to pass to <c>pm install-create</c>.</param>
        /// <returns>A <see cref="Task"/> which return the session ID.</returns>
        protected virtual async Task<string> CreateInstallSessionAsync(string? packageName = null, CancellationToken cancellationToken = default, params string[] arguments)
        {
            ValidateDevice();

            StringBuilder requestBuilder = new StringBuilder().Append("pm install-create");

            if (!StringExtensions.IsNullOrWhiteSpace(packageName))
            {
                _ = requestBuilder.AppendFormat(" -p {0}", packageName);
            }

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    _ = requestBuilder.AppendFormat(" {0}", argument);
                }
            }

            string cmd = requestBuilder.ToString();
            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, cmd, receiver, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(receiver.SuccessMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }

            string result = receiver.SuccessMessage ?? throw new AdbException($"The {nameof(result)} of {nameof(CreateInstallSessionAsync)} is null.");
            int arr = result.IndexOf(']') - 1 - result.IndexOf('[');
            string session = result.Substring(result.IndexOf('[') + 1, arr);

            return session;
        }

        /// <summary>
        /// Write an apk into the given install session.
        /// </summary>
        /// <param name="session">The session ID of the install session.</param>
        /// <param name="apkName">The name of the application.</param>
        /// <param name="path">The absolute file path to package file on device.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        protected virtual async Task WriteInstallSessionAsync(string session, string apkName, string path, CancellationToken cancellationToken = default)
        {
            ValidateDevice();

            InstallOutputReceiver receiver = new();
            await AdbClient.ExecuteShellCommandAsync(Device, $"pm install-write {session} {apkName}.apk \"{path}\"", receiver, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(receiver.ErrorMessage))
            {
                throw new PackageInstallationException(receiver.ErrorMessage);
            }
        }
    }
}
#endif