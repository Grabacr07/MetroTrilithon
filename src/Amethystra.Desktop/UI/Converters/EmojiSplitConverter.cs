using System;
using System.Globalization;
using System.Windows.Data;

namespace Amethystra.UI.Converters;

/// <summary>
/// "絵文字｜テキスト" または "絵文字|テキスト" 形式の文字列から、絵文字部分とテキスト部分を取り出すコンバーターです。
/// </summary>
/// <remarks>
/// <para>
/// 区切り文字は半角 <c>|</c> (U+007C) と全角 <c>｜</c> (U+FF5C) のどちらでも構いません。区切り文字の前後にある空白文字は取り除きます。
/// </para>
/// <para>
/// 入力に区切り文字が存在しない場合:
/// <list type="bullet">
/// <item><see cref="EmojiSplitPart.Emoji"/> 指定時は <see cref="DefaultEmoji"/> を返します。</item>
/// <item><see cref="EmojiSplitPart.Text"/> 指定時は入力文字列をそのまま返します。</item>
/// </list>
/// </para>
/// </remarks>
public class EmojiSplitConverter : IValueConverter
{
    private const string _defaultEmoji = "❓";

    private static readonly char[] _separators = ['|', '｜'];

    /// <summary>
    /// このコンバーターが返すパート (絵文字部 / テキスト部) の種別を取得または設定します。
    /// </summary>
    public EmojiSplitPart Part { get; set; } = EmojiSplitPart.Text;

    /// <summary>
    /// 入力に区切り文字が含まれていなかった場合に <see cref="EmojiSplitPart.Emoji"/> として返す絵文字を取得または設定します。
    /// </summary>
    public string DefaultEmoji { get; set; } = _defaultEmoji;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        var index = text.IndexOfAny(_separators);
        return index < 0
            ? this.Part == EmojiSplitPart.Emoji ? this.DefaultEmoji : text
            : this.Part == EmojiSplitPart.Emoji
                ? text[..index].Trim()
                : text[(index + 1)..].Trim();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// <see cref="EmojiSplitConverter"/> が返すパートの種別。
/// </summary>
public enum EmojiSplitPart
{
    /// <summary>
    /// テキスト部 (区切り文字より後ろ)。区切り文字がなければ入力そのもの。
    /// </summary>
    Text,

    /// <summary
    /// >絵文字部 (区切り文字より前)。区切り文字がなければ <see cref="EmojiSplitConverter.DefaultEmoji"/>。
    /// </summary>
    Emoji,
}
