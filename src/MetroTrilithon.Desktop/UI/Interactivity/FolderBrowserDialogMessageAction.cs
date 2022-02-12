using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Livet.Behaviors.Messaging;
using Livet.Messaging;

namespace MetroTrilithon.UI.Interactivity
{
    public class FolderBrowserDialogMessageAction : InteractionMessageAction<DependencyObject>
    {
        protected override void InvokeAction(InteractionMessage im)
        {
            if (!(im is FolderBrowserDialogMessage message)) return;

            var owner = Window.GetWindow(this.AssociatedObject);

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(owner is IWin32Window window)) window = new Win32Window(owner);

            var path = Environment.ExpandEnvironmentVariables(message.SelectedPath);
            if (string.IsNullOrWhiteSpace(path)) path = ".\\";

            var dialog = new FolderBrowserDialog()
            {
                AutoUpgradeEnabled = message.AutoUpgradeEnabled,
                Description = message.Description,
                RootFolder = message.RootFolder,
                SelectedPath = Path.GetFullPath(path),
                ShowNewFolderButton = message.ShowNewFolderButton,
                UseDescriptionForTitle = message.UseDescriptionForTitle,
            };

            if (dialog.ShowDialog(window) == DialogResult.OK)
            {
                message.Response = dialog.SelectedPath;
            }
        }

        private class Win32Window : IWin32Window
        {
            private readonly Window _source;

            public IntPtr Handle
                => new System.Windows.Interop.WindowInteropHelper(this._source).Handle;

            public Win32Window(Window source)
            {
                this._source = source;
            }
        }
    }
}
