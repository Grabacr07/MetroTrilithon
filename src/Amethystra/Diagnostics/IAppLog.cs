using System;

namespace Amethystra.Diagnostics;

public interface IAppLog
{
    AppLog.Logger For<T>();

    AppLog.Logger For(Type type);
}
