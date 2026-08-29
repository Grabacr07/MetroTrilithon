using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Amethystra.UI.Text;

/// <summary>
/// <see cref="TextBlock"/> に対して、テキスト中の絵文字シーケンスをカラー絵文字としてレンダリングする添付プロパティを提供します。
/// </summary>
/// <remarks>
/// 通常の <see cref="TextBlock.Text"/> は WPF の標準テキスト描画パスに乗るため、Segoe UI Emoji のカラーグリフテーブルを参照できません。<see cref="TextProperty"/> を
/// <c>Text</c> の代わりに使うと、絵文字部分のみ DirectWrite + Direct2D で <see cref="BitmapSource"/> 化し、<see cref="InlineUIContainer"/>
/// としてインラインに差し込みます。
/// </remarks>
public static class EmojiText
{
    #region Text attached property

    public static readonly DependencyProperty TextProperty
        = DependencyProperty.RegisterAttached(
            nameof(TextProperty).GetPropertyName(),
            typeof(string),
            typeof(EmojiText),
            new PropertyMetadata(null, HandleTextChanged));

    public static void SetText(DependencyObject element, string? value)
        => element.SetValue(TextProperty, value);

    public static string? GetText(DependencyObject element)
        => (string?)element.GetValue(TextProperty);

    private static void HandleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        // 絵文字を含まないテキストは標準のテキスト描画パスへそのまま流す。
        // 遅延再構築・Loaded 購読・DPI 追跡がすべて不要になり、リスト行の大量生成時のコストを抑える。
        var text = (string?)e.NewValue;
        if (EmojiSegmenter.ContainsEmoji(text) == false)
        {
            textBlock.Loaded -= HandleLoaded;
            textBlock.Unloaded -= HandleUnloaded;
            AbortPendingRebuild(textBlock);
            DetachDpiHandler(textBlock);
            DetachLayoutUpdatedHandler(textBlock);

            textBlock.SetValue(LastRenderedTextProperty, text);
            textBlock.Text = text ?? string.Empty;
            return;
        }

        // Loaded / Unloaded を一度だけ購読する。複数回 HandleTextChanged が走っても二重登録にならないよう先に解除する。
        textBlock.Loaded -= HandleLoaded;
        textBlock.Loaded += HandleLoaded;
        textBlock.Unloaded -= HandleUnloaded;
        textBlock.Unloaded += HandleUnloaded;

        // リサイクルされたコンテナーでは Loaded が再発火しないため、ここでも購読しておく (二重登録は内部で防止される)。
        AttachLayoutUpdatedHandler(textBlock);

        ScheduleRebuild(textBlock);
    }

    #endregion

    #region DpiHandler attached property

    /// <summary>
    /// 各 <see cref="TextBlock"/> が現在購読している <see cref="Window.DpiChanged"/> ハンドラを保持します。
    /// </summary>
    /// <remarks>
    /// Unloaded 時に確実に購読解除できるよう、ハンドラ参照を要素自身に紐付けて保管します。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty DpiHandlerProperty
        = DependencyProperty.RegisterAttached(
            nameof(DpiHandlerProperty).GetPropertyName(),
            typeof(DpiChangedEventHandler),
            typeof(EmojiText),
            new PropertyMetadata(null));

    private static void AttachDpiHandler(TextBlock textBlock)
    {
        // 既に購読済みであれば一度外す。Loaded がアイテム仮想化などで複数回発火する状況に備える。
        DetachDpiHandler(textBlock);

        var window = Window.GetWindow(textBlock);
        if (window is null) return;

        // 新しい DPI でラスタライズし直す。EmojiBitmapCache は (text, emSize, pixelsPerDip) をキーに
        // 別エントリを作るので、古いビットマップは残るがレイアウトには新しい鮮明な方が使われる。
        DpiChangedEventHandler handler = (_, _) => ScheduleRebuild(textBlock);
        textBlock.SetValue(DpiHandlerProperty, handler);
        window.DpiChanged += handler;
    }

    private static void DetachDpiHandler(TextBlock textBlock)
    {
        if (textBlock.GetValue(DpiHandlerProperty) is not DpiChangedEventHandler handler) return;

        Window.GetWindow(textBlock)?.DpiChanged -= handler;

        textBlock.ClearValue(DpiHandlerProperty);
    }

    #endregion

    #region PendingRebuild attached property

    /// <summary>
    /// 各 <see cref="TextBlock"/> が現在キューに積んでいる再構築の <see cref="DispatcherOperation"/> を保持します。
    /// </summary>
    /// <remarks>
    /// 新しい再構築要求が来たら旧オペレーションを <see cref="DispatcherOperation.Abort"/> して、
    /// 高速スクロール中にディスパッチャキューが肥大化するのを防ぎます。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty PendingRebuildProperty
        = DependencyProperty.RegisterAttached(
            nameof(PendingRebuildProperty).GetPropertyName(),
            typeof(DispatcherOperation),
            typeof(EmojiText),
            new PropertyMetadata(null));

    /// <summary>
    /// キューに積まれている再構築オペレーションがあれば破棄します。
    /// </summary>
    private static void AbortPendingRebuild(TextBlock textBlock)
    {
        if (textBlock.GetValue(PendingRebuildProperty) is not DispatcherOperation pending) return;

        pending.Abort();
        textBlock.ClearValue(PendingRebuildProperty);
    }

    #endregion

    #region LastRenderedText attached property

    /// <summary>
    /// 最後にレンダリングしたテキストを保持します。
    /// </summary>
    /// <remarks>
    /// 値が変わっていないときの無意味な再構築をスキップするために使用します。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty LastRenderedTextProperty
        = DependencyProperty.RegisterAttached(
            nameof(LastRenderedTextProperty).GetPropertyName(),
            typeof(string),
            typeof(EmojiText),
            new PropertyMetadata(null));

    #endregion

    #region LastRenderedScale attached property

    /// <summary>
    /// 最後にレンダリングした実効スケール (デバイス DPI × 祖先の LayoutTransform の累積スケール) を保持します。
    /// 未レンダリングの場合は 0 です。
    /// </summary>
    /// <remarks>
    /// スケールが変わっていないときの無意味な再構築をスキップするために使用します。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty LastRenderedScaleProperty
        = DependencyProperty.RegisterAttached(
            nameof(LastRenderedScaleProperty).GetPropertyName(),
            typeof(double),
            typeof(EmojiText),
            new PropertyMetadata(0.0));

    /// <summary>
    /// 実効スケールの比較に使う許容誤差。この差を超えたときのみ再構築します。
    /// </summary>
    private const double _scaleEpsilon = 0.001;

    #endregion

    #region LayoutUpdatedHandler attached property

    /// <summary>
    /// 各 <see cref="TextBlock"/> が現在購読している <see cref="UIElement.LayoutUpdated"/> ハンドラを保持します。
    /// </summary>
    /// <remarks>
    /// ズームなどの LayoutTransform 変更で実効スケールが変わったことを検知するための購読で、Unloaded 時や
    /// 絵文字を含まないテキストへの変更時に確実に解除できるよう、ハンドラ参照を要素自身に紐付けて保管します。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty LayoutUpdatedHandlerProperty
        = DependencyProperty.RegisterAttached(
            nameof(LayoutUpdatedHandlerProperty).GetPropertyName(),
            typeof(EventHandler),
            typeof(EmojiText),
            new PropertyMetadata(null));

    private static void AttachLayoutUpdatedHandler(TextBlock textBlock)
    {
        // 既に購読済みであれば何もしない。ハンドラはインスタンスごとのクロージャなので、二重登録だけ防げばよい。
        if (textBlock.GetValue(LayoutUpdatedHandlerProperty) is not null) return;

        // LayoutUpdated の sender は常に null のため、対象要素はクロージャで捕捉する。
        EventHandler handler = (_, _) => HandleLayoutUpdated(textBlock);
        textBlock.SetValue(LayoutUpdatedHandlerProperty, handler);
        textBlock.LayoutUpdated += handler;
    }

    private static void DetachLayoutUpdatedHandler(TextBlock textBlock)
    {
        if (textBlock.GetValue(LayoutUpdatedHandlerProperty) is not EventHandler handler) return;

        textBlock.LayoutUpdated -= handler;
        textBlock.ClearValue(LayoutUpdatedHandlerProperty);
    }

    /// <summary>
    /// レイアウト更新のたびに実効スケールを確認し、前回レンダリング時から変わっていれば再構築を予約します。
    /// </summary>
    /// <remarks>
    /// LayoutUpdated はビジュアルツリー内のあらゆるレイアウトパスで発火するため、ここでの処理は
    /// 「スケールを計算して前回値と比較する」だけの軽量なものに留め、変化したときのみ
    /// <see cref="ScheduleRebuild"/> の Background キューへ委譲する。
    /// </remarks>
    private static void HandleLayoutUpdated(TextBlock textBlock)
    {
        if (textBlock.IsLoaded == false) return;

        // 一度もレンダリングしていない (初期レイアウト中の) 場合は、Loaded / ScheduleRebuild 側に任せる。
        var lastScale = (double)textBlock.GetValue(LastRenderedScaleProperty);
        if (lastScale <= 0) return;

        var scale = GetEffectiveScale(textBlock);
        if (Math.Abs(scale - lastScale) < _scaleEpsilon) return;

        ScheduleRebuild(textBlock);
    }

    #endregion

    private static void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock) return;

        AttachDpiHandler(textBlock);
        AttachLayoutUpdatedHandler(textBlock);

        // Loaded 時点では Measure/Arrange パスを抜けているので同期実行で OK。
        // HandleTextChanged 経由で Background キューに積まれた再構築要求があれば破棄し、ここで先に処理する。
        // これによって「ウィンドウ表示直後に Inlines が空 → 一拍遅れて埋まる」というチラつきを抑える。
        AbortPendingRebuild(textBlock);

        var text = GetText(textBlock);
        textBlock.SetValue(LastRenderedTextProperty, text);
        RebuildInlines(textBlock, text);
    }

    private static void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock) return;

        AbortPendingRebuild(textBlock);
        DetachDpiHandler(textBlock);
        DetachLayoutUpdatedHandler(textBlock);
    }

    /// <summary>
    /// Inlines の再構築をディスパッチャキューに積みます。
    /// </summary>
    /// <remarks>
    /// バインディング評価が ItemsControl の Measure パス中に走ると、その同じ呼び出しスタックで
    /// <see cref="TextBlock.Inlines"/> を変更することになり、"測定または整列中に、ドキュメント ツリーまたはプロパティが変更されました"
    /// 例外が出ます。さらに、仮想化されたリストで高速スクロールするとリサイクルされる TextBlock の数だけ再構築要求が走るので、優先度と重複制御が重要になります。
    /// <para>
    /// 戦略: 既存の保留中オペレーションがあれば <see cref="DispatcherOperation.Abort"/> で破棄し、新しいオペレーションを <see cref="DispatcherPriority.Background"/>
    /// で積み直します。これにより、スクロール中はキューが肥大化せず、入力やレンダリングを邪魔せず、スクロールが落ち着いた瞬間にまとめて再構築されます。
    /// </para>
    /// <para>
    /// また、最後にレンダリングしたテキスト・実効スケールと比較していずれも同一であれば、no-op として早期復帰します。
    /// 仮想化コンテナの再利用で同じテキストが割り当てられたケースで再構築コストを払わずに済みます。
    /// </para>
    /// </remarks>
    private static void ScheduleRebuild(TextBlock textBlock)
    {
        var newText = GetText(textBlock);
        var lastText = (string?)textBlock.GetValue(LastRenderedTextProperty);
        var lastScale = (double)textBlock.GetValue(LastRenderedScaleProperty);
        if (string.Equals(newText, lastText, StringComparison.Ordinal)
            && Math.Abs(GetEffectiveScale(textBlock) - lastScale) < _scaleEpsilon)
        {
            return;
        }

        // 既存の保留中オペレーションがあれば破棄して、後発の要求で上書きする。
        if (textBlock.GetValue(PendingRebuildProperty) is DispatcherOperation existing)
        {
            existing.Abort();
        }

        var op = textBlock.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                textBlock.ClearValue(PendingRebuildProperty);
                var current = GetText(textBlock);
                textBlock.SetValue(LastRenderedTextProperty, current);
                RebuildInlines(textBlock, current);
            }));

        textBlock.SetValue(PendingRebuildProperty, op);
    }

    private static void RebuildInlines(TextBlock textBlock, string? text)
    {
        var pixelsPerDip = GetEffectiveScale(textBlock);
        textBlock.SetValue(LastRenderedScaleProperty, pixelsPerDip);

        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(text)) return;

        var fontSize = textBlock.FontSize;

        foreach (var segment in EmojiSegmenter.Split(text))
        {
            if (segment.Kind == EmojiSegmenter.SegmentKind.Text)
            {
                textBlock.Inlines.Add(new Run(segment.Text));
                continue;
            }

            var bitmap = EmojiBitmapCache.Default.GetOrCreate(segment.Text, fontSize, pixelsPerDip);
            if (bitmap is null)
            {
                // フォールバック: ラスタライズに失敗したセグメントは素のテキストとして描画する。
                textBlock.Inlines.Add(new Run(segment.Text));
                continue;
            }

            var image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Width = bitmap.Width,
                Height = bitmap.Height,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
            };

            var container = new InlineUIContainer(image)
            {
                BaselineAlignment = BaselineAlignment.Center,
            };

            textBlock.Inlines.Add(container);
        }
    }

    /// <summary>
    /// 指定された要素の実効スケール (1 DIP が最終的に何ピクセルで描画されるか) を返します。
    /// </summary>
    /// <remarks>
    /// デバイス DPI スケールに加えて、エディターのズームのような祖先の LayoutTransform / RenderTransform による
    /// 累積スケールを掛け合わせます。これを掛けておかないと、等倍でラスタライズしたビットマップが
    /// レイアウト側の変換で引き伸ばされてボケてしまう。
    /// </remarks>
    private static double GetEffectiveScale(Visual visual)
    {
        var source = PresentationSource.FromVisual(visual);
        if (source?.CompositionTarget is null) return 1.0;

        // TransformToDevice.M11 はテキスト方向 (X 軸) の DPI スケールを表す。
        var scale = source.CompositionTarget.TransformToDevice.M11;
        if (scale <= 0) scale = 1.0;

        if (source.RootVisual is { } root && ReferenceEquals(root, visual) == false)
        {
            try
            {
                // TransformToAncestor はレイアウトオフセットを含む累積変換を返すが、
                // M11 / M12 からはスケール (回転を含む場合も X 軸方向の拡大率) だけを取り出せる。
                if (visual.TransformToAncestor(root) is Transform { Value: var matrix })
                {
                    var layoutScale = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
                    if (layoutScale > 0) scale *= layoutScale;
                }
            }
            catch (InvalidOperationException)
            {
                // ビジュアルツリーからの切断と競合した場合は、デバイス DPI のみのスケールへフォールバックする。
            }
        }

        return scale;
    }
}
