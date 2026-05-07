using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amethystra.IO;

public sealed class FilePathJsonConverter : JsonConverter<FilePath>
{
    public override FilePath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var path = reader.GetString();
        return string.IsNullOrEmpty(path) ? null : new FilePath(path);
    }

    public override void Write(Utf8JsonWriter writer, FilePath value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.FullName);
}

public sealed class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>
{
    public override DirectoryPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var path = reader.GetString();
        return string.IsNullOrEmpty(path) ? null : new DirectoryPath(path);
    }

    public override void Write(Utf8JsonWriter writer, DirectoryPath value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.FullName);
}
