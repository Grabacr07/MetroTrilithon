using System;
using System.Runtime.InteropServices;

namespace Amethystra.Win32;

/// <summary>
/// ウィンドウ コンポジションの非公開 API へのラッパーを提供します。
/// </summary>
/// <remarks>
/// <para>
/// 公開 API は <c>Windows.Win32.PInvoke</c> (CsWin32 生成) を経由するのが原則ですが、
/// <c>SetWindowCompositionAttribute</c> は Windows SDK のメタデータに含まれないため CsWin32 では扱えません。
/// このクラスでは <see cref="LibraryImportAttribute"/> による手書き宣言で同 API を呼び出します。
/// </para>
/// <para>
/// 非公開 API のため、Windows のバージョンアップによりシグネチャや動作が変わる可能性があります。
/// </para>
/// </remarks>
public static partial class WindowComposition
{
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    /// <summary>
    /// 指定されたウィンドウに <see cref="AccentPolicy"/> を適用します。
    /// </summary>
    /// <param name="hwnd">適用対象のウィンドウ ハンドル。</param>
    /// <param name="policy">適用する <see cref="AccentPolicy"/>。</param>
    /// <remarks>
    /// <para>
    /// Windows 10 1803 以降の Acrylic / Blur 効果を有効化する用途で使用します。
    /// Windows 11 22H2 以降では Acrylic blur が制限される (薄いグラデーション扱いになる) ため、
    /// 視覚効果が低減することがあります。
    /// </para>
    /// </remarks>
    public static void SetAccentPolicy(IntPtr hwnd, AccentPolicy policy)
    {
        var size = Marshal.SizeOf<AccentPolicy>();
        var policyPtr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(policy, policyPtr, fDeleteOld: false);
            var data = new WindowCompositionAttributeData()
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                Data = policyPtr,
                SizeOfData = size,
            };
            _ = SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(policyPtr);
        }
    }
}

/// <summary>
/// <c>SetWindowCompositionAttribute</c> へ渡すデータを表します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct WindowCompositionAttributeData
{
    /// <summary>
    /// 適用する属性の種別を取得または設定します。
    /// </summary>
    public WindowCompositionAttribute Attribute;

    /// <summary>
    /// 属性データへのポインタを取得または設定します。
    /// </summary>
    public IntPtr Data;

    /// <summary>
    /// <see cref="Data"/> が指すデータのバイト サイズを取得または設定します。
    /// </summary>
    public int SizeOfData;
}

/// <summary>
/// <see cref="WindowCompositionAttributeData.Attribute"/> に指定する属性の種別を表します。
/// </summary>
public enum WindowCompositionAttribute
{
    /// <summary>
    /// アクセント ポリシーを適用することを示します。
    /// </summary>
    WCA_ACCENT_POLICY = 19,
}

/// <summary>
/// ウィンドウに適用するアクセント効果のポリシーを表します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct AccentPolicy
{
    /// <summary>
    /// 適用するアクセント効果の種別を取得または設定します。
    /// </summary>
    public AccentState AccentState;

    /// <summary>
    /// 描画する境界線などのオプションを取得または設定します。
    /// </summary>
    public AccentFlags AccentFlags;

    /// <summary>
    /// アクセントの tint 色を ABGR 形式で取得または設定します。
    /// </summary>
    /// <remarks>
    /// 下位 24 bit が RGB、上位 8 bit が alpha を表します。
    /// </remarks>
    public uint GradientColor;

    /// <summary>
    /// アニメーション識別子を取得または設定します。
    /// </summary>
    public uint AnimationId;
}

/// <summary>
/// ウィンドウに適用するアクセント効果の種別を表します。
/// </summary>
public enum AccentState
{
    /// <summary>
    /// アクセント効果を適用しません。
    /// </summary>
    ACCENT_DISABLED = 0,

    /// <summary>
    /// 単色のグラデーションを適用します。
    /// </summary>
    ACCENT_ENABLE_GRADIENT = 1,

    /// <summary>
    /// 透過つきのグラデーションを適用します。
    /// </summary>
    ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,

    /// <summary>
    /// 背景にぼかし (Blur Behind) を適用します。
    /// </summary>
    ACCENT_ENABLE_BLURBEHIND = 3,

    /// <summary>
    /// Acrylic blur を適用します。
    /// </summary>
    ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,

    /// <summary>
    /// 無効な状態を示します。
    /// </summary>
    ACCENT_INVALID_STATE = 5,
}

/// <summary>
/// <see cref="AccentPolicy"/> に指定する境界線描画オプションを表します。
/// </summary>
[Flags]
public enum AccentFlags : uint
{
    /// <summary>
    /// 境界線を描画しません。
    /// </summary>
    None = 0,

    /// <summary>
    /// 左辺に境界線を描画します。
    /// </summary>
    DrawLeftBorder = 0x20,

    /// <summary>
    /// 上辺に境界線を描画します。
    /// </summary>
    DrawTopBorder = 0x40,

    /// <summary>
    /// 右辺に境界線を描画します。
    /// </summary>
    DrawRightBorder = 0x80,

    /// <summary>
    /// 下辺に境界線を描画します。
    /// </summary>
    DrawBottomBorder = 0x100,

    /// <summary>
    /// すべての辺に境界線を描画します。
    /// </summary>
    DrawAllBorders = DrawLeftBorder | DrawTopBorder | DrawRightBorder | DrawBottomBorder,
}
