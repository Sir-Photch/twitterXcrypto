using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.util;

internal static class IOExtensions
{
    internal static bool IsEmpty(this DirectoryInfo dir) 
        => !dir.GetFileSystemInfos().Any();

    internal static bool ContainsFile(this DirectoryInfo dir, string fileName, bool recursive = false) 
        => dir.EnumerateFiles(fileName, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Any();
}
