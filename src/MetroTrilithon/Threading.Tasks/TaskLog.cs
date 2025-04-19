using System;
using System.Diagnostics;

namespace MetroTrilithon.Threading.Tasks;

public class TaskLog(string callerMemberName, string callerFilePath, int callerLineNumber, Exception exception)
{
    public string CallerMemberName { get; } = callerMemberName;

    public string CallerFilePath { get; } = callerFilePath;

    public int CallerLineNumber { get; } = callerLineNumber;

    public Exception Exception { get; } = exception;

    public static EventHandler<TaskLog> Occurred = (_, e) =>
    {
        const string format = @"Unhandled Exception occurred from Task.Forget()
-----------
Caller file  : {1}
             : line {2}
Caller member: {0}
Exception: {3}

";
        Debug.WriteLine(format, e.CallerMemberName, e.CallerFilePath, e.CallerLineNumber, e.Exception);
    };

    internal static void Raise(TaskLog log)
    {
        Occurred.Invoke(typeof(TaskLog), log);
    }
}
