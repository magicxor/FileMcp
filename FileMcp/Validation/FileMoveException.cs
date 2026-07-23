namespace FileMcp.Validation;

/// <summary>
/// Represents an expected, user-facing failure of the move operation (invalid argument,
/// blocked by policy, file too old, name clash, ...). The <see cref="Exception.Message"/>
/// is written to be clear and actionable for the calling AI agent, and is surfaced to the
/// MCP client as the tool's error text.
/// </summary>
public sealed class FileMoveException : Exception
{
    public FileMoveException(string message) : base(message)
    {
    }
}
