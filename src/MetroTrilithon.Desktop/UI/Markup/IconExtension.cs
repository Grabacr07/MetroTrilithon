using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace MetroTrilithon.UI.Markup;

public class IconExtension : MarkupExtension
{
    private const string _packPrefix = "pack://application:,,,";
    private string _source = "";

    public string Source
    {
        get => this._source;
        set => this._source = value.StartsWith(_packPrefix, StringComparison.Ordinal) ? value : _packPrefix + value;
    }

    public int Size { get; set; }

    public IconExtension()
    {
    }

    public IconExtension(string source, int size)
    {
        this.Source = source;
        this.Size = size;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var decoder = BitmapDecoder.Create(
            new Uri(this.Source),
            BitmapCreateOptions.DelayCreation,
            BitmapCacheOption.OnDemand);

        return decoder.Frames
                .SingleOrDefault(f => (int)f.Width == this.Size)
            ?? decoder.Frames
                .OrderBy(f => f.Width)
                .First();
    }
}
