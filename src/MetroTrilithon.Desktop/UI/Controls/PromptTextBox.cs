using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MetroTrilithon.UI.Controls;

[TemplateVisualState(Name = "Empty", GroupName = "TextStates")]
[TemplateVisualState(Name = "NotEmpty", GroupName = "TextStates")]
public class PromptTextBox : System.Windows.Controls.TextBox
{
    static PromptTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PromptTextBox), new FrameworkPropertyMetadata(typeof(PromptTextBox)));
    }

    public PromptTextBox()
    {
        this.UpdateTextStates(true);
        this.TextChanged += (_, _) => this.UpdateTextStates(true);
        this.GotKeyboardFocus += (_, _) => this.UpdateTextStates(true);
    }

    #region Prompt dependency property

    public static readonly DependencyProperty PromptProperty
        = DependencyProperty.Register(
            nameof(Prompt),
            typeof(string),
            typeof(PromptTextBox),
            new UIPropertyMetadata(""));

    public string Prompt
    {
        get => (string)this.GetValue(PromptProperty);
        set => this.SetValue(PromptProperty, value);
    }

    #endregion

    #region PromptBrush dependency property

    public static readonly DependencyProperty PromptBrushProperty
        = DependencyProperty.Register(
            nameof(PromptBrush),
            typeof(Brush),
            typeof(PromptTextBox),
            new UIPropertyMetadata(Brushes.Gray));

    public Brush PromptBrush
    {
        get => (Brush)this.GetValue(PromptBrushProperty);
        set => this.SetValue(PromptBrushProperty, value);
    }

    #endregion


    private void UpdateTextStates(bool useTransitions)
    {
        VisualStateManager.GoToState(this, string.IsNullOrEmpty(this.Text) ? "Empty" : "NotEmpty", useTransitions);
    }
}
