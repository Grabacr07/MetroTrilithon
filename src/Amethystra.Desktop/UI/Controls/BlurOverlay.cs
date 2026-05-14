using System.Windows;
using System.Windows.Media;

namespace Amethystra.UI.Controls;

/// <summary>
/// Describes a single blur layer inside <see cref="CardImageBackdrop"/>. The layer renders the
/// backdrop's <see cref="CardImageBackdrop.Source"/> through a <see cref="System.Windows.Media.Effects.BlurEffect"/>
/// of the given <see cref="Radius"/>, masked by <see cref="Mask"/>. Multiple instances stack to
/// produce arbitrary blur shapes (header band, side fade, etc.).
/// </summary>
public class BlurOverlay : Freezable
{
    #region Radius dependency property

    public static readonly DependencyProperty RadiusProperty
        = DependencyProperty.Register(
            nameof(Radius),
            typeof(double),
            typeof(BlurOverlay),
            new PropertyMetadata(20.0));

    public double Radius
    {
        get => (double)this.GetValue(RadiusProperty);
        set => this.SetValue(RadiusProperty, value);
    }

    #endregion

    #region Mask dependency property

    public static readonly DependencyProperty MaskProperty
        = DependencyProperty.Register(
            nameof(Mask),
            typeof(Brush),
            typeof(BlurOverlay),
            new PropertyMetadata(null));

    public Brush? Mask
    {
        get => (Brush?)this.GetValue(MaskProperty);
        set => this.SetValue(MaskProperty, value);
    }

    #endregion

    protected override Freezable CreateInstanceCore()
        => new BlurOverlay();
}
