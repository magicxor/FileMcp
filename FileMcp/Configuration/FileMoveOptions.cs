namespace FileMcp.Configuration;

/// <summary>
/// Strongly-typed settings for the file-move MCP server, bound from the
/// <c>FileMove</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class FileMoveOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "FileMove";

    /// <summary>
    /// Regex patterns a source directory must match to be allowed.
    /// A directory is allowed when it matches <em>any</em> pattern.
    /// An empty list means "deny all" (nothing is allowed as a source).
    /// </summary>
    public List<string> AllowedSourcePatterns { get; set; } = new();

    /// <summary>
    /// Regex patterns a target directory must match to be allowed.
    /// A directory is allowed when it matches <em>any</em> pattern.
    /// An empty list means "deny all" (nothing is allowed as a target).
    /// </summary>
    public List<string> AllowedTargetPatterns { get; set; } = new();

    /// <summary>
    /// Maximum age of the source file, in seconds. Files whose last-write time is older
    /// than this are refused. Defaults to 600 seconds (10 minutes).
    /// </summary>
    public int MaxFileAgeSeconds { get; set; } = 600;
}
