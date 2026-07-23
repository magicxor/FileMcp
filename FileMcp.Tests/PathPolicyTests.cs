using FileMcp.Configuration;
using Xunit;

namespace FileMcp.Tests;

public class PathPolicyTests
{
    private static PathPolicy Policy(
        IEnumerable<string>? source = null,
        IEnumerable<string>? target = null,
        int maxAgeSeconds = 600)
    {
        return new PathPolicy(new FileMoveOptions
        {
            AllowedSourcePatterns = (source ?? Array.Empty<string>()).ToList(),
            AllowedTargetPatterns = (target ?? Array.Empty<string>()).ToList(),
            MaxFileAgeSeconds = maxAgeSeconds,
        });
    }

    [Fact]
    public void EmptyPatterns_DenyAll()
    {
        var policy = Policy();

        Assert.False(policy.IsSourceAllowed("C:\\anything"));
        Assert.False(policy.IsTargetAllowed("C:\\anything"));
        Assert.False(policy.HasSourcePatterns);
        Assert.False(policy.HasTargetPatterns);
    }

    [Fact]
    public void Matching_AllowsAndBlocks()
    {
        var policy = Policy(
            source: new[] { "^C:\\\\Temp\\\\In\\\\.*$" },
            target: new[] { "^C:\\\\Temp\\\\Out\\\\.*$" });

        Assert.True(policy.IsSourceAllowed("C:\\Temp\\In\\job1"));
        Assert.False(policy.IsSourceAllowed("C:\\Temp\\Other"));
        Assert.True(policy.IsTargetAllowed("C:\\Temp\\Out\\job1"));
        Assert.False(policy.IsTargetAllowed("C:\\Temp\\In\\job1"));
    }

    [Fact]
    public void Matching_IsCaseInsensitive()
    {
        var policy = Policy(source: new[] { "^C:\\\\Temp\\\\In\\\\.*$" });

        Assert.True(policy.IsSourceAllowed("c:\\temp\\in\\job1"));
    }

    [Fact]
    public void MultiplePatterns_UseOrSemantics()
    {
        var policy = Policy(source: new[] { "^C:\\\\A\\\\.*$", "^D:\\\\B\\\\.*$" });

        Assert.True(policy.IsSourceAllowed("C:\\A\\x"));
        Assert.True(policy.IsSourceAllowed("D:\\B\\y"));
        Assert.False(policy.IsSourceAllowed("E:\\C\\z"));
    }

    [Fact]
    public void MaxFileAge_ReflectsOptions()
    {
        var policy = Policy(maxAgeSeconds: 120);

        Assert.Equal(TimeSpan.FromSeconds(120), policy.MaxFileAge);
    }
}
