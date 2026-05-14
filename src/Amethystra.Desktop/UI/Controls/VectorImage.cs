using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using R3;

namespace Amethystra.UI.Controls;

/// <summary>
/// Renders a vector <see cref="DrawingImage"/> as a bitmap whose aspect ratio always matches
/// the source. The bitmap's pixel resolution is rasterized once at the size required by the
/// current display and DPI, and re-rasterized via R3 debounce when the display grows past
/// the cached resolution. Stretch behaviour (None / Uniform / UniformToFill / Fill) is
/// applied at draw time, so resizing the element does not change the bitmap's content; only
/// the destination rect changes. As a result, debounced re-rasterization upgrades pixel
/// sharpness without producing any visible "shift" in the displayed image.
/// </summary>
public class VectorImage : FrameworkElement
{
    private static readonly TimeSpan _resolutionUpgradeDebounce = TimeSpan.FromMilliseconds(150);

    #region Source dependency property

    public static readonly DependencyProperty SourceProperty
        = DependencyProperty.Register(
            nameof(Source),
            typeof(DrawingImage),
            typeof(VectorImage),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                HandleSourceChanged));

    private static void HandleSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VectorImage instance) instance._cache = null;
    }

    public DrawingImage? Source
    {
        get => (DrawingImage?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    #endregion

    #region Stretch dependency property

    public static readonly DependencyProperty StretchProperty
        = DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(VectorImage),
            new FrameworkPropertyMetadata(
                Stretch.Uniform,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public Stretch Stretch
    {
        get => (Stretch)this.GetValue(StretchProperty);
        set => this.SetValue(StretchProperty, value);
    }

    #endregion

    private readonly Subject<Unit> _upgradeRequests = new();
    private IDisposable? _subscription;
    private BitmapSource? _cache;
    private int _cachePixelWidth;

    public VectorImage()
    {
        this.Loaded += this.HandleLoaded;
        this.Unloaded += this.HandleUnloaded;
    }

    protected override Size MeasureOverride(Size constraint)
        => this.ComputeStretchedSize(constraint);

    protected override Size ArrangeOverride(Size finalSize)
        => finalSize;

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (this.Source?.Drawing is not { } drawing) return;
        var size = this.RenderSize;
        if (size.Width <= 0 || size.Height <= 0) return;

        var bounds = drawing.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var aspect = bounds.Width / bounds.Height;
        var dpi = this.GetCurrentDpiScale();
        var requiredPixelWidth = ComputeRequiredPixelWidth(size, aspect, dpi);

        if (this._cache == null)
        {
            this._cache = Rasterize(drawing, bounds, requiredPixelWidth);
            this._cachePixelWidth = requiredPixelWidth;
        }
        else if (this._cachePixelWidth < requiredPixelWidth)
        {
            this._upgradeRequests.OnNext(default);
        }

        var drawRect = ComputeDrawRect(size, new Size(bounds.Width, bounds.Height), this.Stretch);
        var needsClip = this.Stretch == Stretch.UniformToFill
            || (this.Stretch == Stretch.None && (drawRect.Width > size.Width || drawRect.Height > size.Height));

        if (needsClip)
        {
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, size.Width, size.Height)));
        }

        drawingContext.DrawImage(this._cache, drawRect);
        if (needsClip)
        {
            drawingContext.Pop();
        }
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if (this._subscription != null) return;
        this._subscription = this._upgradeRequests
            .Debounce(_resolutionUpgradeDebounce)
            .Subscribe(_ => UIDispatcher.Instance.BeginInvoke(this.UpgradeResolution));
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        this._subscription?.Dispose();
        this._subscription = null;
    }

    private void UpgradeResolution()
    {
        if (this.Source?.Drawing is not { } drawing) return;
        var size = this.RenderSize;
        if (size.Width <= 0 || size.Height <= 0) return;

        var bounds = drawing.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var aspect = bounds.Width / bounds.Height;
        var dpi = this.GetCurrentDpiScale();
        var requiredPixelWidth = ComputeRequiredPixelWidth(size, aspect, dpi);

        if (this._cache != null && this._cachePixelWidth >= requiredPixelWidth) return;

        this._cache = Rasterize(drawing, bounds, requiredPixelWidth);
        this._cachePixelWidth = requiredPixelWidth;
        this.InvalidateVisual();
    }

    private double GetCurrentDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        var matrix = source?.CompositionTarget?.TransformToDevice;
        return matrix?.M11 ?? 1.0;
    }

    private Size ComputeStretchedSize(Size availableSize)
    {
        if (this.Source?.Drawing is not { } drawing) return new Size(0, 0);
        var bounds = drawing.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return new Size(0, 0);

        var sx = availableSize.Width / bounds.Width;
        var sy = availableSize.Height / bounds.Height;
        if (double.IsPositiveInfinity(availableSize.Width)) sx = sy;
        if (double.IsPositiveInfinity(availableSize.Height)) sy = sx;
        if (double.IsPositiveInfinity(sx)) sx = 1.0;
        if (double.IsPositiveInfinity(sy)) sy = 1.0;

        switch (this.Stretch)
        {
            case Stretch.None:
                sx = sy = 1.0;
                break;
            case Stretch.Uniform:
                sx = sy = Math.Min(sx, sy);
                break;
            case Stretch.UniformToFill:
                sx = sy = Math.Max(sx, sy);
                break;
        }

        var w = bounds.Width * sx;
        var h = bounds.Height * sy;
        return new Size(
            double.IsPositiveInfinity(availableSize.Width) ? w : Math.Min(w, availableSize.Width),
            double.IsPositiveInfinity(availableSize.Height) ? h : Math.Min(h, availableSize.Height));
    }

    /// <summary>
    /// Computes the pixel width the rasterized bitmap needs in order to render the display
    /// at native resolution for the most demanding Stretch case (UniformToFill).
    /// For a bitmap with the source's aspect, the bitmap's displayed pixel width under
    /// UniformToFill equals <c>max(W, H * aspect) * dpi</c>.
    /// </summary>
    private static int ComputeRequiredPixelWidth(Size displaySize, double aspect, double dpiScale)
    {
        var demand = Math.Max(displaySize.Width, displaySize.Height * aspect) * dpiScale;
        return Math.Max(1, (int)Math.Ceiling(demand));
    }

    /// <summary>
    /// Computes the destination rect for drawing the cached bitmap so the requested
    /// <see cref="Stretch"/> behaviour is applied at draw time rather than baked into the
    /// rasterization.
    /// </summary>
    private static Rect ComputeDrawRect(Size container, Size bitmapLogical, Stretch stretch)
    {
        var sx = container.Width / bitmapLogical.Width;
        var sy = container.Height / bitmapLogical.Height;
        switch (stretch)
        {
            case Stretch.None:
                sx = sy = 1.0;
                break;
            case Stretch.Uniform:
                sx = sy = Math.Min(sx, sy);
                break;
            case Stretch.UniformToFill:
                sx = sy = Math.Max(sx, sy);
                break;
        }

        var w = bitmapLogical.Width * sx;
        var h = bitmapLogical.Height * sy;
        var x = (container.Width - w) / 2;
        var y = (container.Height - h) / 2;
        return new Rect(x, y, w, h);
    }

    private static BitmapSource Rasterize(Drawing drawing, Rect bounds, int pixelWidth)
    {
        var aspect = bounds.Width / bounds.Height;
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(pixelWidth / aspect));
        var scale = pixelWidth / bounds.Width;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
            dc.DrawDrawing(drawing);
            dc.Pop();
            dc.Pop();
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
