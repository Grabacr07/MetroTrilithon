namespace MetroTrilithon.UI;

public static class BooleanBoxes
{
    public static object TrueBox = true;
    public static object FalseBox = false;

    public static object Box(bool value)
        => value
            ? TrueBox
            : FalseBox;
}
