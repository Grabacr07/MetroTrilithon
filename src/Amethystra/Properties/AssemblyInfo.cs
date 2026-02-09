using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Amethystra.Test")]

namespace Amethystra.Properties;

public interface IAssemblyInfo
{
    string Title { get; }

    string Description { get; }

    string Company { get; }

    string Product { get; }

    string Copyright { get; }

    string Trademark { get; }

    Version Version { get; }

    string VersionString { get; }

    string InformationalVersion { get; }
}
