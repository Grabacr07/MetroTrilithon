using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Livet.Messaging;

namespace MetroTrilithon.UI.Interactivity
{
    /// <summary>
    /// <see cref="TaskbarMessageAction"/> 経由で <see cref="Window.TaskbarItemInfo"/> を設定するための相互作用メッセージを表します。
    /// </summary>
    public class TaskbarMessage : InteractionMessage
    {
        #region ProgressState Dependency property

        public static readonly DependencyProperty ProgressStateProperty
            = DependencyProperty.Register(
                nameof(ProgressState),
                typeof(TaskbarItemProgressState?),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));

        public TaskbarItemProgressState? ProgressState
        {
            get => (TaskbarItemProgressState?)this.GetValue(ProgressStateProperty);
            set => this.SetValue(ProgressStateProperty, value);
        }

        #endregion

        #region ProgressValue Dependency property

        public static readonly DependencyProperty ProgressValueProperty
            = DependencyProperty.Register(
                nameof(ProgressValue),
                typeof(double?),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));


        public double? ProgressValue
        {
            get => (double?)this.GetValue(ProgressValueProperty);
            set => this.SetValue(ProgressValueProperty, value);
        }

        #endregion

        #region Overlay Dependency property

        public static readonly DependencyProperty OverlayProperty
            = DependencyProperty.Register(
                nameof(Overlay),
                typeof(ImageSource),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));

        public ImageSource Overlay
        {
            get => (ImageSource)this.GetValue(OverlayProperty);
            set => this.SetValue(OverlayProperty, value);
        }

        #endregion

        #region Description Dependency property

        public static readonly DependencyProperty DescriptionProperty
            = DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));

        public string Description
        {
            get => (string)this.GetValue(DescriptionProperty);
            set => this.SetValue(DescriptionProperty, value);
        }

        #endregion

        #region ThumbnailClipMargin Dependency property

        public static readonly DependencyProperty ThumbnailClipMarginProperty
            = DependencyProperty.Register(
                nameof(ThumbnailClipMargin),
                typeof(Thickness?),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));

        public Thickness? ThumbnailClipMargin
        {
            get => (Thickness?)this.GetValue(ThumbnailClipMarginProperty);
            set => this.SetValue(ThumbnailClipMarginProperty, value);
        }

        #endregion

        #region ThumbButtonInfos Dependency property

        public static readonly DependencyProperty ThumbButtonInfosProperty
            = DependencyProperty.Register(
                nameof(ThumbButtonInfos),
                typeof(ThumbButtonInfoCollection),
                typeof(TaskbarMessage),
                new UIPropertyMetadata(null));

        public ThumbButtonInfoCollection ThumbButtonInfos
        {
            get => (ThumbButtonInfoCollection)this.GetValue(ThumbButtonInfosProperty);
            set => this.SetValue(ThumbButtonInfosProperty, value);
        }

        #endregion

        public TaskbarMessage()
        {
        }

        public TaskbarMessage(string messageKey)
            : base(messageKey)
        {
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TaskbarMessage(this.MessageKey)
            {
                ProgressState = this.ProgressState,
                ProgressValue = this.ProgressValue,
                Overlay = this.Overlay,
                Description = this.Description,
                ThumbnailClipMargin = this.ThumbnailClipMargin,
                ThumbButtonInfos = this.ThumbButtonInfos,
            };
        }
    }
}
