using FileMcp.Configuration;
using FileMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Xunit;

namespace FileMcp.Tests;

public sealed class FileMoveToolsTests : IDisposable
{
    private readonly string _root;
    private readonly string _source;
    private readonly string _target;
    private readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public FileMoveToolsTests()
    {
        // A unique temp workspace per test instance; allow-list patterns match anything under it.
        _root = Path.Combine(Path.GetTempPath(), "FileMcpTests", Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_root, "in");
        _target = Path.Combine(_root, "out");
        Directory.CreateDirectory(_source);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private FileMoveTools CreateTool(int maxAgeSeconds = 600)
    {
        var policy = new PathPolicy(new FileMoveOptions
        {
            AllowedSourcePatterns = { ".*" },
            AllowedTargetPatterns = { ".*" },
            MaxFileAgeSeconds = maxAgeSeconds,
        });

        return new FileMoveTools(policy, new FixedTimeProvider(_now), NullLogger<FileMoveTools>.Instance);
    }

    private FileMoveTools CreateTool(string sourcePattern, string targetPattern)
    {
        var policy = new PathPolicy(new FileMoveOptions
        {
            AllowedSourcePatterns = { sourcePattern },
            AllowedTargetPatterns = { targetPattern },
            MaxFileAgeSeconds = 600,
        });

        return new FileMoveTools(policy, new FixedTimeProvider(_now), NullLogger<FileMoveTools>.Instance);
    }

    private string WriteSourceFile(string name, TimeSpan age)
    {
        var path = Path.Combine(_source, name);
        File.WriteAllText(path, "content");
        File.SetLastWriteTimeUtc(path, (_now - age).UtcDateTime);
        return path;
    }

    [Fact]
    public void MoveFile_HappyPath_CreatesTargetAndMovesFile()
    {
        var sourceFile = WriteSourceFile("report.pdf", TimeSpan.FromSeconds(10));
        var tool = CreateTool();

        var result = tool.MoveFile(_source, _target, "report.pdf");

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(Path.Combine(_target, "report.pdf")));
        Assert.True(Directory.Exists(_target)); // target created on demand
        Assert.Contains("report.pdf", result);
    }

    [Fact]
    public void MoveFile_FileTooOld_IsRejectedAndLeavesSource()
    {
        var sourceFile = WriteSourceFile("old.txt", TimeSpan.FromSeconds(700));
        var tool = CreateTool(maxAgeSeconds: 600);

        var ex = Assert.Throws<McpException>(() => tool.MoveFile(_source, _target, "old.txt"));

        Assert.Contains("too old", ex.Message);
        Assert.True(File.Exists(sourceFile)); // not moved
    }

    [Fact]
    public void MoveFile_AtExactMaxAge_IsAllowed()
    {
        WriteSourceFile("edge.txt", TimeSpan.FromSeconds(600));
        var tool = CreateTool(maxAgeSeconds: 600);

        tool.MoveFile(_source, _target, "edge.txt"); // age == max => allowed (not older than)

        Assert.True(File.Exists(Path.Combine(_target, "edge.txt")));
    }

    [Fact]
    public void MoveFile_TargetAlreadyHasSameName_IsRejected()
    {
        var sourceFile = WriteSourceFile("dup.txt", TimeSpan.FromSeconds(10));
        Directory.CreateDirectory(_target);
        File.WriteAllText(Path.Combine(_target, "dup.txt"), "existing");
        var tool = CreateTool();

        var ex = Assert.Throws<McpException>(() => tool.MoveFile(_source, _target, "dup.txt"));

        Assert.Contains("already exists", ex.Message);
        Assert.True(File.Exists(sourceFile)); // not moved
        Assert.Equal("existing", File.ReadAllText(Path.Combine(_target, "dup.txt"))); // not overwritten
    }

    [Fact]
    public void MoveFile_SourceMissing_IsRejected()
    {
        var tool = CreateTool();

        var ex = Assert.Throws<McpException>(() => tool.MoveFile(_source, _target, "missing.txt"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void MoveFile_SourceNotAllowedByPolicy_IsRejected()
    {
        WriteSourceFile("x.txt", TimeSpan.FromSeconds(10));
        var tool = CreateTool(sourcePattern: "^Z:\\\\nope\\\\.*$", targetPattern: ".*");

        var ex = Assert.Throws<McpException>(() => tool.MoveFile(_source, _target, "x.txt"));

        Assert.Contains("not allowed", ex.Message);
    }

    [Theory]
    [InlineData("relative", "C:\\out", "file.txt")]        // source not rooted
    [InlineData("C:\\in\\..\\x", "C:\\out", "file.txt")]  // ".." segment
    [InlineData("C:\\in", "C:\\out", "sub/file.txt")]      // separator in file name
    public void MoveFile_InvalidArguments_AreRejected(string source, string target, string fileName)
    {
        var tool = CreateTool();

        Assert.Throws<McpException>(() => tool.MoveFile(source, target, fileName));
    }
}
