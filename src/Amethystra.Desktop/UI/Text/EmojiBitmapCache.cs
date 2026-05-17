using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct2D.Common;
using Windows.Win32.Graphics.DirectWrite;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.System.Com;

namespace Amethystra.UI.Text;

/// <summary>
/// DirectWrite + Direct2D + WIC で絵文字シーケンスをラスタライズし、結果を <see cref="BitmapSource"/> として
/// キャッシュします。生成された <see cref="BitmapSource"/> は <see cref="Freezable.Freeze"/> 済みなので、
/// 異なる WPF コントロール間で安全に共有できます。
/// </summary>
public sealed class EmojiBitmapCache : IDisposable
{
    private const string _defaultEmojiFontFamily = "Segoe UI Emoji";

    private readonly Lock _gate = new();
    private readonly Dictionary<CacheKey, BitmapSource> _cache = [];

    private IDWriteFactory? _dWriteFactory;
    // ReSharper disable once InconsistentNaming
    private ID2D1Factory? _d2dFactory;
    private IWICImagingFactory? _wicFactory;
    private IDWriteTextFormat? _textFormat;
    private float _textFormatSize;

    /// <summary>
    /// 共有インスタンス。プロセス全体で 1 つ持つことを想定しています。
    /// </summary>
    public static EmojiBitmapCache Default { get; } = new();

    /// <summary>
    /// 指定された絵文字シーケンスを指定サイズ・DPI でラスタライズした <see cref="BitmapSource"/> を返します。
    /// 同一の入力に対しては同じインスタンスを返します。
    /// </summary>
    /// <param name="emojiText">単一の絵文字クラスタを表す文字列。</param>
    /// <param name="emSize">DIP 単位のフォントサイズ。</param>
    /// <param name="pixelsPerDip">1 DIP あたりのピクセル数 (DPI スケール)。</param>
    public BitmapSource? GetOrCreate(string emojiText, double emSize, double pixelsPerDip)
    {
        if (string.IsNullOrEmpty(emojiText)) return null;
        if (emSize <= 0 || pixelsPerDip <= 0) return null;

        var key = new CacheKey(emojiText, (float)emSize, (float)pixelsPerDip);

        lock (this._gate)
        {
            if (this._cache.TryGetValue(key, out var cached)) return cached;

            try
            {
                var bitmap = this.Render(emojiText, key.EmSize, key.PixelsPerDip);
                this._cache[key] = bitmap;
                return bitmap;
            }
            catch (Exception)
            {
                // ラスタライズに失敗した場合は null を返し、呼び出し側で素のテキスト描画にフォールバックする。
                return null;
            }
        }
    }

    public void Dispose()
    {
        lock (this._gate)
        {
            this._cache.Clear();

            if (this._textFormat is not null)
            {
                Marshal.ReleaseComObject(this._textFormat);
                this._textFormat = null;
            }

            if (this._wicFactory is not null)
            {
                Marshal.ReleaseComObject(this._wicFactory);
                this._wicFactory = null;
            }

            if (this._d2dFactory is not null)
            {
                Marshal.ReleaseComObject(this._d2dFactory);
                this._d2dFactory = null;
            }

            if (this._dWriteFactory is not null)
            {
                Marshal.ReleaseComObject(this._dWriteFactory);
                this._dWriteFactory = null;
            }
        }
    }

    private BitmapSource Render(string emojiText, float emSize, float pixelsPerDip)
    {
        var dWrite = this.EnsureDWriteFactory();
        // ReSharper disable once InconsistentNaming
        var d2d = this.EnsureD2DFactory();
        var wic = this.EnsureWicFactory();
        var format = this.EnsureTextFormat(emSize);

        // テキストメトリクスを取得して必要なピクセルサイズを決める。
        dWrite.CreateTextLayout(emojiText, (uint)emojiText.Length, format, emSize * 8f, emSize * 8f, out var layout);

        try
        {
            layout.GetMetrics(out var metrics);

            // 余白を少し付けつつ、上下方向は絵文字フォントの実高さに合わせる。
            var paddingDip = MathF.Max(emSize * 0.05f, 1f);
            var widthDip = MathF.Max(metrics.widthIncludingTrailingWhitespace, emSize) + paddingDip * 2f;
            var heightDip = MathF.Max(metrics.height, emSize) + paddingDip * 2f;

            var widthPx = (uint)Math.Max(1, Math.Ceiling(widthDip * pixelsPerDip));
            var heightPx = (uint)Math.Max(1, Math.Ceiling(heightDip * pixelsPerDip));

            var pixelFormat = PInvoke.GUID_WICPixelFormat32bppPBGRA;
            wic.CreateBitmap(widthPx, heightPx, in pixelFormat, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad, out var wicBitmap);

            var rtProps = new D2D1_RENDER_TARGET_PROPERTIES
            {
                type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
                pixelFormat = new D2D1_PIXEL_FORMAT
                {
                    format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                dpiX = 96f * pixelsPerDip,
                dpiY = 96f * pixelsPerDip,
                usage = D2D1_RENDER_TARGET_USAGE.D2D1_RENDER_TARGET_USAGE_NONE,
                minLevel = D2D1_FEATURE_LEVEL.D2D1_FEATURE_LEVEL_DEFAULT,
            };

            d2d.CreateWicBitmapRenderTarget(wicBitmap, in rtProps, out var rt);

            try
            {
                var black = new D2D1_COLOR_F { r = 0f, g = 0f, b = 0f, a = 1f };
                rt.CreateSolidColorBrush(in black, null, out var brush);

                try
                {
                    rt.BeginDraw();

                    var transparent = default(D2D1_COLOR_F);
                    unsafe
                    {
                        rt.Clear(&transparent);
                    }

                    var origin = new D2D_POINT_2F { x = paddingDip, y = paddingDip };
                    rt.DrawTextLayout(origin, layout, brush, D2D1_DRAW_TEXT_OPTIONS.D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);

                    unsafe
                    {
                        rt.EndDraw(null, null);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(brush);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(rt);
            }

            try
            {
                return CopyToBitmapSource(wicBitmap, widthPx, heightPx, pixelsPerDip);
            }
            finally
            {
                Marshal.ReleaseComObject(wicBitmap);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(layout);
        }
    }

    private static unsafe BitmapSource CopyToBitmapSource(IWICBitmap wicBitmap, uint widthPx, uint heightPx, float pixelsPerDip)
    {
        wicBitmap.Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, out var bitmapLock);

        try
        {
            bitmapLock.GetStride(out var stride);
            byte* dataPointer;
            bitmapLock.GetDataPointer(out var dataSize, &dataPointer);

            var buffer = new byte[dataSize];
            Marshal.Copy((nint)dataPointer, buffer, 0, (int)dataSize);

            var dpi = 96.0 * pixelsPerDip;
            var bmp = BitmapSource.Create(
                (int)widthPx,
                (int)heightPx,
                dpi,
                dpi,
                PixelFormats.Pbgra32,
                null,
                buffer,
                (int)stride);

            bmp.Freeze();
            return bmp;
        }
        finally
        {
            Marshal.ReleaseComObject(bitmapLock);
        }
    }

    private IDWriteFactory EnsureDWriteFactory()
    {
        if (this._dWriteFactory is not null) return this._dWriteFactory;

        PInvoke.DWriteCreateFactory(
            DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
            typeof(IDWriteFactory).GUID,
            out var factory);

        this._dWriteFactory = (IDWriteFactory)factory;
        return this._dWriteFactory;
    }

    private ID2D1Factory EnsureD2DFactory()
    {
        if (this._d2dFactory is not null) return this._d2dFactory;

        unsafe
        {
            var iid = typeof(ID2D1Factory).GUID;
            PInvoke.D2D1CreateFactory(
                D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED,
                &iid,
                null,
                out var factory);

            this._d2dFactory = (ID2D1Factory)factory;
        }

        return this._d2dFactory;
    }

    private IWICImagingFactory EnsureWicFactory()
    {
        if (this._wicFactory is not null) return this._wicFactory;

        var hr = PInvoke.CoCreateInstance(
            PInvoke.CLSID_WICImagingFactory,
            null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            out IWICImagingFactory factory);

        Marshal.ThrowExceptionForHR(hr.Value);

        this._wicFactory = factory;
        return this._wicFactory;
    }

    private IDWriteTextFormat EnsureTextFormat(float emSize)
    {
        if (this._textFormat is not null && Math.Abs(this._textFormatSize - emSize) < 0.01f)
        {
            return this._textFormat;
        }

        if (this._textFormat is not null)
        {
            Marshal.ReleaseComObject(this._textFormat);
            this._textFormat = null;
        }

        var dWrite = this.EnsureDWriteFactory();
        dWrite.CreateTextFormat(
            _defaultEmojiFontFamily,
            null,
            DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
            DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
            DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
            emSize,
            "",
            out var format);

        format.SetTextAlignment(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_LEADING);
        format.SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT.DWRITE_PARAGRAPH_ALIGNMENT_NEAR);
        format.SetWordWrapping(DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_NO_WRAP);

        this._textFormat = format;
        this._textFormatSize = emSize;
        return this._textFormat;
    }

    private readonly record struct CacheKey(string Text, float EmSize, float PixelsPerDip);
}
