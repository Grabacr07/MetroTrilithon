namespace Amethystra.UI.Interactivity;

public enum NotificationSeverity
{
    Info,
    Success,
    Caution,
    Danger,
}

public readonly record struct Notification(
    string Title,
    string Message,
    NotificationSeverity Severity = NotificationSeverity.Info);
