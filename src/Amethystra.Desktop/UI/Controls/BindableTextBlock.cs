using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace Amethystra.UI.Controls;

[ContentProperty(nameof(TextTemplates))]
public class BindableTextBlock : TextBlock
{
    #region TextTemplates dependency property

    public static readonly DependencyProperty TextTemplatesProperty
        = DependencyProperty.Register(
            nameof(TextTemplates),
            typeof(DataTemplateCollection),
            typeof(BindableTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnNeedUpdate));

    public DataTemplateCollection TextTemplates
    {
        get => (DataTemplateCollection)this.GetValue(TextTemplatesProperty);
        set => this.SetValue(TextTemplatesProperty, value);
    }

    #endregion

    #region TextSource dependency property

    public static readonly DependencyProperty TextSourceProperty
        = DependencyProperty.Register(
            nameof(TextSource),
            typeof(IEnumerable<object>),
            typeof(BindableTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnNeedUpdate));

    private static void OnNeedUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var instance = d as BindableTextBlock;
        instance?.Update();
    }

    public IEnumerable<object>? TextSource
    {
        get => (IEnumerable<object>)this.GetValue(TextSourceProperty);
        set => this.SetValue(TextSourceProperty, value);
    }

    #endregion

    public BindableTextBlock()
    {
        this.TextTemplates = new DataTemplateCollection();
        this.Loaded += (_, _) => this.Update();
    }

    private IEnumerable<InlineHolder> CreateTemplateInstance(IEnumerable<object> textSourcePart)
    {
        foreach (var bindable in textSourcePart)
        {
            InlineHolder result;

            var template = this.TextTemplates.FirstOrDefault(dt => dt.DataType is Type type && type == bindable.GetType());
            if (template == null)
            {
                result = new InlineHolder { Inlines = new InlineSimpleCollection(new Inline[] { new Run(bindable.ToString()) }) };
            }
            else
            {
                result = (InlineHolder)template.LoadContent();
                result.DataContext = bindable;
                foreach (var inline in result.Inlines)
                {
                    inline.DataContext = bindable;
                }
            }

            yield return result;
        }
    }

    private void Update()
    {
        this.Inlines.Clear();

        if (this.TextSource == null) return;

        foreach (var inline in this.CreateTemplateInstance(this.TextSource).SelectMany(inlineHolder => inlineHolder.Inlines))
        {
            this.Inlines.Add(inline);
        }
    }
}

[ContentProperty(nameof(Inlines))]
public class InlineHolder : FrameworkElement
{
    public InlineSimpleCollection Inlines { get; set; } = new();
}

public class InlineSimpleCollection : List<Inline>
{
    public InlineSimpleCollection() { }

    public InlineSimpleCollection(IEnumerable<Inline> source)
    {
        this.AddRange(source);
    }
}
