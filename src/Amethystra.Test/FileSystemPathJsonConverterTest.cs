using System.Text.Json;
using Amethystra.IO;
using Amethystra.IO.Destructive;

namespace Amethystra.Test;

[TestClass]
public sealed class FileSystemPathJsonConverterTest
{
    // ── FilePath - Serialize ─────────────────────────────────────────

    [TestMethod]
    public void FilePath_Serialize_WritesPathAsString()
    {
        var path = new FilePath(@"C:\Users\test\file.txt");
        var json = JsonSerializer.Serialize<FilePath?>(path);

        Assert.StartsWith("\"", json, "Should be a JSON string, not an object");
        Assert.Contains("file.txt", json, $"JSON should contain the filename, got: {json}");
    }

    [TestMethod]
    public void FilePath_Serialize_NullValue_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<FilePath?>(null);
        Assert.AreEqual("null", json);
    }

    // ── FilePath - Deserialize ────────────────────────────────────────

    [TestMethod]
    public void FilePath_Deserialize_StringValue_ReturnsFilePath()
    {
        var original = new FilePath(@"C:\Users\test\file.txt");
        var json = JsonSerializer.Serialize<FilePath?>(original);
        var result = JsonSerializer.Deserialize<FilePath?>(json);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.FullName.EndsWith("file.txt", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FilePath_Deserialize_JsonNull_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<FilePath?>("null");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FilePath_Deserialize_EmptyString_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<FilePath?>("\"\"");
        Assert.IsNull(result);
    }

    // ── FilePath - RoundTrip ─────────────────────────────────────────

    [TestMethod]
    public void FilePath_RoundTrip_PreservesFullName()
    {
        var original = new FilePath(@"C:\Users\test\settings.json");
        var json = JsonSerializer.Serialize<FilePath?>(original);
        var restored = JsonSerializer.Deserialize<FilePath?>(json);

        Assert.IsNotNull(restored);
        Assert.AreEqual(original.FullName, restored.FullName);
    }

    [TestMethod]
    public void FilePath_RoundTrip_NullPreservesNull()
    {
        var json = JsonSerializer.Serialize<FilePath?>(null);
        var restored = JsonSerializer.Deserialize<FilePath?>(json);

        Assert.IsNull(restored);
    }

    // ── DirectoryPath - Serialize ─────────────────────────────────────

    [TestMethod]
    public void DirectoryPath_Serialize_WritesPathAsString()
    {
        var path = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(path);

        Assert.StartsWith("\"", json, "Should be a JSON string, not an object");
        Assert.Contains("test", json, $"JSON should contain the directory name, got: {json}");
    }

    [TestMethod]
    public void DirectoryPath_Serialize_NullValue_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<DirectoryPath?>(null);
        Assert.AreEqual("null", json);
    }

    // ── DirectoryPath - Deserialize ───────────────────────────────────

    [TestMethod]
    public void DirectoryPath_Deserialize_StringValue_ReturnsDirectoryPath()
    {
        var original = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(original);
        var result = JsonSerializer.Deserialize<DirectoryPath?>(json);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.FullName.EndsWith("test", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_JsonNull_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<DirectoryPath?>("null");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_EmptyString_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<DirectoryPath?>("\"\"");
        Assert.IsNull(result);
    }

    // ── DirectoryPath - RoundTrip ─────────────────────────────────────

    [TestMethod]
    public void DirectoryPath_RoundTrip_PreservesFullName()
    {
        var original = new DirectoryPath(@"C:\Users\test");
        var json = JsonSerializer.Serialize<DirectoryPath?>(original);
        var restored = JsonSerializer.Deserialize<DirectoryPath?>(json);

        Assert.IsNotNull(restored);
        Assert.AreEqual(original.FullName, restored.FullName);
    }

    [TestMethod]
    public void DirectoryPath_RoundTrip_NullPreservesNull()
    {
        var json = JsonSerializer.Serialize<DirectoryPath?>(null);
        var restored = JsonSerializer.Deserialize<DirectoryPath?>(json);

        Assert.IsNull(restored);
    }

    // ── 無効なパスのデシリアライズ ────────────────────────────────────

    [TestMethod]
    public void FilePath_Deserialize_InvalidPathChars_DoesNotThrow()
    {
        var result = JsonSerializer.Deserialize<FilePath?>(@"""C:\\invalid?path\\file.txt""");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DirectoryPath_Deserialize_InvalidPathChars_DoesNotThrow()
    {
        var result = JsonSerializer.Deserialize<DirectoryPath?>(@"""C:\\invalid<dir>""");
        Assert.IsNotNull(result);
    }
}
