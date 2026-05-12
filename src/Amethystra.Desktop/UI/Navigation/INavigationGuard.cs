using System.Threading.Tasks;

namespace Amethystra.UI.Navigation;

/// <summary>
/// ナビゲーション スタックから自身が取り除かれてよいかを判定する機能を提供します。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NavigationHost.GoBackAsync"/> / <see cref="NavigationHost.GoToDepthAsync"/> はスタックの末尾の ViewModel が
/// このインターフェイスを実装している場合、pop の前に <see cref="CanLeaveAsync"/> を await して許可を確認します。
/// </para>
/// <para>
/// 未保存の編集内容がある画面で確認 ダイアログを挟みたい用途を想定しています。
/// </para>
/// </remarks>
public interface INavigationGuard
{
    /// <summary>
    /// この ViewModel をスタックから取り除いてよいかを非同期に判定します。
    /// </summary>
    /// <returns>取り除きを許可する場合は <see langword="true"/>、阻止する場合は <see langword="false"/>。</returns>
    ValueTask<bool> CanLeaveAsync();
}
