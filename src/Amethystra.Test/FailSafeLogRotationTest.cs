using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amethystra.Diagnostics;
using Mio;
using Mio.Destructive;

namespace Amethystra.Test;

[TestClass]
[DoNotParallelize]
public sealed class FailSafeLogRotationTest
{
    private DirectoryPath _tempRoot = null!;
    private FilePath _logFilePath = null!;

    [TestInitialize]
    public void Initialize()
    {
        this._tempRoot = DirectoryPath.GetTempDirectory()
            .ChildDirectory("Amethystra.FailSafeLog.Test")
            .ChildDirectory(Guid.NewGuid().ToString("N"))
            .EnsureCreated();
        this._logFilePath = this._tempRoot.ChildFile("FailSafeLog.json");

#if DEBUG
        FailSafeLog.SetTestOverrides(this._logFilePath, 4 * 1024, 5);
#else
        Assert.Inconclusive("FailSafeLog.SetTestOverrides は DEBUG のみ有効です。テストは Debug 構成で実行してください。");
#endif
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
    public void Rotate_When_Exceeds_MaxBytes_Creates_Generation1()
    {
        WriteManyEntries(50, 1500);

        var gen1 = this._tempRoot.ChildFile("FailSafeLog.1.json");

        Assert.IsTrue(this._logFilePath.Exists());
        Assert.IsTrue(gen1.Exists());
    }

    [TestMethod]
    public void Rotate_Keeps_Only_5_Generations()
    {
        WriteManyEntries(300, 1500);

        for (var i = 1; i <= 5; i++)
        {
            var path = this._tempRoot.ChildFile($"FailSafeLog.{i}.json");
            Assert.IsTrue(path.Exists(), $"Expected generation {i} to exist.");
        }

        var gen6 = this._tempRoot.ChildFile("FailSafeLog.6.json");
        Assert.IsFalse(gen6.Exists());
    }

    [TestMethod]
    public void Parallel_Writes_Do_Not_Create_Extra_Generations_Or_Throw()
    {
        var payload = new string('C', 1500);

        Parallel.For(0, 400, i =>
        {
            FailSafeLog.Info(
                "Parallel",
                "Test",
                new Dictionary<string, object?>
                {
                    ["payload"] = payload,
                    ["i"] = i,
                });
        });

        var gen6 = this._tempRoot.ChildFile("FailSafeLog.6.json");
        Assert.IsFalse(gen6.Exists());
        Assert.IsTrue(this._logFilePath.Exists());
    }

    [TestMethod]
    public void BeginOperation_Writes_Begin_And_End_Lines()
    {
        using (FailSafeLog.BeginOperation("Op", "Test"))
        {
            FailSafeLog.Info("Inside", "Test");
        }

        Assert.IsTrue(this._logFilePath.Exists());

        var lines = this._logFilePath.ReadAllLines();
        Assert.IsTrue(lines.Any(x => x.Contains("\"Message\":\"Begin: Op\"", StringComparison.Ordinal)));
        Assert.IsTrue(lines.Any(x => x.Contains("\"Message\":\"End: Op\"", StringComparison.Ordinal)));
    }

    private static void WriteManyEntries(int entryCount, int payloadSize)
    {
        var payload = new string('A', payloadSize);

        for (var i = 0; i < entryCount; i++)
        {
            FailSafeLog.Info(
                "Test",
                "Rotate",
                new Dictionary<string, object?>
                {
                    ["payload"] = payload,
                    ["i"] = i,
                });
        }
    }
}
