using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amethystra.Serialization;

/// <summary>
/// <see cref="IntPtr"/> / <see cref="UIntPtr"/> (および同等の nint / nuint) を 16 進文字列としてシリアライズするための
/// <see cref="JsonConverter"/> ファクトリです。
/// </summary>
/// <remarks>
/// 診断ログ用途で使用する想定の write-only 実装。
/// </remarks>
public sealed class NativeIntJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(IntPtr) || typeToConvert == typeof(UIntPtr);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => typeToConvert == typeof(IntPtr)
            ? new IntPtrConverter()
            : new UIntPtrConverter();

    private sealed class IntPtrConverter : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("This converter is for logging (write-only).");

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
            => writer.WriteStringValue($"0x{value.ToInt64():x}");
    }

    private sealed class UIntPtrConverter : JsonConverter<UIntPtr>
    {
        public override UIntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("This converter is for logging (write-only).");

        public override void Write(Utf8JsonWriter writer, UIntPtr value, JsonSerializerOptions options)
            => writer.WriteStringValue($"0x{value.ToUInt64():x}");
    }
}
