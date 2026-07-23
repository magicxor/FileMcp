namespace FileMcp.Validation;

/// <summary>
/// Pure (no-IO) validation helpers for the arguments accepted by the move tool.
/// Every failure throws a <see cref="FileMoveException"/> whose message explains, in plain
/// terms, exactly what is wrong so the calling agent can correct the request.
/// </summary>
public static class PathValidator
{
    private static readonly char[] Separators =
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };

    /// <summary>
    /// Validates a bare file name: non-empty, not "." or "..", and free of characters that
    /// are invalid in a file name (which also rules out directory separators, so no path
    /// component can be smuggled in through this argument).
    /// </summary>
    public static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new FileMoveException("fileName must not be empty.");
        }

        if (fileName is "." or "..")
        {
            throw new FileMoveException("fileName must be a real file name, not '.' or '..'.");
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new FileMoveException(
                $"fileName '{fileName}' contains invalid characters or a path separator; " +
                "it must be a single file name including its extension (for example 'report.pdf').");
        }

        // Defense in depth: reject anything that is not exactly its own file name.
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            throw new FileMoveException(
                $"fileName '{fileName}' must not contain any directory component.");
        }
    }

    /// <summary>
    /// Validates a directory argument: must be an absolute (rooted, fully-qualified) path,
    /// contain no invalid characters, and contain no "." or ".." segment. The raw string is
    /// validated as-is; it is intentionally never normalized with <see cref="Path.GetFullPath"/>,
    /// because normalization would silently collapse ".." instead of rejecting it.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="argName">Argument name used in error messages (e.g. "sourceDirectory").</param>
    public static void ValidateDirectory(string path, string argName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileMoveException($"{argName} must not be empty.");
        }

        if (!Path.IsPathFullyQualified(path))
        {
            throw new FileMoveException(
                $"{argName} must be an absolute, rooted path (for example 'C:\\data\\out'). " +
                $"'{path}' is not fully qualified.");
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new FileMoveException($"{argName} '{path}' contains invalid path characters.");
        }

        // Split off the root (e.g. "C:\" or "\\server\share\") and inspect the remaining
        // segments only; the root legitimately contains ':' which is not a valid file-name char.
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var remainder = path[root.Length..];
        var segments = remainder.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        var invalidNameChars = Path.GetInvalidFileNameChars();
        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new FileMoveException(
                    $"{argName} '{path}' must not contain '.' or '..' path segments.");
            }

            if (segment.IndexOfAny(invalidNameChars) >= 0)
            {
                throw new FileMoveException(
                    $"{argName} '{path}' contains an invalid character in segment '{segment}'.");
            }
        }
    }
}
