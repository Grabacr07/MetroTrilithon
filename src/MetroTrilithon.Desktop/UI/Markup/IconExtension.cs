using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using MetroTrilithon.Properties;

namespace MetroTrilithon.UI.Markup;

[MarkupExtensionReturnType(typeof(BitmapFrame))]
public class IconExtension : MarkupExtension
{
    public string Source { get; set; }

    public int Size { get; set; }

    public IconExtension(string source, int size)
    {
        this.Source = source;
        this.Size = size;
    }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (Uri.TryCreate(Definitions.PackageUriAuthority + this.Source, UriKind.Absolute, out var uri))
        {
            var decoder = BitmapDecoder.Create(
                uri,
                BitmapCreateOptions.DelayCreation,
                BitmapCacheOption.OnDemand);

            return decoder.Frames
                    .SingleOrDefault(f => (int)f.Width == this.Size)
                ?? decoder.Frames
                    .OrderBy(f => f.Width)
                    .First();
        }

        return null;
    }
}
