using System.Collections.Generic;
using System.Text;

namespace Amethystra.UI.Text;

/// <summary>
/// プレーンテキストと絵文字シーケンスを区別するための簡易セグメンターです。
/// </summary>
/// <remarks>
/// UTS #51 の完全な実装ではなく、UI 上でユーザーが入力する一般的な絵文字を正しく 1 セグメントに束ねることを目的としています。
/// ZWJ シーケンス、VS-16、肌色トーン修飾子、地域インジケーター (国旗) のペア、結合キーキャップまでをサポートします。
/// IME 変換中のテキストなどに現れる対にならないサロゲートは、例外とせず通常テキストとして扱います。
/// </remarks>
public static class EmojiSegmenter
{
    /// <summary>
    /// セグメントの種類。
    /// </summary>
    public enum SegmentKind
    {
        /// <summary>絵文字以外の通常テキスト。</summary>
        Text,

        /// <summary>1 つの絵文字を構成する文字シーケンス。</summary>
        Emoji,
    }

    /// <summary>
    /// セグメント 1 件分のデータ。
    /// </summary>
    public readonly record struct Segment(string Text, SegmentKind Kind);

    /// <summary>
    /// 入力文字列をテキストセグメントと絵文字セグメントに分割します。
    /// </summary>
    public static IReadOnlyList<Segment> Split(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var result = new List<Segment>();
        var i = 0;
        var textStart = 0;

        while (i < text.Length)
        {
            // 対にならないサロゲートはスカラー値を構成しないため、1 コード単位を通常テキストとして読み進める。
            if (Rune.TryGetRuneAt(text, i, out var rune) == false)
            {
                i++;
                continue;
            }

            var consumed = rune.Utf16SequenceLength;

            if (IsEmojiClusterStart(text, i, rune) == false)
            {
                i += consumed;
                continue;
            }

            // 直前までに蓄積されたテキストをフラッシュ。
            if (i > textStart)
            {
                result.Add(new Segment(text[textStart..i], SegmentKind.Text));
            }

            var emojiStart = i;
            var firstRune = rune;
            i += consumed;

            // 修飾子・結合子・後続絵文字を貪欲に取り込む。
            while (i < text.Length)
            {
                // 対にならないサロゲートに達したらクラスタを打ち切る。
                if (Rune.TryGetRuneAt(text, i, out var next) == false) break;

                var nextLen = next.Utf16SequenceLength;

                switch (next.Value)
                {
                    // 異体字セレクタ (VS-16 / VS-15) は同一クラスタ。
                    case 0xFE0F or 0xFE0E:
                    // 結合キーキャップ (1️⃣ などの末尾)。
                    case 0x20E3:
                        i += nextLen;
                        continue;
                }

                // 肌色トーン修飾子。
                if (IsSkinToneModifier(next))
                {
                    i += nextLen;
                    continue;
                }

                // ZWJ の直後が絵文字であれば結合し続ける。
                if (next.Value == 0x200D)
                {
                    var after = i + nextLen;
                    if (after >= text.Length) break;

                    if (Rune.TryGetRuneAt(text, after, out var afterRune) == false) break;
                    if (IsEmojiBase(afterRune) == false) break;

                    i = after + afterRune.Utf16SequenceLength;
                    continue;
                }

                // 地域インジケーターのペア (国旗): 開始が地域インジケーターで、その直後にもう 1 つだけ続く。
                if (IsRegionalIndicator(firstRune)
                    && IsRegionalIndicator(next)
                    && i == emojiStart + firstRune.Utf16SequenceLength)
                {
                    i += nextLen;
                }

                break;
            }

            result.Add(new Segment(text[emojiStart..i], SegmentKind.Emoji));
            textStart = i;
        }

        if (i > textStart)
        {
            result.Add(new Segment(text[textStart..i], SegmentKind.Text));
        }

        return result;
    }

    /// <summary>
    /// 指定位置のルーンが絵文字クラスタの起点になり得るかを判定します。
    /// </summary>
    /// <remarks>
    /// 標準的な絵文字コードポイントに加えて、直後に VS-16 (U+FE0F) または結合キーキャップ (U+20E3) を
    /// 伴うルーンも起点と見なします。これにより、デジタル数字や <c>#</c>, <c>*</c>、©, ®, ™ などの
    /// 「VS-16 付きで絵文字表示になる」コードポイントを取りこぼさずに済みます。
    /// </remarks>
    private static bool IsEmojiClusterStart(string text, int index, Rune rune)
    {
        if (IsEmojiBase(rune)) return true;

        var nextIndex = index + rune.Utf16SequenceLength;
        if (nextIndex >= text.Length) return false;

        if (Rune.TryGetRuneAt(text, nextIndex, out var next) == false) return false;
        return next.Value is 0xFE0F or 0x20E3;
    }

    /// <summary>
    /// デフォルトで絵文字表示されるコードポイント範囲に属しているかを判定します。
    /// </summary>
    private static bool IsEmojiBase(Rune rune)
        => rune.Value switch
        {
            // 主要な絵文字平面 (Emoticons / Misc Pictographs / Transport / Symbols Extended A 等)。
            >= 0x1F000 and <= 0x1FAFF => true,
            // Miscellaneous Symbols + Dingbats (☀ ⭐ ✨ など)。
            >= 0x2600 and <= 0x27BF => true,
            // Geometric Shapes (▶ ◀ など、絵文字としても扱われるもの)。
            >= 0x25A0 and <= 0x25FF => true,
            // Miscellaneous Symbols and Arrows。
            >= 0x2B00 and <= 0x2BFF => true,
            // Miscellaneous Technical (⌛ ⌚ など)。
            >= 0x2300 and <= 0x23FF => true,

            _ => false,
        };

    private static bool IsRegionalIndicator(Rune rune)
        => rune.Value is >= 0x1F1E6 and <= 0x1F1FF;

    private static bool IsSkinToneModifier(Rune rune)
        => rune.Value is >= 0x1F3FB and <= 0x1F3FF;
}
