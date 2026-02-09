using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace Amethystra.UI.Controls;

public class BindableRichTextBox : RichTextBox
{
    static BindableRichTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BindableRichTextBox), new FrameworkPropertyMetadata(typeof(BindableRichTextBox)));
    }

    #region TextTemplates dependency property

    public static readonly DependencyProperty TextTemplatesProperty
        = DependencyProperty.Register(
            nameof(TextTemplates),
            typeof(DataTemplateCollection),
            typeof(BindableRichTextBox),
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
            typeof(BindableRichTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnNeedUpdate));

    private static void OnNeedUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var instance = d as BindableRichTextBox;
        instance?.Update();
    }

    public IEnumerable<object>? TextSource
    {
        get => (IEnumerable<object>)this.GetValue(TextSourceProperty);
        set => this.SetValue(TextSourceProperty, value);
    }

    #endregion


    public BindableRichTextBox()
    {
        this.TextTemplates = [];
        this.Loaded += (_, _) => this.Update();
    }

    private IEnumerable<BlockHolder> CreateTemplateInstance(IEnumerable<object> textSourcePart)
    {
        return textSourcePart.Select(
            obj =>
            {
                BlockHolder result;

                var template = this.TextTemplates.FirstOrDefault(dt => dt.DataType is Type type && (type == obj.GetType()));
                if (template == null)
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run(obj.ToString()));
                    result = new BlockHolder { Blocks = new BlockSimpleCollection([paragraph]) };
                }
                else
                {
                    result = (BlockHolder)template.LoadContent();
                    result.DataContext = obj;
                    foreach (var block in result.Blocks)
                    {
                        block.DataContext = obj;
                    }
                }

                return result;
            });
    }

    private void Update()
    {
        this.Document.Blocks.Clear();

        if (this.TextSource == null) return;

        foreach (var block in this.CreateTemplateInstance(this.TextSource).SelectMany(static holder => holder.Blocks))
        {
            this.Document.Blocks.Add(block);
        }
    }
}

[ContentProperty(nameof(Blocks))]
public class BlockHolder : FrameworkElement
{
    public BlockSimpleCollection Blocks { get; set; } = [];
}

public class DataTemplateCollection : List<DataTemplate>
{
    public DataTemplateCollection() { }

    public DataTemplateCollection(IEnumerable<DataTemplate> source)
    {
        this.AddRange(source);
    }
}

public class BlockSimpleCollection : List<Block>
{
    public BlockSimpleCollection() { }

    public BlockSimpleCollection(IEnumerable<Block> source)
    {
        this.AddRange(source);
    }
}
