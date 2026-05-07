using System.Text.Json;
using Amethystra.Serialization;
using Mio;
using Mio.Destructive;

namespace Amethystra.Test;

[TestClass]
public sealed class MioPathJsonConverterFactoryTest
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new MioPathJsonConverterFactory() },
    };

    // ── CanConvert ──────────────────────────────────────────────────

    [TestMethod]
    public void CanConvert_FilePath_ReturnsTrue()
    {
        var factory = new MioPathJsonConverterFactory();
        Assert.IsTrue(factory.CanConvert(typeof(FilePath)));
    }

    [TestMethod]
    public void CanConvert_DirectoryPath_ReturnsTrue()
    {
        var factory = new MioPathJsonConverterFactory();
        Assert.IsTrue(factory.CanConvert(typeof(DirectoryPath)));
    }

    [TestMethod]
    public void CanConvert_String_ReturnsFalse()
    {
        var factory = new MioPathJsonConverterFactory();
        Assert.IsFalse(factory.CanConvert(typeof(string)));
    }

    // ── FilePath - Serialize ─────────────────────────────────────────

    [TestMethod]
    public void FilePath_Serialize_WritesPathAsString()
    {
        var path = new FilePath(@"C:\Users\test\file.txt");
        var json = JsonSerializer.Serialize<FilePath?>(path, _options);

        Assert.StartsWith("\"", json, "Should be a JSON string, not an object");
        Assert.Contains("file.txt", json, $"JSON should contain the filename, got: {json}");
    }

    [TestMethod]
    public void FilePath_Serialize_NullValue_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<FilePath?>(null, _options);
        Assert.AreEqual("null", json);
    }

    // ── FilePath - Deserialize ────────────────────────────────────────

    [TestMethod]
    public void FilePath_Deserialize_StringValue_ReturnsFilePath()
    {
        // Serialize で正しくエスケープされた JSON を生成してからデシリアライズ
        var original = new FilePath(@"C:\Users\test\file.txt");
        var json = JsonSerializer.Serialize<FilePath?>(original, _options);
        var result = JsonSerializer.Deserialize<FilePath?>(json, _options);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.AsDestructive().FullName.EndsWith("file.txt", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FilePath_Deserialize_JsonNull_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<FilePath?>("null", _options);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FilePath_Deserialize_EmptyString_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<FilePath?>("\"\"", _options);
        Assert.IsNull(result);
    }

    // ── FilePath - RoundTrip ─────────────────────────────────────────

    [TestMethod]
    public void FilePath_RoundTrip_PreservesFullName()
    {
        var original = new FilePath(@"C:\Users\test\settings.json");
        var json = JsonSerializer.Serialize<FilePath?>(original, _options);
        var restored = JsonSerializer.Deserialize<FilePath?>(json, _options);

        Assert.IsNotNull(restored);
        Assert.AreEqual(original.AsDestructive().FullName, restored.AsDestructive().FullName);
    }

    [TestMethod]
    public void FilePath_RoundTrip_NullPreservesNull()
    {
        var json = JsonSerializer.Serialize<FilePath?>(null, _options);
        var restored = JsonSerializer.Deserialize<FilePath?>(json, _options);

        Assert.IsNull(restored);
    }

    // ── DirectoryPath - Serialize ─────────────────────────────────────

    [TestMethod]
    public void DirectoryPath_Serialize_WritesPathAsString()
    {
        var path = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(path, _options);

        Assert.StartsWith("\"", json, "Should be a JSON string, not an object");
        Assert.Contains("test", json, $"JSON should contain the directory name, got: {json}");
    }

    [TestMethod]
    public void DirectoryPath_Serialize_NullValue_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<DirectoryPath?>(null, _options);
        Assert.AreEqual("null", json);
    }

    // ── DirectoryPath - Deserialize ───────────────────────────────────

    [TestMethod]
    public void DirectoryPath_Deserialize_StringValue_ReturnsDirectoryPath()
    {
        var original = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(original, _options);
        var result = JsonSerializer.Deserialize<DirectoryPath?>(json, _options);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.AsDestructive().FullName.EndsWith("test", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_JsonNull_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<DirectoryPath?>("null", _options);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_EmptyString_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<DirectoryPath?>("\"\"", _options);
        Assert.IsNull(result);
    }

    // ── DirectoryPath - RoundTrip ─────────────────────────────────────

    [TestMethod]
    public void DirectoryPath_RoundTrip_PreservesFullName()
    {
        var original = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(original, _options);
        var restored = JsonSerializer.Deserialize<DirectoryPath?>(json, _options);

        Assert.IsNotNull(restored);
        Assert.AreEqual(original.AsDestructive().FullName, restored.AsDestructive().FullName);
    }

    [TestMethod]
    public void DirectoryPath_RoundTrip_NullPreservesNull()
    {
        var json = JsonSerializer.Serialize<DirectoryPath?>(null, _options);
        var restored = JsonSerializer.Deserialize<DirectoryPath?>(json, _options);

        Assert.IsNull(restored);
    }

    // ── 無効なパスのデシリアライズ ────────────────────────────────────

    [TestMethod]
    public void FilePath_Deserialize_InvalidPathChars_DoesNotThrow()
    {
        // Mio はコンストラクタ時点でパスの有効性を検証しないため、デシリアライズ自体は成功する
        var result = JsonSerializer.Deserialize<FilePath?>(@"""C:\\invalid?path\\file.txt""", _options);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_InvalidPathChars_DoesNotThrow()
    {
        // 同上
        var result = JsonSerializer.Deserialize<DirectoryPath?>(@"""C:\\invalid<dir>""", _options);
        Assert.IsNotNull(result);
    }
}
