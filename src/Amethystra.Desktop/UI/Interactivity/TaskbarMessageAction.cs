using System.Windows;
using System.Windows.Shell;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="TaskbarMessage"/> を受け取り、アタッチされた <see cref="Window"/> の <see cref="Window.TaskbarItemInfo"/> プロパティを設定する機能を提供します。
/// </summary>
public class TaskbarMessageAction : Behavior<Window>
{
    #region Source dependency property

    public static readonly DependencyProperty SourceProperty
        = DependencyProperty.Register(
            nameof(Source),
            typeof(TaskbarMessage),
            typeof(TaskbarMessageAction),
            new PropertyMetadata(null, HandleSourceChanged));

    public TaskbarMessage? Source
    {
        get => (TaskbarMessage?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    private static void HandleSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TaskbarMessageAction action && e.NewValue is TaskbarMessage message)
        {
            action.Apply(message);
        }
    }

    #endregion

    private void Apply(TaskbarMessage message)
    {
        if (this.AssociatedObject == null) return;

        var taskbarInfo = this.AssociatedObject.TaskbarItemInfo
            ?? (this.AssociatedObject.TaskbarItemInfo = new TaskbarItemInfo());

        if (message.ProgressState != null)
        {
            taskbarInfo.ProgressState = message.ProgressState.Value;
        }

        if (message.ProgressValue != null)
        {
            taskbarInfo.ProgressValue = message.ProgressValue.Value;
        }

        if (message.Overlay != null)
        {
            taskbarInfo.Overlay = message.Overlay;
        }

        if (message.Description != null)
        {
            taskbarInfo.Description = message.Description;
        }

        if (message.ThumbnailClipMargin != null)
        {
            taskbarInfo.ThumbnailClipMargin = message.ThumbnailClipMargin.Value;
        }

        if (message.ThumbButtonInfos != null)
        {
            taskbarInfo.ThumbButtonInfos = message.ThumbButtonInfos;
        }
    }
}
