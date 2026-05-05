using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Amethystra.UI.Markup;

public class BindingProxy : Freezable
{
    #region Data dependency property

    public static readonly DependencyProperty DataProperty
        = DependencyProperty.Register(
            nameof(Data),
            typeof(object),
            typeof(BindingProxy),
            new PropertyMetadata(0));

    public object Data
    {
        get => this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    #endregion

    protected override Freezable CreateInstanceCore()
        => new BindingProxy();
}

/// <summary>
/// ビジュアル ツリーから切り離された WPF 要素へ DataContext を橋渡しするための、型付き <see cref="Freezable"/> プロキシを表します。
/// </summary>
/// <typeparam name="T">プロキシが保持するデータの型。クラスである必要があります。</typeparam>
/// <remarks>
/// <para>
/// <see cref="ContextMenu"/> や <see cref="ToolTip"/> は WPF のビジュアルツリーとは独立したポップアップウィンドウとして表示されるため、親要素の DataContext を素直には継承できません。
/// <see cref="Freezable"/> は配置先の inheritance context を受け取れるという WPF の特性を利用し、このクラスをリソースディクショナリに配置することで、DataContext を <see cref="Data"/> プロパティ経由で参照できます。
/// ビジュアルツリー外の要素からは <c>StaticResource</c> 参照を通じてバインドソースとして利用します。
/// </para>
/// <para>
/// WPF XAML では <c>x:TypeArguments</c> 属性をルート要素以外に指定できないため、このクラスを XAML から直接インスタンス化する際に型引数を与えることができません。
/// XAML で使用する場合は、このクラスを継承した具体型のサブクラスを定義してください。
/// </para>
/// </remarks>
/// <example>
/// 次の例は、このクラスを継承したサブクラスを定義し、<see cref="ContextMenu"/> の <see cref="ItemsControl.ItemContainerStyle"/> 内でコマンドをバインドする方法を示します。
/// <code>
/// // C# でサブクラスを定義する
/// internal sealed class FooViewModelProxy : BindingProxy&lt;FooViewModel&gt;;
/// </code>
/// <code lang="XAML">
/// &lt;!-- ウィンドウのリソースとして宣言し、ContextMenu 内から StaticResource で参照する --&gt;
/// &lt;Window.Resources&gt;
///     &lt;binds:FooViewModelProxy x:Key="ViewModel" Data="{Binding}" /&gt;
/// &lt;/Window.Resources&gt;
///
/// &lt;ContextMenu ItemsSource="{Binding Data.Items, Source={StaticResource ViewModel}, FallbackValue={x:Null}}"&gt;
///     &lt;ContextMenu.ItemContainerStyle&gt;
///         &lt;Style TargetType="MenuItem"&gt;
///             &lt;Setter Property="Command"
///                     Value="{Binding Data.SomeCommand,
///                                     Source={StaticResource ViewModel},
///                                     FallbackValue={x:Null}}" /&gt;
///             &lt;Setter Property="CommandParameter" Value="{Binding}" /&gt;
///         &lt;/Style&gt;
///     &lt;/ContextMenu.ItemContainerStyle&gt;
/// &lt;/ContextMenu&gt;
/// </code>
/// </example>
public class BindingProxy<T> : Freezable
    where T : class
{
    #region Data dependency property

    public static readonly DependencyProperty DataProperty
        = DependencyProperty.Register(
            nameof(Data),
            typeof(T),
            typeof(BindingProxy<T>));

    public T? Data
    {
        get => (T?)this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    #endregion

    protected override Freezable CreateInstanceCore()
        => new BindingProxy<T>();
}
