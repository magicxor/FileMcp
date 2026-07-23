using System.ComponentModel;
using FileMcp.Configuration;
using FileMcp.Validation;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FileMcp.Tools;

/// <summary>
/// The single tool exposed by this MCP server: move one file between two directories,
/// keeping its name, subject to path validation, an allow-list policy, and a max-age check.
/// </summary>
[McpServerToolType]
public sealed class FileMoveTools
{
    private readonly PathPolicy _policy;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileMoveTools> _logger;

    public FileMoveTools(PathPolicy policy, TimeProvider timeProvider, ILogger<FileMoveTools> logger)
    {
        _policy = policy;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [McpServerTool(Name = "move_file")]
    [Description(
        "Moves a single file from a source directory to a target directory, keeping the same " +
        "file name. Both directories must be absolute (rooted) paths that contain no '.' or " +
        "'..' segments and no invalid characters. The target directory is created if it does " +
        "not already exist. Fails with a clear message if the paths are not allowed by policy, " +
        "if the source file does not exist, if it is older than the configured maximum age, or " +
        "if a file with the same name already exists in the target directory.")]
    public string MoveFile(
        [Description("Absolute path to the source directory. Must be rooted, contain no '.'/'..' segments, and no invalid characters.")]
        string sourceDirectory,
        [Description("Absolute path to the target directory. Must be rooted, contain no '.'/'..' segments, and no invalid characters. Created if it does not exist.")]
        string targetDirectory,
        [Description("File name including extension, for example 'report.pdf'. Must not contain any path separator.")]
        string fileName)
    {
        try
        {
            return Move(sourceDirectory, targetDirectory, fileName);
        }
        catch (FileMoveException ex)
        {
            // Expected, user-facing failure: surface the clear message to the MCP client.
            _logger.LogWarning("move_file rejected: {Message}", ex.Message);
            throw new McpException(ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Unexpected IO failure during the move itself.
            _logger.LogError(ex, "move_file failed while moving the file.");
            throw new McpException($"Failed to move file '{fileName}': {ex.Message}");
        }
    }

    private string Move(string sourceDirectory, string targetDirectory, string fileName)
    {
        // 1. Validate the raw arguments.
        PathValidator.ValidateFileName(fileName);
        PathValidator.ValidateDirectory(sourceDirectory, nameof(sourceDirectory));
        PathValidator.ValidateDirectory(targetDirectory, nameof(targetDirectory));

        // 2. Enforce the configured allow-list policy.
        if (!_policy.IsSourceAllowed(sourceDirectory))
        {
            throw new FileMoveException(BuildPolicyMessage(
                nameof(sourceDirectory), sourceDirectory, "source", _policy.HasSourcePatterns));
        }

        if (!_policy.IsTargetAllowed(targetDirectory))
        {
            throw new FileMoveException(BuildPolicyMessage(
                nameof(targetDirectory), targetDirectory, "target", _policy.HasTargetPatterns));
        }

        // 3. The source file must exist.
        var sourceFile = Path.Combine(sourceDirectory, fileName);
        if (!File.Exists(sourceFile))
        {
            throw new FileMoveException($"Source file '{sourceFile}' does not exist.");
        }

        // 4. The source file must not be older than the configured maximum age.
        var lastWriteUtc = File.GetLastWriteTimeUtc(sourceFile);
        var age = _timeProvider.GetUtcNow() - new DateTimeOffset(lastWriteUtc, TimeSpan.Zero);
        if (age > _policy.MaxFileAge)
        {
            throw new FileMoveException(
                $"File '{fileName}' is too old to move: its age is " +
                $"{(long)age.TotalSeconds}s, which exceeds the maximum of " +
                $"{(long)_policy.MaxFileAge.TotalSeconds}s.");
        }

        // 5. Create the target directory if needed.
        Directory.CreateDirectory(targetDirectory);

        // 6. Never overwrite an existing file in the target directory.
        var targetFile = Path.Combine(targetDirectory, fileName);
        if (File.Exists(targetFile))
        {
            throw new FileMoveException(
                $"A file named '{fileName}' already exists in the target directory '{targetDirectory}'.");
        }

        // 7. Move it (keeping the name).
        File.Move(sourceFile, targetFile);
        _logger.LogInformation("Moved '{Source}' to '{Target}'.", sourceFile, targetFile);

        return $"Moved '{fileName}' to '{targetFile}'.";
    }

    private static string BuildPolicyMessage(string argName, string directory, string kind, bool hasPatterns)
    {
        return hasPatterns
            ? $"{argName} '{directory}' is not allowed: it does not match any configured {kind} pattern."
            : $"{argName} '{directory}' is not allowed: no {kind} patterns are configured, so every " +
              $"{kind} directory is blocked. Configure FileMove:Allowed{(kind == "source" ? "Source" : "Target")}Patterns.";
    }
}
