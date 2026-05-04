namespace Amethystra.UI.Converters;

public class BooleanToDoubleConverter(double trueValue = 1.0, double falseValue = 0.0)
    : BooleanConverterBase<double>(trueValue, falseValue);
