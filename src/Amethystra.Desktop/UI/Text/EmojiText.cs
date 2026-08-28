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

            textBlock.SetValue(LastRenderedTextProperty, text);
            textBlock.Text = text ?? string.Empty;
            return;
        }

        // Loaded / Unloaded を一度だけ購読する。複数回 HandleTextChanged が走っても二重登録にならないよう先に解除する。
        textBlock.Loaded -= HandleLoaded;
        textBlock.Loaded += HandleLoaded;
        textBlock.Unloaded -= HandleUnloaded;
        textBlock.Unloaded += HandleUnloaded;

        ScheduleRebuild(textBlock);
    }

    #endregion

    /// <remarks>
    /// 各 TextBlock が現在購読している Window.DpiChanged ハンドラを保持するための内部添付プロパティ。
    /// Unloaded 時に確実に購読解除できるよう、ハンドラ参照を要素自身に紐付けて保管する。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty DpiHandlerProperty
        = DependencyProperty.RegisterAttached(
            nameof(DpiHandlerProperty).GetPropertyName(),
            typeof(DpiChangedEventHandler),
            typeof(EmojiText),
            new PropertyMetadata(null));

    /// <remarks>
    /// 各 TextBlock が現在キューに積んでいる再構築 DispatcherOperation を保持する。
    /// 新しい再構築要求が来たら旧オペレーションを Abort して、ディスパッチャキューが
    /// 高速スクロール中に肥大化するのを防ぐ。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty PendingRebuildProperty
        = DependencyProperty.RegisterAttached(
            nameof(PendingRebuildProperty).GetPropertyName(),
            typeof(DispatcherOperation),
            typeof(EmojiText),
            new PropertyMetadata(null));

    /// <remarks>
    /// 最後にレンダリングしたテキスト。値が変わっていないときの無意味な再構築をスキップするため。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty LastRenderedTextProperty
        = DependencyProperty.RegisterAttached(
            nameof(LastRenderedTextProperty).GetPropertyName(),
            typeof(string),
            typeof(EmojiText),
            new PropertyMetadata(null));

    private static void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock) return;

        AttachDpiHandler(textBlock);

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
    }

    /// <summary>
    /// キューに積まれている再構築オペレーションがあれば破棄します。
    /// </summary>
    private static void AbortPendingRebuild(TextBlock textBlock)
    {
        if (textBlock.GetValue(PendingRebuildProperty) is not DispatcherOperation pending) return;

        pending.Abort();
        textBlock.ClearValue(PendingRebuildProperty);
    }

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
    /// また、最後にレンダリングしたテキストと比較して同一であれば、no-op として早期復帰します。
    /// 仮想化コンテナの再利用で同じテキストが割り当てられたケースで再構築コストを払わずに済みます。
    /// </para>
    /// </remarks>
    private static void ScheduleRebuild(TextBlock textBlock)
    {
        var newText = GetText(textBlock);
        var lastText = (string?)textBlock.GetValue(LastRenderedTextProperty);
        if (string.Equals(newText, lastText, StringComparison.Ordinal)) return;

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
        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(text)) return;

        var pixelsPerDip = GetPixelsPerDip(textBlock);
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

    private static double GetPixelsPerDip(Visual visual)
    {
        var source = PresentationSource.FromVisual(visual);
        if (source?.CompositionTarget is null) return 1.0;

        // TransformToDevice.M11 はテキスト方向 (X 軸) の DPI スケールを表す。
        var scale = source.CompositionTarget.TransformToDevice.M11;
        return scale > 0 ? scale : 1.0;
    }
}
