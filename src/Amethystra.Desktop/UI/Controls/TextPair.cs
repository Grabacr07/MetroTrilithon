using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Brush = System.Windows.Media.Brush;

namespace Amethystra.UI.Controls;

public class TextPair : System.Windows.Controls.Control
{
    static TextPair()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TextPair),
            new FrameworkPropertyMetadata(typeof(TextPair)));
    }

    #region Text dependency property

    public static readonly DependencyProperty TextProperty
        = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TextPair),
            new PropertyMetadata(""));

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    #endregion

    #region SubText dependency property

    public static readonly DependencyProperty SubTextProperty
        = DependencyProperty.Register(
            nameof(SubText),
            typeof(string),
            typeof(TextPair),
            new PropertyMetadata(""));

    public string SubText
    {
        get => (string)this.GetValue(SubTextProperty);
        set => this.SetValue(SubTextProperty, value);
    }

    #endregion

    #region SubTextForeground dependency property

    public static readonly DependencyProperty SubTextForegroundProperty
        = DependencyProperty.Register(
            nameof(SubTextForeground),
            typeof(Brush),
            typeof(TextPair),
            new PropertyMetadata(default(Brush)));

    public Brush SubTextForeground
    {
        get => (Brush)this.GetValue(SubTextForegroundProperty);
        set => this.SetValue(SubTextForegroundProperty, value);
    }

    #endregion

    #region SubTextSize dependency property

    public static readonly DependencyProperty SubTextSizeProperty
        = DependencyProperty.Register(
            nameof(SubTextSize),
            typeof(double),
            typeof(TextPair),
            new PropertyMetadata(.0));

    public double SubTextSize
    {
        get => (double)this.GetValue(SubTextSizeProperty);
        set => this.SetValue(SubTextSizeProperty, value);
    }

    #endregion

    #region SubTextWeight dependency property

    public static readonly DependencyProperty SubTextWeightProperty
        = DependencyProperty.Register(
            nameof(SubTextWeight),
            typeof(FontWeight),
            typeof(TextPair),
            new PropertyMetadata(default(FontWeight)));

    public FontWeight SubTextWeight
    {
        get => (FontWeight)this.GetValue(SubTextWeightProperty);
        set => this.SetValue(SubTextWeightProperty, value);
    }

    #endregion
}
