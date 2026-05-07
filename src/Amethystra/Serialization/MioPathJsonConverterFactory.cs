using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amethystra.IO;
using Amethystra.IO.Destructive;

namespace Amethystra.Serialization;

public sealed class MioPathJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(FilePath) || typeToConvert == typeof(DirectoryPath);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(FilePath)) return new FilePathJsonConverter();
        if (typeToConvert == typeof(DirectoryPath)) return new DirectoryPathJsonConverter();

        throw new NotSupportedException($"Type {typeToConvert} is not supported.");
    }

    private sealed class FilePathJsonConverter : JsonConverter<FilePath>
    {
        public override FilePath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var path = reader.GetString();
            return string.IsNullOrEmpty(path) ? null : new FilePath(path);
        }

        public override void Write(Utf8JsonWriter writer, FilePath value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.AsDestructive().FullName);
    }

    private sealed class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>
    {
        public override DirectoryPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var path = reader.GetString();
            return string.IsNullOrEmpty(path) ? null : new DirectoryPath(path);
        }

        public override void Write(Utf8JsonWriter writer, DirectoryPath value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.AsDestructive().FullName);
    }
}
