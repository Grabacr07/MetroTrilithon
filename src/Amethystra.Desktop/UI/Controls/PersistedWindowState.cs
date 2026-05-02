namespace Amethystra.UI.Controls;

public sealed record PersistedWindowState(
    double Left,
    double Top,
    double Width,
    double Height,
    bool IsMaximized,
    bool IsTopmost);
