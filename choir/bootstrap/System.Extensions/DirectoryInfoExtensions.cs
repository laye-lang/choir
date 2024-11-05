namespace System.IO;

public static class DirectoryInfoExtensions
{
    public static DirectoryInfo ChildDirectory(this DirectoryInfo di, string childPath)
    {
        return new DirectoryInfo(Path.Combine(di.FullName, childPath));
    }

    public static FileInfo ChildFile(this DirectoryInfo di, string childPath)
    {
        return new FileInfo(Path.Combine(di.FullName, childPath));
    }
}
