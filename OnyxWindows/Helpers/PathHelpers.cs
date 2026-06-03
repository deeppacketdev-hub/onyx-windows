using System;
using System.IO;

namespace OnyxWindows.Helpers;

/// <summary>
/// Path utilities for Windows — classpath separator, path normalization, etc.
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// Classpath separator for Windows (semicolon).
    /// macOS uses colon (:), Windows uses semicolon (;).
    /// </summary>
    public const char ClasspathSeparator = ';';

    /// <summary>
    /// Joins classpath entries with the Windows separator.
    /// </summary>
    public static string JoinClasspath(IEnumerable<string> paths)
    {
        return string.Join(ClasspathSeparator, paths);
    }

    /// <summary>
    /// Normalizes a path to use the OS-native separator.
    /// </summary>
    public static string NormalizePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Ensures a directory exists, creating it (and parents) if needed.
    /// </summary>
    public static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Gets a safe directory name from an instance name.
    /// </summary>
    public static string SafeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return sanitized.ToLowerInvariant().Trim('-');
    }

    /// <summary>
    /// Calculates the total size of a directory recursively.
    /// </summary>
    public static long DirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;

        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { size += new FileInfo(file).Length; }
                catch { /* skip inaccessible files */ }
            }
        }
        catch { /* skip inaccessible directories */ }
        return size;
    }

    /// <summary>
    /// Formats a byte count as a human-readable string (KB, MB, GB).
    /// </summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
