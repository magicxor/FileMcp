using FileMcp.Validation;
using Xunit;

namespace FileMcp.Tests;

public class PathValidatorTests
{
    [Theory]
    [InlineData("report.pdf")]
    [InlineData("a.txt")]
    [InlineData("no-extension")]
    [InlineData("naïve.txt")]          // non-ASCII letters are fine
    [InlineData("my report.pdf")]      // an interior space is fine
    public void ValidateFileName_AcceptsPortableNames(string fileName)
    {
        PathValidator.ValidateFileName(fileName); // does not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]                 // all dots
    [InlineData("sub/report.pdf")]      // POSIX separator
    [InlineData("sub\\report.pdf")]     // Windows separator (rejected even when running on Linux)
    [InlineData("bad|name.txt")]        // pipe
    [InlineData("a<b.txt")]
    [InlineData("a>b.txt")]
    [InlineData("a:b.txt")]             // colon (also alternate-data-stream on Windows)
    [InlineData("a\"b.txt")]
    [InlineData("a?b.txt")]
    [InlineData("a*b.txt")]
    [InlineData("tab\tname.txt")]       // control character
    [InlineData("CON")]                 // reserved Windows device name
    [InlineData("con.txt")]             // reserved, case-insensitive, with extension
    [InlineData("LPT1.log")]
    [InlineData("report.")]             // trailing dot (Windows strips it)
    [InlineData("report ")]             // trailing space (Windows strips it)
    public void ValidateFileName_RejectsUnsafeNames(string fileName)
    {
        Assert.Throws<FileMoveException>(() => PathValidator.ValidateFileName(fileName));
    }

    [Theory]
    [InlineData("/srv/out")]            // POSIX absolute (the production shape on Linux)
    [InlineData("/srv/out/")]           // single trailing separator is benign
    [InlineData("/")]                   // POSIX root
    [InlineData("C:\\data\\out")]       // Windows drive-absolute
    [InlineData("C:\\data\\out\\")]
    [InlineData("C:\\")]                // drive root
    [InlineData("\\\\server\\share\\folder")] // UNC
    public void ValidateDirectory_AcceptsAbsolutePaths(string path)
    {
        PathValidator.ValidateDirectory(path, "dir"); // does not throw
    }

    [Theory]
    [InlineData("relative/path")]       // not rooted
    [InlineData("relative\\path")]      // not rooted
    [InlineData("out")]                 // not rooted
    [InlineData("C:")]                  // drive-relative
    [InlineData("C:data\\out")]         // drive-relative
    [InlineData("\\data\\out")]         // single leading '\' is drive-relative on Windows
    [InlineData("/data/../out")]        // ".." segment
    [InlineData("C:\\data\\..\\out")]   // ".." segment
    [InlineData("/data/./out")]         // "." segment
    [InlineData("/data/.../out")]       // all-dots segment
    [InlineData("/data//out")]          // empty segment (double separator)
    [InlineData("/data/out//")]         // doubled trailing separator (empty segment)
    [InlineData("/data/ou<t")]          // invalid character in segment
    [InlineData("C:\\data\\ou|t")]      // invalid character in segment
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
