using System.Threading.Tasks;
using JetBrains.Annotations;
using R3;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="Subject{T}"/> に対するウィンドウ遷移の拡張メソッドを提供します。
/// </summary>
public static class WindowTransitionExtensions
{
    extension(Subject<WindowTransitionRequest> subject)
    {
        public void Show(object viewModel)
            => subject.OnNext(new WindowTransitionRequest(viewModel));

        [MustUseReturnValue]
        public Task<bool?> ShowDialogAsync(
            object viewModel,
            CancellationToken cancellationToken = default)
        {
            var request = new WindowTransitionRequest(viewModel, WindowTransitionMode.ShowDialog);
            subject.OnNext(request);
            return request.WaitForResultAsync(cancellationToken);
        }
    }
}
