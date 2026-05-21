using System;
using System.Windows.Markup;
using Amethystra.UI.Controls;

namespace Amethystra.UI.Markup;

/// <summary>
/// XAML から <see cref="EmojiIcon"/> を MarkupExtension 形式で生成するヘルパーです。
/// </summary>
/// <remarks>
/// <para>
/// Wpf.Ui の <c>{ui:SymbolIcon Foo20}</c> や <c>{ui:FontIcon ...}</c> と同じ流儀で、属性値に直接書ける形を提供します。
/// </para>
/// <example>
/// <code>
/// &lt;ui:CardExpander Icon="{metro:EmojiIcon 🎯}" /&gt;
/// &lt;ui:CardExpander Icon="{metro:EmojiIcon 🎯, Size=24}" /&gt;
/// </code>
/// </example>
/// </remarks>
[MarkupExtensionReturnType(typeof(EmojiIcon))]
public class EmojiIconExtension : MarkupExtension
{
    public EmojiIconExtension()
    {
    }

    public EmojiIconExtension(string emoji)
    {
        this.Emoji = emoji;
    }

    /// <summary>
    /// 表示する絵文字シーケンス。1 つの絵文字クラスタ (ZWJ シーケンス・国旗・キーキャップ等を含む) を指定します。
    /// </summary>
    [ConstructorArgument("emoji")]
    public string? Emoji { get; set; }

    /// <summary>
    /// アイコンの一辺の寸法 (DIP)。<see cref="double.NaN"/> または 0 以下の場合は <see cref="EmojiIcon"/> 既定値が使われます。
    /// </summary>
    public double Size { get; set; } = double.NaN;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var icon = new EmojiIcon
        {
            Emoji = this.Emoji,
        };

        if (double.IsNaN(this.Size) == false && this.Size > 0)
        {
            icon.Width = this.Size;
            icon.Height = this.Size;
        }

        return icon;
    }
}
