using System;
using Amethystra.Diagnostics;
using Mio;
using Mio.Destructive;

namespace Amethystra.Test;

[TestClass]
[DoNotParallelize]
public sealed class AppLogRotationTest
{
    private DirectoryPath _tempRoot = DirectoryPath.GetTempDirectory();
    private FilePath _logFilePath = DirectoryPath.GetTempDirectory().ChildFile("App.log");

    [TestInitialize]
    public void Initialize()
    {
        this._tempRoot = DirectoryPath.GetTempDirectory()
            .ChildDirectory("Amethystra.AppLog.Test")
            .ChildDirectory(Guid.NewGuid().ToString("N"))
            .EnsureCreated();
        this._logFilePath = this._tempRoot.ChildFile("App.log");
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (this._tempRoot.Exists())
            {
                this._tempRoot.AsDestructive().DeleteAll();
            }
        }
        catch
        {
            // テスト後処理なので握りつぶします
        }
    }

    [TestMethod]
    public void Rotate_Reopens_Current_LogFile()
    {
        var options = new AppLogOptions(
            this._logFilePath,
            AppLogOptions.UTF8NoBOM,
            MaxLogBytes: 4 * 1024,
            MaxGenerations: 5);
        var payload = new string('A', 1500);

        using (var log = new AppLog(options))
        {
            var logger = log.For<AppLogRotationTest>();
            for (var i = 0; i < 20; i++)
            {
                logger.Info($"entry-{i}: {payload}");
                Thread.Sleep(120);
            }
        }

        var generation1 = this._tempRoot.ChildFile("App.1.log");

        Assert.IsTrue(generation1.Exists(), "Expected generation-1 log file to exist after rotation.");
        Assert.IsTrue(this._logFilePath.Exists(), "Expected current log file to exist after rotation.");
    }
}
