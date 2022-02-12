using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace MetroTrilithon.UI.Interactivity
{
    public class ScrollBarThresholdBehavior : Behavior<ScrollViewer>
    {
        private ScrollBarVisibility? _hsbvBackup;
        private ScrollBarVisibility? _vsbvBackup;
        private double? _maxwBackup;
        private double? _maxhBackup;

        #region Horizontal dependency property

        public static readonly DependencyProperty HorizontalProperty
            = DependencyProperty.Register(
                nameof(Horizontal),
                typeof(double),
                typeof(ScrollBarThresholdBehavior),
                new PropertyMetadata(.0));

        public double Horizontal
        {
            get => (double)this.GetValue(HorizontalProperty);
            set => this.SetValue(HorizontalProperty, value);
        }

        #endregion

        #region Vertical dependency property

        public static readonly DependencyProperty VerticalProperty
            = DependencyProperty.Register(
                nameof(Vertical),
                typeof(double),
                typeof(ScrollBarThresholdBehavior),
                new PropertyMetadata(.0));

        public double Vertical
        {
            get => (double)this.GetValue(VerticalProperty);
            set => this.SetValue(VerticalProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SizeChanged += this.HandleSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.SizeChanged -= this.HandleSizeChanged;
        }

        private void HandleSizeChanged(object sender, SizeChangedEventArgs args)
        {
            // ScrollViewer.Content に MaxWidth が設定されていたらガン無視するようになってしまうのでアレ

            if (this.Horizontal > .0)
            {
                if (args.NewSize.Width < this.Horizontal)
                {
                    this._hsbvBackup ??= this.AssociatedObject.HorizontalScrollBarVisibility;
                    this._maxwBackup ??= ((FrameworkElement)this.AssociatedObject.Content).MaxWidth;

                    this.AssociatedObject.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    ((FrameworkElement)this.AssociatedObject.Content).MaxWidth = this.Horizontal;
                }
                else
                {
                    if (this._hsbvBackup != null) this.AssociatedObject.HorizontalScrollBarVisibility = this._hsbvBackup.Value;
                    if (this._maxwBackup != null) ((FrameworkElement)this.AssociatedObject.Content).MaxWidth = this._maxwBackup.Value;
                }
            }

            if (this.Vertical > .0)
            {
                if (args.NewSize.Height < this.Vertical)
                {
                    this._vsbvBackup ??= this.AssociatedObject.VerticalScrollBarVisibility;
                    this._maxhBackup ??= ((FrameworkElement)this.AssociatedObject.Content).MaxHeight;

                    this.AssociatedObject.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    ((FrameworkElement)this.AssociatedObject.Content).MaxHeight = this.Vertical;
                }
                else
                {
                    if (this._vsbvBackup != null) this.AssociatedObject.VerticalScrollBarVisibility = this._vsbvBackup.Value;
                    if (this._maxhBackup != null) ((FrameworkElement)this.AssociatedObject.Content).MaxHeight = this._maxhBackup.Value;
                }
            }
        }
    }
}
