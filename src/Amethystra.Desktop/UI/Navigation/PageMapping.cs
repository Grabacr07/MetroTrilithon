using System;

namespace Amethystra.UI.Navigation;

/// <summary>
/// ViewModel の型と対応する Page の型のマッピングを表します。
/// </summary>
/// <remarks>
/// <see cref="FrameNavigationBehavior.Mappings"/> の要素として使用します。
/// </remarks>
public sealed class PageMapping
{
    /// <summary>
    /// ナビゲーションのキーとなる ViewModel の型を取得または設定します。
    /// </summary>
    public Type? ViewModel { get; set; }

    /// <summary>
    /// 対応する Page の型を取得または設定します。
    /// </summary>
    public Type? Page { get; set; }
}
