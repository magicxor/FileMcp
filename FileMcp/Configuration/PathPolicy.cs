using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FileMcp.Configuration;

/// <summary>
/// Compiled, ready-to-use view of <see cref="FileMoveOptions"/>. The allow-list regexes are
/// compiled once (at construction) rather than on every tool call. Registered as a singleton.
/// </summary>
public sealed class PathPolicy
{
    private readonly Regex[] _sourcePatterns;
    private readonly Regex[] _targetPatterns;

    // Windows paths are case-insensitive, so the allow-list matches case-insensitively too.
    private const RegexOptions Options =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    public PathPolicy(IOptions<FileMoveOptions> options)
        : this(options.Value)
    {
    }

    public PathPolicy(FileMoveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _sourcePatterns = Compile(options.AllowedSourcePatterns);
        _targetPatterns = Compile(options.AllowedTargetPatterns);
        MaxFileAge = TimeSpan.FromSeconds(options.MaxFileAgeSeconds);
    }

    /// <summary>Maximum permitted age of a source file.</summary>
    public TimeSpan MaxFileAge { get; }

    /// <summary>True when at least one source pattern is configured.</summary>
    public bool HasSourcePatterns => _sourcePatterns.Length > 0;

    /// <summary>True when at least one target pattern is configured.</summary>
    public bool HasTargetPatterns => _targetPatterns.Length > 0;

    /// <summary>
    /// Returns true when <paramref name="directory"/> matches any configured source pattern.
    /// An empty pattern list always returns false (deny-all).
    /// </summary>
    public bool IsSourceAllowed(string directory) => IsAllowed(_sourcePatterns, directory);

    /// <summary>
    /// Returns true when <paramref name="directory"/> matches any configured target pattern.
    /// An empty pattern list always returns false (deny-all).
    /// </summary>
    public bool IsTargetAllowed(string directory) => IsAllowed(_targetPatterns, directory);

    private static bool IsAllowed(Regex[] patterns, string directory)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.IsMatch(directory))
            {
                return true;
            }
        }

        return false;
    }

    private static Regex[] Compile(IReadOnlyList<string> patterns)
    {
        var compiled = new Regex[patterns.Count];
        for (var i = 0; i < patterns.Count; i++)
        {
            // Throws ArgumentException / RegexParseException for an invalid pattern; surfaced
            // at startup through options validation so misconfiguration fails fast and loudly.
            compiled[i] = new Regex(patterns[i], Options);
        }

        return compiled;
    }
}
