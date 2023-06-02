using System.Windows;

namespace MetroTrilithon.UI.Markup;

public class BindingProxy : Freezable
{
    #region Data dependency property

    public object Data
    {
        get => this.GetValue(DataProperty);
        set => this.SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty
        = DependencyProperty.Register(
            nameof(Data),
            typeof(object),
            typeof(BindingProxy),
            new PropertyMetadata(0));

    #endregion

    protected override Freezable CreateInstanceCore()
        => new BindingProxy();
}
