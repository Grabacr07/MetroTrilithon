using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Livet.Messaging;

namespace MetroTrilithon.UI.Interactivity
{
    public class FolderBrowserDialogMessage : ResponsiveInteractionMessage<string?>
    {
        #region AutoUpgradeEnabled dependency property

        public static readonly DependencyProperty AutoUpgradeEnabledProperty
            = DependencyProperty.Register(
                nameof(AutoUpgradeEnabled),
                typeof(bool),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool AutoUpgradeEnabled
        {
            get => (bool)this.GetValue(AutoUpgradeEnabledProperty);
            set => this.SetValue(AutoUpgradeEnabledProperty, BooleanBoxes.Box(value));
        }

        #endregion

        #region Description dependency property

        public static readonly DependencyProperty DescriptionProperty
            = DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(default(string)));

        public string Description
        {
            get => (string)this.GetValue(DescriptionProperty);
            set => this.SetValue(DescriptionProperty, value);
        }

        #endregion

        #region RootFolder dependency property

        public static readonly DependencyProperty RootFolderProperty
            = DependencyProperty.Register(
                nameof(RootFolder),
                typeof(Environment.SpecialFolder),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(Environment.SpecialFolder.Desktop));

        public Environment.SpecialFolder RootFolder
        {
            get => (Environment.SpecialFolder)this.GetValue(RootFolderProperty);
            set => this.SetValue(RootFolderProperty, value);
        }

        #endregion

        #region SelectedPath dependency property

        public static readonly DependencyProperty SelectedPathProperty
            = DependencyProperty.Register(
                nameof(SelectedPath),
                typeof(string),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(""));

        public string SelectedPath
        {
            get => (string)this.GetValue(SelectedPathProperty);
            set => this.SetValue(SelectedPathProperty, value);
        }

        #endregion

        #region ShowNewFolderButton dependency property

        public static readonly DependencyProperty ShowNewFolderButtonProperty
            = DependencyProperty.Register(
                nameof(ShowNewFolderButton),
                typeof(bool),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool ShowNewFolderButton
        {
            get => (bool)this.GetValue(ShowNewFolderButtonProperty);
            set => this.SetValue(ShowNewFolderButtonProperty, BooleanBoxes.Box(value));
        }

        #endregion

        #region UseDescriptionForTitle dependency property

        public static readonly DependencyProperty UseDescriptionForTitleProperty
            = DependencyProperty.Register(
                nameof(UseDescriptionForTitle),
                typeof(bool),
                typeof(FolderBrowserDialogMessage),
                new PropertyMetadata(BooleanBoxes.FalseBox));

        public bool UseDescriptionForTitle
        {
            get => (bool)this.GetValue(UseDescriptionForTitleProperty);
            set => this.SetValue(UseDescriptionForTitleProperty, BooleanBoxes.Box(value));
        }

        #endregion

        public FolderBrowserDialogMessage()
        {
        }

        public FolderBrowserDialogMessage(string messageKey)
            : base(messageKey)
        {
        }

        protected override Freezable CreateInstanceCore()
            => new FolderBrowserDialogMessage(this.MessageKey)
            {
                AutoUpgradeEnabled = this.AutoUpgradeEnabled,
                Description = this.Description,
                RootFolder = this.RootFolder,
                SelectedPath = this.SelectedPath,
                ShowNewFolderButton = this.ShowNewFolderButton,
                UseDescriptionForTitle = this.UseDescriptionForTitle,
            };
    }
}
