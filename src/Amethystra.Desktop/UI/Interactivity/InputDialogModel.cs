using System;
using Amethystra.Mvvm;
using R3;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// 1 行入力ダイアログの内容を保持するデータ ViewModel です。
/// </summary>
/// <remarks>
/// <para>
/// このオブジェクトを <see cref="ConfirmMessage.Content"/> に渡すと、既定の <see cref="DataTemplate"/> によって入力フォームの UI が展開されます。
/// </para>
/// <para>
/// コンストラクタに渡した <c>validator</c> は <see cref="Text"/> の変化に応じて自動的に呼び出され、戻り値が
/// <see cref="ErrorMessage"/> に反映されます。<see cref="ConfirmMessage.CanCommit"/> には <see cref="IsValid"/>
/// を渡せばバリデーション結果に応じてプライマリ ボタンの有効/無効が制御されます。
/// </para>
/// </remarks>
public sealed class InputDialogModel : ViewModelBase
{
    /// <summary>
    /// テキスト ボックスの上に表示する説明文を取得します。空文字または <see langword="null"/> の場合は表示されません。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// テキスト ボックスのプレースホルダー (空欄時のヒント) を取得します。空文字または <see langword="null"/> の場合は表示されません。
    /// </summary>
    public string? PlaceholderText { get; init; }

    /// <summary>
    /// <see cref="PlaceholderText"/> が指定されているかどうかを取得します。View 側でプレースホルダーの表示有無を判定するために使用します。
    /// </summary>
    public bool HasPlaceholder
        => string.IsNullOrEmpty(this.PlaceholderText) == false;

    /// <summary>
    /// ユーザーが入力中のテキストを取得します。
    /// </summary>
    public BindableReactiveProperty<string> Text { get; }

    /// <summary>
    /// テキスト ボックス下に表示するエラー メッセージを取得します。
    /// <see langword="null"/> または空文字の場合は表示されません。
    /// </summary>
    /// <remarks>
    /// コンストラクタに <c>validator</c> を渡した場合、<see cref="Text"/> の変化に応じて自動で更新されます。
    /// </remarks>
    public BindableReactiveProperty<string?> ErrorMessage { get; }

    /// <summary>
    /// <see cref="ErrorMessage"/> が空かどうかを示すストリームを取得します。<see cref="ConfirmMessage.CanCommit"/> への接続に使用します。
    /// </summary>
    public Observable<bool> IsValid
        => this.ErrorMessage.Select(static e => string.IsNullOrEmpty(e));

    /// <summary>
    /// <see cref="InputDialogModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="initialText">テキスト ボックスの初期値。</param>
    /// <param name="validator">
    /// 入力検証関数。<see cref="Text"/> の変化に応じて呼び出され、戻り値が <see cref="ErrorMessage"/> に反映されます。
    /// 戻り値が <see langword="null"/> なら有効な入力です。<see langword="null"/> を渡した場合は自動検証を行いません。
    /// </param>
    public InputDialogModel(string initialText = "", Func<string, string?>? validator = null)
    {
        this.Text = new BindableReactiveProperty<string>(initialText).AddTo(this);
        this.ErrorMessage = new BindableReactiveProperty<string?>().AddTo(this);

        if (validator is not null)
        {
            this.Text
                .Subscribe(value => this.ErrorMessage.Value = validator(value))
                .AddTo(this);
        }
    }
}
