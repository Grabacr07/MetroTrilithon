using System;
using System.Globalization;
using System.Windows.Data;
using Amethystra.UI.Text;

namespace Amethystra.UI.Converters;

/// <summary>
/// "絵文字｜テキスト" または "絵文字|テキスト" 形式の文字列から、絵文字部分とテキスト部分を取り出すコンバーターです。
/// </summary>
/// <remarks>
/// <para>
/// 区切り文字は半角 <c>|</c> (U+007C) と全角 <c>｜</c> (U+FF5C) のどちらでも構いません。区切り文字の前後にある空白文字は取り除きます。
/// </para>
/// <para>
/// 分割は「区切り文字の左側が絵文字クラスタのみで構成されている」場合に限って行います。左側に通常の文字が混じっている、あるいは入力に区切り文字が存在しない場合は分割せず、以下のように振る舞います:
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

        // 区切り文字が無い、または左側が「絵文字のみ」のクラスタで構成されていない場合は分割しない。
        // 「左側に文字が混じっている」「左側に絵文字が含まれていない」ケースを、誤って icon に流さないための判定。
        if (index < 0 || IsEmojiOnly(text[..index]) == false)
        {
            return this.Part == EmojiSplitPart.Emoji ? this.DefaultEmoji : text;
        }

        return this.Part == EmojiSplitPart.Emoji
            ? text[..index].Trim()
            : text[(index + 1)..].Trim();
    }

    /// <summary>
    /// 入力文字列 (前後の空白を除いたもの) が、<see cref="EmojiSegmenter"/> から見てすべて絵文字セグメントで構成されているかを判定します。
    /// </summary>
    private static bool IsEmojiOnly(string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;

        var segments = EmojiSegmenter.Split(trimmed);
        return segments.Count != 0 && segments.All(segment => segment.Kind == EmojiSegmenter.SegmentKind.Emoji);
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
