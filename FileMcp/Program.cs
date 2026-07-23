using System.Text.RegularExpressions;
using FileMcp.Configuration;
using FileMcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Bind and validate the server settings from the "FileMove" section of appsettings.json.
builder.Services
    .AddOptions<FileMoveOptions>()
    .Bind(builder.Configuration.GetSection(FileMoveOptions.SectionName))
    .Validate(o => o.MaxFileAgeSeconds > 0, "FileMove:MaxFileAgeSeconds must be greater than 0.")
    .Validate(o => AllPatternsCompile(o.AllowedSourcePatterns),
        "FileMove:AllowedSourcePatterns contains an invalid regular expression.")
    .Validate(o => AllPatternsCompile(o.AllowedTargetPatterns),
        "FileMove:AllowedTargetPatterns contains an invalid regular expression.")
    .ValidateOnStart();

// Shared services used by the tool.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PathPolicy>();

// Add the MCP services: the transport to use (stdio) and the single tool to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FileMoveTools>();

await builder.Build().RunAsync();

static bool AllPatternsCompile(IEnumerable<string> patterns)
{
    foreach (var pattern in patterns)
    {
        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    return true;
}
