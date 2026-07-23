using FileMcp.Validation;
using Xunit;

namespace FileMcp.Tests;

public class PathValidatorTests
{
    [Theory]
    [InlineData("report.pdf")]
    [InlineData("a.txt")]
    [InlineData("no-extension")]
    public void ValidateFileName_AcceptsPlainNames(string fileName)
    {
        PathValidator.ValidateFileName(fileName); // does not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("sub/report.pdf")]
    [InlineData("sub\\report.pdf")]
    [InlineData("bad|name.txt")]
    public void ValidateFileName_RejectsInvalidNames(string fileName)
    {
        Assert.Throws<FileMoveException>(() => PathValidator.ValidateFileName(fileName));
    }

    [Theory]
    [InlineData("C:\\data\\out")]
    [InlineData("C:\\data\\out\\")]
    [InlineData("\\\\server\\share\\folder")]
    public void ValidateDirectory_AcceptsAbsolutePaths(string path)
    {
        PathValidator.ValidateDirectory(path, "dir"); // does not throw
    }

    [Theory]
    [InlineData("relative\\path")]            // not rooted
    [InlineData("out")]                        // not rooted
    [InlineData("C:\\data\\..\\out")]         // contains ".."
    [InlineData("C:\\data\\.\\out")]          // contains "."
    [InlineData("C:\\data\\ou<t")]            // invalid character in segment
    public void ValidateDirectory_RejectsInvalidPaths(string path)
    {
        Assert.Throws<FileMoveException>(() => PathValidator.ValidateDirectory(path, "dir"));
    }

    [Fact]
    public void ValidateDirectory_RejectsEmpty()
    {
        Assert.Throws<FileMoveException>(() => PathValidator.ValidateDirectory("", "dir"));
    }
}
