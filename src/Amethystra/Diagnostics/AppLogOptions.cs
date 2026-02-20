using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Amethystra.Properties;
using Mio;

namespace Amethystra.Diagnostics;

public record struct AppLogOptions(
    FilePath LogFilePath,
    Encoding Encoding,
    long MaxLogBytes = 10L * 1024L * 1024L,
    int MaxGenerations = 5,
    int QueueCapacity = 2048,
    int BestEffortTimeoutMsForDispose = 1000,
    bool Warmup = true)
{
    public AppLogOptions(IAssemblyInfo assemblyInfo)
        : this(CreatePath(assemblyInfo), UTF8NoBOM)
    {
    }

    public static Encoding UTF8NoBOM
        => new UTF8Encoding(false);

    public static FilePath CreatePath(IAssemblyInfo info)
        => new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .ChildDirectory(info.Company)
            .ChildDirectory(info.Product)
            .EnsureCreated()
            .ChildFile($"{Assembly.GetEntryAssembly()?.GetName().Name}.log");
}
