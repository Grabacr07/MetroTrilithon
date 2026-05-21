using System;
using System.Windows;
using System.Windows.Threading;
using Amethystra.UI.Text;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Controls;

/// <summary>
/// Wpf.Ui の <see cref="ImageIcon"/> を継承し、Unicode のカラー絵文字を表示するアイコン コントロールです。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Emoji"/> プロパティに 1 つの絵文字クラスタ (ZWJ シーケンス・国旗・キーキャップを含む) を指定すると、
/// <see cref="EmojiBitmapCache"/> 経由で DirectWrite + Direct2D により COLR/CPAL カラーグリフをラスタライズし、結果の
/// <see cref="System.Windows.Media.Imaging.BitmapSource"/> を <see cref="ImageIcon.Source"/> にセットします。
/// </para>
/// <para>
/// ラスタライズ対象のサイズは Loaded 後の <see cref="FrameworkElement.ActualHeight"/> から決定し、以降は
/// <see cref="Window.DpiChanged"/> でのみ再生成します。<see cref="FrameworkElement.SizeChanged"/> を購読すると、
/// <see cref="ImageIcon.Source"/> の差し替えで誘発される自身の <c>DesiredSize</c> 変動が再ラスタライズへフィードバックし、WIC の
/// <see cref="System.UInt32"/> 範囲を超えるピクセルサイズ要求まで膨張する暴走が起きるため購読しません。表示サイズへの追従は
/// <see cref="ImageIcon"/> 既定の <see cref="System.Windows.Media.Stretch.Uniform"/> によるスケーリングに委ねます。
/// </para>
/// <para>
/// <see cref="EmojiBitmapCache"/> が (text, emSize, pixelsPerDip) をキーに同一インスタンスを返すため、同一サイズへの再要求はキャッシュヒットで完結します。
/// </para>
/// </remarks>
public class EmojiIcon : ImageIcon
{
    private const double _defaultEmSize = 20.0;
    private const double _emSizeThreshold = 0.5;
    private const double _dpiThreshold = 0.01;

    #region Emoji dependency property

    public static readonly DependencyProperty EmojiProperty
        = DependencyProperty.Register(
            nameof(Emoji),
            typeof(string),
            typeof(EmojiIcon),
            new PropertyMetadata(null, HandleEmojiChanged));

    public string? Emoji
    {
        get => (string?)this.GetValue(EmojiProperty);
        set => this.SetValue(EmojiProperty, value);
    }

    private static void HandleEmojiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EmojiIcon icon) icon.RebuildSource();
    }

    #endregion

    private string? _lastRenderedCluster;
    private double _lastRenderedEmSize;
    private double _lastRenderedPixelsPerDip;
    private DpiChangedEventHandler? _dpiHandler;
    private Window? _dpiHandlerOwner;

    public EmojiIcon()
    {
        // ImageIcon は SymbolIcon と異なり FontSize でサイズが決まらず、Width / Height で寸法を決める設計。
        // CardExpander などの Icon コンテナは ImageIcon に対して Stretch を許す形で領域を渡すため、Width / Height が未指定 (NaN) のままだと
        // BitmapSource がコンテナいっぱいに拡大されてしまう。Wpf.Ui の SymbolIcon の慣例サイズ (20) を既定として与え、XAML から明示指定があればそちらを尊重する。
        // SetValue (= 直接代入) で設定すると LocalValue として記録され、XAML の Width="..." 指定と同じ優先度になるため、ItemsControl 仮想化や
        // Wpf.Ui のテーマ Style と相互作用したときに後勝ちが逆転して既定値が残るケースがある。SetCurrentValue であれば LocalValue を占有しないため、
        // XAML で明示した値 (LocalValue) が常に優先される。
        this.SetCurrentValue(WidthProperty, _defaultEmSize);
        this.SetCurrentValue(HeightProperty, _defaultEmSize);

        this.Loaded += this.HandleLoaded;
        this.Unloaded += this.HandleUnloaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        this.AttachDpiHandler();

        // Loaded 時点ではまだ Measure / Arrange が完了しておらず ActualHeight が 0 のことがあるため、
        // レイアウト確定後 (DispatcherPriority.Loaded) に RebuildSource を実行して、実枠サイズを反映させる。
        this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(this.RebuildSource));
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        this.DetachDpiHandler();
    }

    private void AttachDpiHandler()
    {
        this.DetachDpiHandler();

        var window = Window.GetWindow(this);
        if (window is null) return;

        this._dpiHandler = (_, _) => this.RebuildSource();
        this._dpiHandlerOwner = window;
        window.DpiChanged += this._dpiHandler;
    }

    private void DetachDpiHandler()
    {
        if (this._dpiHandler is null) return;

        this._dpiHandlerOwner?.DpiChanged -= this._dpiHandler;
        this._dpiHandler = null;
        this._dpiHandlerOwner = null;
    }

    private void RebuildSource()
    {
        var emoji = this.Emoji;
        if (string.IsNullOrEmpty(emoji))
        {
            this.Source = null;
            this._lastRenderedCluster = null;
            return;
        }

        // 入力に複数絵文字が含まれていても 1 アイコンとしては最初のクラスタのみを採用する。
        // ただし最初のセグメントが Text の場合 (絵文字ではない普通の文字) も、そのまま EmojiBitmapCache に渡す。
        var segments = EmojiSegmenter.Split(emoji);
        var cluster = segments.Count > 0 ? segments[0].Text : emoji;

        var emSize = this.ComputeEmSize();
        var pixelsPerDip = this.GetPixelsPerDip();

        if (string.Equals(cluster, this._lastRenderedCluster, StringComparison.Ordinal)
            && Math.Abs(emSize - this._lastRenderedEmSize) < _emSizeThreshold
            && Math.Abs(pixelsPerDip - this._lastRenderedPixelsPerDip) < _dpiThreshold)
        {
            return;
        }

        var bitmap = EmojiBitmapCache.Default.GetOrCreate(cluster, emSize, pixelsPerDip);
        if (bitmap is null) return;

        this.Source = bitmap;
        this._lastRenderedCluster = cluster;
        this._lastRenderedEmSize = emSize;
        this._lastRenderedPixelsPerDip = pixelsPerDip;
    }

    private double ComputeEmSize()
    {
        // 実測サイズ → 明示指定の Height / Width → 既定値の順にフォールバック。
        // 実測前 (Loaded 直後の初回 RebuildSource) は ActualHeight が 0 のため、Height / Width の明示指定を拾い、最終的に _defaultEmSize へ落ちる。
        if (this.ActualHeight > 0) return this.ActualHeight;
        if (double.IsNaN(this.Height) == false && this.Height > 0) return this.Height;
        if (double.IsNaN(this.Width) == false && this.Width > 0) return this.Width;

        return _defaultEmSize;
    }

    private double GetPixelsPerDip()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null) return 1.0;

        var scale = source.CompositionTarget.TransformToDevice.M11;
        return scale > 0 ? scale : 1.0;
    }
}
