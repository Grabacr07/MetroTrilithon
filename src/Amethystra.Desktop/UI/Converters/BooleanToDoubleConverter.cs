namespace Amethystra.UI.Converters;

public class BooleanToDoubleConverter(double trueValue, double falseValue) : BooleanConverterBase<double>(trueValue, falseValue)
{
    public BooleanToDoubleConverter()
        : this(1.0, 0.0)
    {
    }
}
