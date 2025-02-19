﻿using NSubstitute;
using System;
using System.IO;
using Xunit;

namespace AdvancedSharpAdbClient.DeviceCommands.Tests
{
    public partial class PackageManagerTests
    {
        [Fact]
        public void ConstructorNullTest()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new PackageManager(null, null));
            _ = Assert.Throws<ArgumentNullException>(() => new PackageManager(null, new DeviceData()));
            _ = Assert.Throws<ArgumentNullException>(() => new PackageManager(Substitute.For<IAdbClient>(), null));
        }

        [Theory]
        [InlineData("package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d", "com.android.gallery3d", "/system/app/Gallery2/Gallery2.apk")]
        [InlineData("package:mwc2015.be", "mwc2015.be", "")]
        public void PackagesPropertyTest(string command, string packageName, string path)
        {
            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            DummyAdbClient client = new();
            client.Commands["shell:pm list packages -f"] = command;
            PackageManager manager = new(client, device);

            Assert.True(manager.Packages.ContainsKey(packageName));
            Assert.Equal(path, manager.Packages[packageName]);
        }

        [Fact]
        public void InstallRemotePackageTest()
        {
            DummyAdbClient adbClient = new();

            adbClient.Commands["shell:pm list packages -f"] = "package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d";
            adbClient.Commands["shell:pm install \"/data/test.apk\""] = "Success";
            adbClient.Commands["shell:pm install -r -t \"/data/test.apk\""] = "Success";

            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            PackageManager manager = new(adbClient, device);

            using (EventTestHost eventTestHost = new(manager, PackageInstallProgressState.Installing))
            {
                manager.InstallRemotePackage("/data/test.apk");
            }

            Assert.Equal(2, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install \"/data/test.apk\"", adbClient.ReceivedCommands[1]);

            adbClient.ReceivedCommands.Clear();

            using (EventTestHost eventTestHost = new(manager, PackageInstallProgressState.Installing))
            {
                manager.InstallRemotePackage("/data/test.apk", "-r", "-t");
            }

            Assert.Single(adbClient.ReceivedCommands);
            Assert.Equal("shell:pm install -r -t \"/data/test.apk\"", adbClient.ReceivedCommands[0]);
        }

        [Fact]
        public void InstallPackageTest()
        {
            DummySyncService syncService = new();

            DummyAdbClient adbClient = new();

            adbClient.Commands["shell:pm list packages -f"] = "package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d";
            adbClient.Commands["shell:pm install \"/data/local/tmp/test.txt\""] = "Success";
            adbClient.Commands["shell:rm \"/data/local/tmp/test.txt\""] = string.Empty;

            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            PackageManager manager = new(adbClient, device, (c, d) => syncService);

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.Uploading,
                PackageInstallProgressState.Installing,
                PackageInstallProgressState.PostInstall,
                PackageInstallProgressState.Finished))
            {
                manager.InstallPackage("Assets/test.txt", false);
            }

            Assert.Equal(3, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install \"/data/local/tmp/test.txt\"", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:rm \"/data/local/tmp/test.txt\"", adbClient.ReceivedCommands[2]);

            Assert.Single(syncService.UploadedFiles);
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/test.txt"));
        }

        [Fact]
        public void InstallMultipleRemotePackageTest()
        {
            DummyAdbClient adbClient = new();

            adbClient.Commands["shell:pm list packages -f"] = "package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d";
            adbClient.Commands["shell:pm install-create"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-create -r -t"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-create -p com.google.android.gms"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-create -p com.google.android.gms -r -t"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-write 936013062 base.apk \"/data/base.apk\""] = "Success";
            adbClient.Commands["shell:pm install-write 936013062 split0.apk \"/data/split-dpi.apk\""] = "Success";
            adbClient.Commands["shell:pm install-write 936013062 split1.apk \"/data/split-abi.apk\""] = "Success";
            adbClient.Commands["shell:pm install-commit 936013062"] = "Success";

            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            PackageManager manager = new(adbClient, device);

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing))
            {
                manager.InstallMultipleRemotePackage("/data/base.apk", ["/data/split-dpi.apk", "/data/split-abi.apk"]);
            }

            Assert.Equal(6, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 base.apk \"/data/base.apk\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/split-dpi.apk\"", adbClient.ReceivedCommands[3]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/split-abi.apk\"", adbClient.ReceivedCommands[4]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[5]);

            adbClient.ReceivedCommands.Clear();


            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing))
            {
                manager.InstallMultipleRemotePackage("/data/base.apk", ["/data/split-dpi.apk", "/data/split-abi.apk"], "-r", "-t");
            }

            Assert.Equal(5, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create -r -t", adbClient.ReceivedCommands[0]);
            Assert.Equal("shell:pm install-write 936013062 base.apk \"/data/base.apk\"", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/split-dpi.apk\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/split-abi.apk\"", adbClient.ReceivedCommands[3]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[4]);

            adbClient.ReceivedCommands.Clear();

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing))
            {
                manager.InstallMultipleRemotePackage(["/data/split-dpi.apk", "/data/split-abi.apk"], "com.google.android.gms");
            }

            Assert.Equal(4, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create -p com.google.android.gms", adbClient.ReceivedCommands[0]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/split-dpi.apk\"", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/split-abi.apk\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[3]);

            adbClient.ReceivedCommands.Clear();

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing))
            {
                manager.InstallMultipleRemotePackage(["/data/split-dpi.apk", "/data/split-abi.apk"], "com.google.android.gms", "-r", "-t");
            }

            Assert.Equal(4, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create -p com.google.android.gms -r -t", adbClient.ReceivedCommands[0]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/split-dpi.apk\"", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/split-abi.apk\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[3]);
        }

        [Fact]
        public void InstallMultiplePackageTest()
        {
            DummySyncService syncService = new();

            DummyAdbClient adbClient = new();

            adbClient.Commands["shell:pm list packages -f"] = "package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d";
            adbClient.Commands["shell:pm install-create"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-create -p com.google.android.gms"] = "Success: created install session [936013062]";
            adbClient.Commands["shell:pm install-write 936013062 base.apk \"/data/local/tmp/test.txt\""] = "Success";
            adbClient.Commands["shell:pm install-write 936013062 split0.apk \"/data/local/tmp/gapps.txt\""] = "Success";
            adbClient.Commands["shell:pm install-write 936013062 split1.apk \"/data/local/tmp/logcat.bin\""] = "Success";
            adbClient.Commands["shell:pm install-commit 936013062"] = "Success";
            adbClient.Commands["shell:rm \"/data/local/tmp/test.txt\""] = string.Empty;
            adbClient.Commands["shell:rm \"/data/local/tmp/gapps.txt\""] = string.Empty;
            adbClient.Commands["shell:rm \"/data/local/tmp/logcat.bin\""] = string.Empty;

            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            PackageManager manager = new(adbClient, device, (c, d) => syncService);

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.Uploading,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing,
                PackageInstallProgressState.PostInstall,
                PackageInstallProgressState.Finished))
            {
                manager.InstallMultiplePackage("Assets/test.txt", new [] {"Assets/gapps.txt", "Assets/logcat.bin"}, false);
            }

            Assert.Equal(9, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 base.apk \"/data/local/tmp/test.txt\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/local/tmp/gapps.txt\"", adbClient.ReceivedCommands[3]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/local/tmp/logcat.bin\"", adbClient.ReceivedCommands[4]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[5]);
            Assert.Equal("shell:rm \"/data/local/tmp/gapps.txt\"", adbClient.ReceivedCommands[6]);
            Assert.Equal("shell:rm \"/data/local/tmp/logcat.bin\"", adbClient.ReceivedCommands[7]);
            Assert.Equal("shell:rm \"/data/local/tmp/test.txt\"", adbClient.ReceivedCommands[8]);

            Assert.Equal(3, syncService.UploadedFiles.Count);
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/test.txt"));
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/gapps.txt"));
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/logcat.bin"));

            syncService.UploadedFiles.Clear();
            adbClient.ReceivedCommands.Clear();

            using (EventTestHost eventTestHost = new(
                manager,
                PackageInstallProgressState.Uploading,
                PackageInstallProgressState.CreateSession,
                PackageInstallProgressState.WriteSession,
                PackageInstallProgressState.Installing,
                PackageInstallProgressState.PostInstall,
                PackageInstallProgressState.Finished))
            {
                manager.InstallMultiplePackage(new [] {"Assets/gapps.txt", "Assets/logcat.bin"}, "com.google.android.gms", false);
            }

            Assert.Equal(6, adbClient.ReceivedCommands.Count);
            Assert.Equal("shell:pm install-create -p com.google.android.gms", adbClient.ReceivedCommands[0]);
            Assert.Equal("shell:pm install-write 936013062 split0.apk \"/data/local/tmp/gapps.txt\"", adbClient.ReceivedCommands[1]);
            Assert.Equal("shell:pm install-write 936013062 split1.apk \"/data/local/tmp/logcat.bin\"", adbClient.ReceivedCommands[2]);
            Assert.Equal("shell:pm install-commit 936013062", adbClient.ReceivedCommands[3]);
            Assert.Equal("shell:rm \"/data/local/tmp/gapps.txt\"", adbClient.ReceivedCommands[4]);
            Assert.Equal("shell:rm \"/data/local/tmp/logcat.bin\"", adbClient.ReceivedCommands[5]);

            Assert.Equal(2, syncService.UploadedFiles.Count);
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/gapps.txt"));
            Assert.True(syncService.UploadedFiles.ContainsKey("/data/local/tmp/logcat.bin"));
        }

        [Fact]
        public void UninstallPackageTest()
        {
            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            DummyAdbClient client = new();
            client.Commands["shell:pm list packages -f"] = "package:/system/app/Gallery2/Gallery2.apk=com.android.gallery3d";
            client.Commands["shell:pm uninstall com.android.gallery3d"] = "Success";
            PackageManager manager = new(client, device);

            // Command should execute correctly; if the wrong command is passed an exception
            // would be thrown.
            manager.UninstallPackage("com.android.gallery3d");
        }

        [Fact]
        public void GetPackageVersionInfoTest()
        {
            DeviceData device = new()
            {
                State = DeviceState.Online
            };

            DummyAdbClient client = new();
            client.Commands["shell:dumpsys package com.google.android.gms"] = File.ReadAllText("Assets/gapps.txt");
            PackageManager manager = new(client, device, skipInit: true);

            VersionInfo versionInfo = manager.GetVersionInfo("com.google.android.gms");
            Assert.Equal(11062448, versionInfo.VersionCode);
            Assert.Equal("11.0.62 (448-160311229)", versionInfo.VersionName);
        }

        private class EventTestHost : IDisposable
        {
            private readonly PackageManager manager;
            private readonly PackageInstallProgressState[] states;

            private PackageInstallProgressState? state;
            private int packageFinished;
            private int packageRequired;
            private double uploadProgress;

            private int step = 0;

            public EventTestHost(PackageManager manager, params PackageInstallProgressState[] states)
            {
                this.states = states;
                this.manager = manager;
                manager.InstallProgressChanged += OnInstallProgressChanged;
            }

            public void OnInstallProgressChanged(object sender, InstallProgressEventArgs args)
            {
                if (args.State == state)
                {
                    Assert.True(uploadProgress <= args.UploadProgress, $"{nameof(args.UploadProgress)}: {args.UploadProgress} is less than {uploadProgress}.");
                    Assert.True(packageFinished <= args.PackageFinished, $"{nameof(args.PackageFinished)}: {args.PackageFinished} is less than {packageFinished}.");
                }
                else
                {
                    Assert.Equal(states[step++], args.State);
                }

                if (args.State is
                    PackageInstallProgressState.CreateSession
                    or PackageInstallProgressState.Installing
                    or PackageInstallProgressState.Finished)
                {
                    Assert.Equal(0, args.UploadProgress);
                    Assert.Equal(0, args.PackageRequired);
                    Assert.Equal(0, args.PackageFinished);
                }
                else
                {
                    if (packageRequired == 0)
                    {
                        packageRequired = args.PackageRequired;
                    }
                    else
                    {
                        Assert.Equal(packageRequired, args.PackageRequired);
                    }
                }

                state = args.State;
                packageFinished = args.PackageFinished;
                uploadProgress = args.UploadProgress;
            }

            public void Dispose()
            {
                manager.InstallProgressChanged -= OnInstallProgressChanged;
            }
        }
    }
}
