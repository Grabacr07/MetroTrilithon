using System;
using System.Threading.Tasks;
using Amethystra.Serialization;
using Microsoft.Extensions.Time.Testing;
using Mio;
using Mio.Destructive;
using R3;

namespace Amethystra.Test;

[TestClass]
[DoNotParallelize]
public sealed class ReactiveSettingsBaseTest
{
    private DirectoryPath _tempRoot = DirectoryPath.GetTempDirectory();
    private FilePath _settingsFilePath = DirectoryPath.GetTempDirectory().ChildFile("settings.json");

    [TestInitialize]
    public void Initialize()
    {
        this._tempRoot = DirectoryPath.GetTempDirectory()
            .ChildDirectory("Amethystra.Settings.Test")
            .ChildDirectory(Guid.NewGuid().ToString("N"))
            .EnsureCreated();
        this._settingsFilePath = this._tempRoot.ChildFile("settings.json");
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

    private sealed class TestSettings(FilePath filePath, TimeProvider? timeProvider = null)
        : ReactiveSettingsBase(filePath, null, timeProvider)
    {
        public ReactiveProperty<string> Name { get; } = new("default-name");

        public ReactiveProperty<int> Count { get; } = new(0);

        protected override TimeSpan LoadThrottlingDuration => TimeSpan.FromMilliseconds(50);

        protected override TimeSpan SaveThrottlingDuration => TimeSpan.FromMilliseconds(50);
    }

    [TestMethod]
    public async Task IsInitialized_BecomesTrue_WhenFileDoesNotExist()
    {
        using var settings = new TestSettings(this._settingsFilePath);
        await settings.EnsureLoadedAsync();

        Assert.IsTrue(settings.IsInitialized.CurrentValue);
    }

    [TestMethod]
    public async Task IsInitialized_BecomesTrue_WhenFileExists()
    {
        const string json = """
                            {
                              "TestSettings": {
                                "Name": "loaded"
                              }
                            }
                            """;
        await this._settingsFilePath.AsDestructive().WriteAsync(json);

        using var settings = new TestSettings(this._settingsFilePath);
        await settings.EnsureLoadedAsync();

        Assert.IsTrue(settings.IsInitialized.CurrentValue);
    }

    [TestMethod]
    public async Task Load_RestoresPropertyValues_FromExistingFile()
    {
        const string json = """
                            {
                              "TestSettings": {
                                "Name": "hello",
                                "Count": 42
                              }
                            }
                            """;
        await this._settingsFilePath.AsDestructive().WriteAsync(json);

        using var settings = new TestSettings(this._settingsFilePath);
        await settings.EnsureLoadedAsync();

        Assert.AreEqual("hello", settings.Name.Value);
        Assert.AreEqual(42, settings.Count.Value);
    }

    [TestMethod]
    public async Task SaveAsync_PersistsChangedValues()
    {
        using (var settings = new TestSettings(this._settingsFilePath))
        {
            await settings.EnsureLoadedAsync();

            settings.Name.Value = "persisted";
            settings.Count.Value = 99;
            await settings.SaveAsync();
        }

        using var settings2 = new TestSettings(this._settingsFilePath);
        await settings2.EnsureLoadedAsync();

        Assert.AreEqual("persisted", settings2.Name.Value);
        Assert.AreEqual(99, settings2.Count.Value);
    }

    [TestMethod]
    public async Task SaveAsync_OmitsDefaultValues_FromJson()
    {
        using var settings = new TestSettings(this._settingsFilePath);
        await settings.EnsureLoadedAsync();

        settings.Name.Value = "non-default";
        await settings.SaveAsync();

        var json = await this._settingsFilePath.ReadAllTextAsync();
        Assert.Contains("non-default", json);
        Assert.DoesNotContain("\"Count\"", json);
    }

    [TestMethod]
    public async Task AutoSave_WritesFile_AfterDebounceWindow()
    {
        var fakeTime = new FakeTimeProvider();
        using var settings = new TestSettings(this._settingsFilePath, fakeTime);
        await settings.EnsureLoadedAsync();

        settings.Name.Value = "auto-saved";

        // File must not exist yet — debounce has not fired
        Assert.IsFalse(this._settingsFilePath.Exists(), "File should not be written before debounce fires");

        // Advance past SaveThrottlingDuration to fire the debounce timer
        fakeTime.Advance(TimeSpan.FromMilliseconds(200));

        // Allow the async file write to complete on the thread pool
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.IsTrue(this._settingsFilePath.Exists(), "File should be written after debounce fires");
        var json = await this._settingsFilePath.ReadAllTextAsync();
        Assert.Contains("auto-saved", json);
    }

    [TestMethod]
    public async Task AutoSave_Debounce_RapidChanges()
    {
        var fakeTime = new FakeTimeProvider();
        using var settings = new TestSettings(this._settingsFilePath, fakeTime);
        await settings.EnsureLoadedAsync();

        // Rapid changes within the debounce window
        settings.Name.Value = "first";
        settings.Name.Value = "second";
        settings.Name.Value = "last";

        fakeTime.Advance(TimeSpan.FromMilliseconds(200));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        var json = await this._settingsFilePath.ReadAllTextAsync();
        Assert.Contains("last", json);
        Assert.DoesNotContain("\"first\"", json);
    }
}
