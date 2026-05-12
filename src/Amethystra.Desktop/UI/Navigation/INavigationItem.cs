namespace Amethystra.UI.Navigation;

/// <summary>
/// ナビゲーション アイテムが自身の見出し表記を提供します。
/// </summary>
/// <remarks>
/// ナビゲーションのスタックに積まれる ViewModel が、自身の見出し表記を申告するためのインターフェイスです。
/// パンくずリスト等で、各 ViewModel の表示名として参照されます。
/// </remarks>
public interface INavigationItem
{
    /// <summary>
    /// パンくずリスト等に表示される見出しテキストを取得します。
    /// </summary>
    string Heading { get; }
}
