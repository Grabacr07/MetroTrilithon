using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace Amethystra.UI.Controls;

/// <summary>
/// Wraps a <c>ui:CardExpander</c> (or any other element) with a full-bleed image backdrop
/// and arbitrarily many blur overlays. Common responsibilities for the
/// "CardExpander on top of an image" pattern are absorbed here:
/// rounded clipping, image visibility gating via <see cref="IsActive"/>, opacity dimming via
/// <see cref="IsDimmed"/>, and an implicit style that makes the inner CardExpander's
/// background / border transparent while expanded.
/// </summary>
/// <remarks>
/// Application code declares the image <see cref="Source"/>, an <see cref="IsActive"/>
/// binding (typically to the inner CardExpander's <c>IsExpanded</c>), an optional
/// <see cref="IsDimmed"/> binding, and a collection of <see cref="BlurOverlay"/> entries
/// whose <see cref="BlurOverlay.Mask"/> brush positions each blur band. The blur band shape
/// is content-layout-dependent and intentionally remains the caller's responsibility.
/// </remarks>
[ContentProperty(nameof(Content))]
[TemplatePart(Name = PART_Content, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_ImageLayers, Type = typeof(FrameworkElement))]
public class CardImageBackdrop : ContentControl
{
    private const string PART_Content = nameof(PART_Content);
    private const string PART_ImageLayers = nameof(PART_ImageLayers);

    private FrameworkElement? _contentPart;
    private FrameworkElement? _imageLayersPart;

    static CardImageBackdrop()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(typeof(CardImageBackdrop)));
    }

    #region Source dependency property

    public static readonly DependencyProperty SourceProperty
        = DependencyProperty.Register(
            nameof(Source),
            typeof(DrawingImage),
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public DrawingImage? Source
    {
        get => (DrawingImage?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    #endregion

    #region IsActive dependency property

    public static readonly DependencyProperty IsActiveProperty
        = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsActive
    {
        get => (bool)this.GetValue(IsActiveProperty);
        set => this.SetValue(IsActiveProperty, value);
    }

    #endregion

    #region IsDimmed dependency property

    public static readonly DependencyProperty IsDimmedProperty
        = DependencyProperty.Register(
            nameof(IsDimmed),
            typeof(bool),
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsDimmed
    {
        get => (bool)this.GetValue(IsDimmedProperty);
        set => this.SetValue(IsDimmedProperty, value);
    }

    #endregion

    #region DimmedOpacity dependency property

    public static readonly DependencyProperty DimmedOpacityProperty
        = DependencyProperty.Register(
            nameof(DimmedOpacity),
            typeof(double),
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(0.3, FrameworkPropertyMetadataOptions.AffectsRender));

    public double DimmedOpacity
    {
        get => (double)this.GetValue(DimmedOpacityProperty);
        set => this.SetValue(DimmedOpacityProperty, value);
    }

    #endregion

    #region CornerRadius dependency property

    public static readonly DependencyProperty CornerRadiusProperty
        = DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(double),
            typeof(CardImageBackdrop),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CornerRadius
    {
        get => (double)this.GetValue(CornerRadiusProperty);
        set => this.SetValue(CornerRadiusProperty, value);
    }

    #endregion

    public Collection<BlurOverlay> Overlays { get; } = [];

    public override void OnApplyTemplate()
    {
        if (this._contentPart != null)
        {
            this._contentPart.SizeChanged -= this.HandleContentSizeChanged;
        }

        base.OnApplyTemplate();

        this._contentPart = this.GetTemplateChild(PART_Content) as FrameworkElement;
        this._imageLayersPart = this.GetTemplateChild(PART_ImageLayers) as FrameworkElement;

        if (this._contentPart != null)
        {
            this._contentPart.SizeChanged += this.HandleContentSizeChanged;
            this.SyncImageLayersSize();
        }
    }

    private void HandleContentSizeChanged(object sender, SizeChangedEventArgs e)
        => this.SyncImageLayersSize();

    private void SyncImageLayersSize()
    {
        if (this._contentPart == null || this._imageLayersPart == null) return;
        this._imageLayersPart.Width = this._contentPart.ActualWidth;
        this._imageLayersPart.Height = this._contentPart.ActualHeight;
    }
}
