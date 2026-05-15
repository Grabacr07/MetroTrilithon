using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Amethystra.Diagnostics;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// WPF 標準の <see cref="Window.SizeToContent"/> 相当の機能を提供する <see cref="Behavior{T}"/> 実装です。
/// </summary>
/// <remarks>
/// <para>
/// 標準の <see cref="Window.SizeToContent"/> は <c>WindowChrome</c> (および WPF-UI の <c>FluentWindow</c> 等それを内部利用するもの)
/// と組み合わせたとき、タイトルバー領域のドラッグでウィンドウ移動が効かなくなる既知の不具合があります。具体的には、NC ヒットテストで <c>HTCAPTION</c>
/// を返す経路が <see cref="Window.SizeToContent"/> 動作中のレイアウト ループと噛み合わず、ドラッグが抑制されます。本 Behavior は
/// <see cref="Window.SizeToContent"/> を使わずに、コンテンツの高さに合わせて <see cref="Window.Height"/> を能動的に追従させることでこの問題を回避します。
/// </para>
/// <para>
/// 高さ計算式:
/// <code>
///     contentDesired = ScrollViewer.ExtentHeight + (ScrollViewer の Window 内 Y 座標)
///                    または fallback として Window.Content.DesiredSize.Height
///     newHeight      = SnapToPixel( min(contentDesired + ChromeHeight, workArea - EdgeMargin) )
/// </code>
/// </para>
/// <para>
/// レイアウトに関する前提:<br/>
/// 本 Behavior は、Window のコンテンツ ルート要素 (通常は <c>Grid</c>) が <see cref="VerticalAlignment.Top"/> で配置されていることを前提にしています。
/// <c>Stretch</c> 配置のままだとルート要素が常に Window の content area を埋めるため、コンテンツ縮小時に
/// <see cref="UIElement.LayoutUpdated"/> 経由で「縮める要求」を観測できません。テンプレート側でルートを Top 揃えにしてください。
/// </para>
/// <para>
/// クロームの扱い:<br/>
/// <see cref="ChromeHeight"/> プロパティに「NC 領域 + シャドウ等、コンテンツ高さに加算すべき余白の合計 (DIP)」を指定します。
/// <c>WindowChrome</c> + <c>ExtendsContentIntoTitleBar="True"</c> のようにクライアント領域が HWND 全体に拡張されている構成では既定の
/// 0 で十分です。標準の Window (タイトルバーや非クライアント領域が残る構成) で使う場合は、計測した値を指定してください。
/// </para>
/// <para>
/// 再計算トリガー:
/// <list type="bullet">
///     <item><see cref="UIElement.LayoutUpdated"/>: 内部レイアウトの任意の変化 (Expander 開閉、アスペクト比可変な要素のサイズ変動など) を網羅的に拾うため。</item>
///     <item><see cref="Window.LocationChanged"/>: モニターを跨いだ移動時のみ作業領域を再評価。</item>
///     <item><see cref="Window.DpiChanged"/>: DPI 変化時に DIP 換算を更新。</item>
///     <item><see cref="Window.StateChanged"/>: Maximized / Minimized → Normal 復帰時に再フィット。</item>
/// </list>
/// </para>
/// <para>
/// 変化検知ガード:
/// 連続発火する <see cref="UIElement.LayoutUpdated"/> のフィードバック ループ防止のため、「適用予定の <c>newHeight</c> が現在の
/// <see cref="Window.Height"/> と一致していたらスキップ」の判定で抑制しています。<c>contentDesired</c> 基準ではなく
/// <c>Window.Height</c> 基準にしているのは、外部 (たとえばウィンドウ位置・サイズ復元処理) で <see cref="Window.Height"/> が
/// 書き換えられた場合も、その差分をトリガーに再適用できるようにするためです。
/// </para>
/// <para>
/// ピクセル境界スナップ:
/// 計算結果の DIP 値をそのまま <see cref="Window.Height"/> に書き込むと、フラクショナル値によるレイアウト丸めの揺らぎで内部要素が
/// ±1 px シフトすることがあります。これを避けるため、現在モニターの DPI に基づき整数ピクセル境界に揃えてから書き込みます。
/// </para>
/// <para>
/// XAML での使用方法:
/// <code>
/// &lt;b:Interaction.Behaviors>
///     &lt;metro:WindowSizeToContentBehavior ScrollTarget="{Binding ElementName=ContentScroll}" />
/// &lt;/b:Interaction.Behaviors>
/// </code>
/// </para>
/// </remarks>
[GenerateLogger]
public partial class WindowSizeToContentBehavior : Behavior<Window>
{
    /// <summary>
    /// <see cref="Window.Height"/> 更新判定で「変化なし」とみなす差分閾値 (DIP)。
    /// ピクセル境界スナップ後の値を比較するので、実質「1 px 未満の揺れは無視」する意味を持ちます。
    /// </summary>
    private const double _heightUpdateThreshold = 0.5;

    #region ScrollTarget dependency property

    public static readonly DependencyProperty ScrollTargetProperty
        = DependencyProperty.Register(
            nameof(ScrollTarget),
            typeof(FrameworkElement),
            typeof(WindowSizeToContentBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// コンテンツが求める高さの取得元、および <see cref="FrameworkElement.MaxHeight"/> の自動更新対象となる要素を取得または設定します。
    /// 通常はスクロール領域 (<see cref="ScrollViewer"/>) を指定します。
    /// </summary>
    /// <remarks>
    /// 指定された要素が <see cref="ScrollViewer"/> の場合は、その <see cref="ScrollViewer.ExtentHeight"/> を
    /// 「コンテンツが求める真の高さ」として参照するため、Expander 開閉等のスクロール対象内部の高さ変動を
    /// 直接拾えます。指定が null または <see cref="ScrollViewer"/> 以外の場合は、Window.Content の
    /// <see cref="FrameworkElement.DesiredSize"/> をフォールバックとして使用します。
    /// </remarks>
    public FrameworkElement? ScrollTarget
    {
        get => (FrameworkElement?)this.GetValue(ScrollTargetProperty);
        set => this.SetValue(ScrollTargetProperty, value);
    }

    #endregion

    #region EdgeMargin dependency property

    public static readonly DependencyProperty EdgeMarginProperty
        = DependencyProperty.Register(
            nameof(EdgeMargin),
            typeof(double),
            typeof(WindowSizeToContentBehavior),
            new PropertyMetadata(32.0));

    /// <summary>
    /// 画面端からの余白 (DIP) を取得または設定します。
    /// ウィンドウ高さおよび <see cref="ScrollTarget"/> の上限に対して適用されます。
    /// </summary>
    public double EdgeMargin
    {
        get => (double)this.GetValue(EdgeMarginProperty);
        set => this.SetValue(EdgeMarginProperty, value);
    }

    #endregion

    #region ChromeHeight dependency property

    public static readonly DependencyProperty ChromeHeightProperty
        = DependencyProperty.Register(
            nameof(ChromeHeight),
            typeof(double),
            typeof(WindowSizeToContentBehavior),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Window のクローム高さ (NC 領域 + シャドウ等の、コンテンツ高さに加算すべき DIP 値) を取得または設定します。
    /// 既定値は 0 で、<c>WindowChrome</c> + <c>ExtendsContentIntoTitleBar="True"</c> のようにクライアント領域が
    /// HWND 全体に拡張されている構成を想定しています。標準の Window 等で非零のクロームがある場合は計測値を指定してください。
    /// </summary>
    public double ChromeHeight
    {
        get => (double)this.GetValue(ChromeHeightProperty);
        set => this.SetValue(ChromeHeightProperty, value);
    }

    #endregion

    /// <summary>
    /// <see cref="Window.LocationChanged"/> ハンドラで「モニターを跨いだか」を判定するためのキャッシュ。
    /// </summary>
    private HMONITOR _lastMonitor;

    protected override void OnAttached()
    {
        base.OnAttached();

        // Window が既に Loaded ならただちに購読開始、まだなら Loaded を待つ。
        // (Loaded 前に LayoutUpdated 等を購読しても無害だが、初回 Recompute を有意義にしたいので
        //  Loaded 後に走らせる)
        if (this.AssociatedObject.IsLoaded)
        {
            this.AttachListeners();
        }
        else
        {
            this.AssociatedObject.Loaded += this.HandleLoaded;
        }
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.Loaded -= this.HandleLoaded;
        this.DetachListeners();
        base.OnDetaching();
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        this.AssociatedObject.Loaded -= this.HandleLoaded;
        this.AttachListeners();
    }

    private void AttachListeners()
    {
        this.AssociatedObject.LayoutUpdated += this.HandleLayoutUpdated;
        this.AssociatedObject.LocationChanged += this.HandleLocationChanged;
        this.AssociatedObject.DpiChanged += this.HandleDpiChanged;
        this.AssociatedObject.StateChanged += this.HandleStateChanged;

        // 初回フィット。force=true で変化検知ガードを無視させる
        // (起動直後は newHeight と現在の Window.Height がたまたま近接していてもガードに引っかからないように)
        this.Recompute(force: true);
    }

    private void DetachListeners()
    {
        if (this.AssociatedObject is not null)
        {
            this.AssociatedObject.LayoutUpdated -= this.HandleLayoutUpdated;
            this.AssociatedObject.LocationChanged -= this.HandleLocationChanged;
            this.AssociatedObject.DpiChanged -= this.HandleDpiChanged;
            this.AssociatedObject.StateChanged -= this.HandleStateChanged;
        }
    }

    private void HandleLayoutUpdated(object? sender, EventArgs e)
    {
        this.Recompute(force: false);
    }

    private void HandleLocationChanged(object? sender, EventArgs e)
    {
        // LocationChanged は同一モニター内の単純移動でも頻発するので、モニター ハンドルが
        // 変わったときだけ再フィットを起動する。
        var monitor = this.GetCurrentMonitor();
        if (monitor.IsNull || monitor == this._lastMonitor) return;

        this._lastMonitor = monitor;
        this.Recompute(force: true);
    }

    private void HandleDpiChanged(object? sender, DpiChangedEventArgs e)
    {
        // DPI 変化は SnapToPixel の換算結果に影響するので強制再計算
        this.Recompute(force: true);
    }

    private void HandleStateChanged(object? sender, EventArgs e)
    {
        // Maximized / Minimized 中はフィットをかけないため、Normal に戻った直後に再フィットを行う。
        if (this.AssociatedObject.WindowState == WindowState.Normal)
        {
            this.Recompute(force: true);
        }
    }

    /// <summary>
    /// 現在の状態に基づき、<see cref="Window.Height"/> と <see cref="ScrollTarget"/> の
    /// <see cref="FrameworkElement.MaxHeight"/> を再計算して適用します。
    /// </summary>
    /// <param name="force">変化検知ガードを無視して必ず適用するかどうか。
    /// 初期化、モニター跨ぎ、DPI 変化、状態遷移など「外部要因による状態変化」のときに true にします。</param>
    private void Recompute(bool force)
    {
        var window = this.AssociatedObject;

        // Maximized / Minimized 時はサイズ追従を行わない。
        // Normal 復帰時に HandleStateChanged から再度呼ばれる。
        if (window.WindowState != WindowState.Normal) return;

        // モニター情報 (作業領域) が取れなければ何もできない。
        // 多くは HWND がまだ確立していない極初期だけ起きる想定。
        var workArea = this.GetCurrentWorkAreaDip();
        if (workArea is not { } area) return;

        // コンテンツが求める高さを取得。ScrollTarget が ScrollViewer なら ExtentHeight 経由。
        var contentDesired = this.GetContentDesiredHeight();
        if (double.IsFinite(contentDesired) == false || contentDesired <= 0) return;

        var edge = Math.Max(0, this.EdgeMargin);
        var chrome = Math.Max(0, this.ChromeHeight);
        var cap = area.Height - edge;

        var target = this.ScrollTarget;
        var targetOffset = target is not null ? GetTargetOffset(window, target) : 0;

        // newHeight = コンテンツ高さ + クローム を作業領域 - 余白でクランプし、ピクセル境界に丸めた値。
        var newHeightRaw = Math.Min(contentDesired + chrome, cap);
        var newHeight = this.SnapToPixel(newHeightRaw);

        // 連続発火 (LayoutUpdated) でのループ防止: 既に目標高さなら何もしない。
        // contentDesired ではなく window.Height を比較対象にしている理由:
        //   - 同じコンテンツ状態でも、外部 (ウィンドウ状態復元、ユーザー操作等) で
        //     Window.Height が変えられた場合に再フィットを発火させたいため。
        //   - 起動時に過去セッションの巨大な保存高さが復元されると下部に空白が出る不具合があり、
        //     この比較対象変更で解消した経緯がある。
        if (force == false
            && double.IsFinite(newHeight)
            && Math.Abs(window.Height - newHeight) <= _heightUpdateThreshold)
        {
            return;
        }

        // ---- 診断ログ (通常はコメントアウト) ----
        // ウィンドウの構造を変更した際に挙動を再確認するために残しています。
        // 必要に応じて下のブロックをアンコメントして利用してください。
        /*
        var scroll = target as ScrollViewer;
        var extent = scroll?.ExtentHeight ?? double.NaN;
        var viewport = scroll?.ViewportHeight ?? double.NaN;
        var rootHeight = (window.Content as FrameworkElement)?.ActualHeight ?? double.NaN;
        Log.Debug("Recompute",
            new()
            {
                { force, "force" },
                { extent, "extent" },
                { viewport, "viewport" },
                { targetOffset, "offset" },
                { chrome, "chrome" },
                { window.ActualWidth, "winW" },
                { window.ActualHeight, "winH" },
                { window.Top, "winTop" },
                { rootHeight, "rootH" },
                { contentDesired, "desired" },
                { newHeightRaw, "newRaw" },
                { newHeight, "new" },
            });
        */

        // ScrollTarget 更新: 作業領域から「ScrollTarget 上方の他要素分」「クローム」「画面端余白」を引いた値が
        // スクロール領域の上限。Window が cap に達したとき ScrollTarget 内部が確実にスクロールに
        // 切り替わるようにするためのガードレール。
        if (target is not null)
        {
            var maxH = this.SnapToPixel(area.Height - chrome - targetOffset - edge);
            if (double.IsFinite(maxH)
                && maxH > 0
                && Math.Abs(target.MaxHeight - maxH) > _heightUpdateThreshold)
            {
                target.MaxHeight = maxH;
            }
        }

        // Window.Height 更新本体。ガード上ここに来た時点で書き込み必要性がある (差分 > 閾値)。
        // 念のため finite / 正値であることを再確認。
        if (double.IsFinite(newHeight)
            && newHeight > 0
            && Math.Abs(window.Height - newHeight) > _heightUpdateThreshold)
        {
            window.Height = newHeight;
        }
    }

    /// <summary>
    /// 値を現在モニターの DPI のピクセル境界に揃えます。
    /// 100% DPI 時は整数 DIP、125% DPI 時は 0.8 DIP 刻みなど、結果が整数ピクセルになる DIP 値を返します。
    /// </summary>
    /// <remarks>
    /// <see cref="Window.Height"/> にフラクショナル DIP を渡すと、内部レイアウトの丸めが状態によって
    /// 揺らぎ、コンテンツの一部が ±1 px シフトする要因となる。これを避けるため、書き込み前に
    /// ピクセル境界へ揃える。
    /// </remarks>
    private double SnapToPixel(double value)
    {
        if (double.IsFinite(value) == false) return value;

        var dpi = VisualTreeHelper.GetDpi(this.AssociatedObject);
        var scale = dpi.DpiScaleY;
        if (scale <= 0) return value;

        return Math.Round(value * scale) / scale;
    }

    /// <summary>
    /// コンテンツが求める「真の高さ」を返します。
    /// </summary>
    /// <remarks>
    /// <see cref="ScrollTarget"/> が <see cref="ScrollViewer"/> の場合は、
    /// <see cref="ScrollViewer.ExtentHeight"/> + その Y 座標を返します。<c>ExtentHeight</c> は
    /// スクロール領域に押し込まれる前のコンテンツ実寸を表すため、Window.ActualHeight の制約に
    /// 縛られない「コンテンツが要求している量」を得られます。
    /// それ以外の場合は <see cref="Window.Content"/> の <see cref="FrameworkElement.DesiredSize"/> を
    /// フォールバックとして使用しますが、こちらは Window.ActualHeight にクランプされる可能性が
    /// あるため、ScrollViewer 経路ほどの拡大方向追従性は得られません。
    /// </remarks>
    private double GetContentDesiredHeight()
    {
        var window = this.AssociatedObject;

        if (this.ScrollTarget is ScrollViewer scroll)
        {
            var extent = scroll.ExtentHeight;
            if (double.IsFinite(extent) && extent >= 0)
            {
                var offset = GetTargetOffset(window, scroll);
                return offset + extent;
            }
        }

        if (window.Content is FrameworkElement root)
        {
            return root.DesiredSize.Height;
        }

        return double.NaN;
    }

    /// <summary>
    /// 現在ウィンドウが属するモニター ハンドルを返します。未確立時は <see cref="HMONITOR.Null"/>。
    /// </summary>
    private HMONITOR GetCurrentMonitor()
    {
        var hwnd = (HWND)new WindowInteropHelper(this.AssociatedObject).Handle;
        return hwnd.IsNull
            ? HMONITOR.Null
            : PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
    }

    /// <summary>
    /// 現在モニターの作業領域を DIP 単位で返します。HWND 未確立時または取得失敗時は null。
    /// </summary>
    /// <remarks>
    /// <see cref="SystemParameters.WorkArea"/> はプライマリ モニター固定なので、マルチモニター環境で
    /// 正しく動作させるために HWND ベースで <c>MonitorFromWindow</c> + <c>GetMonitorInfo</c> を使う。
    /// 戻り値は <see cref="VisualTreeHelper.GetDpi"/> で取得した現在 DPI を用いてピクセル→DIP に換算。
    /// </remarks>
    private Size? GetCurrentWorkAreaDip()
    {
        var monitor = this.GetCurrentMonitor();
        if (monitor.IsNull) return null;

        this._lastMonitor = monitor;

        var info = new MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<MONITORINFO>(),
        };
        if (PInvoke.GetMonitorInfo(monitor, ref info) == false) return null;

        var work = info.rcWork;
        var dpi = VisualTreeHelper.GetDpi(this.AssociatedObject);
        return new Size(
            (work.right - work.left) / dpi.DpiScaleX,
            (work.bottom - work.top) / dpi.DpiScaleY);
    }

    /// <summary>
    /// <paramref name="target"/> の Y 座標を <paramref name="window"/> ローカル空間で返します。
    /// 通常は ScrollTarget = ScrollViewer の「上方にある行 (TitleBar / 中段要素 等) の合計高さ」を意味します。
    /// </summary>
    /// <remarks>
    /// 要素がまだロード前 / 同一ビジュアルツリーに連結されていない場合は <c>TransformToAncestor</c> が
    /// 例外を投げるため、安全側に倒して 0 を返す。次回の <c>LayoutUpdated</c> で再計算される。
    /// </remarks>
    private static double GetTargetOffset(Window window, FrameworkElement target)
    {
        if (target.IsLoaded == false) return 0;
        if (ReferenceEquals(target, window)) return 0;

        try
        {
            var transform = target.TransformToAncestor(window);
            var origin = transform.Transform(new Point(0, 0));
            return Math.Max(0, origin.Y);
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }
}
