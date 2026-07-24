# FileMcp — a policy-guarded "move file" MCP server

An [MCP](https://modelcontextprotocol.io/) server (built with the official
[MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) and .NET 10, stdio
transport) that exposes a **single** tool: `move_file`. It moves one file from a source
directory to a target directory, keeping the same file name, under strict validation and a
configurable allow-list.

## The `move_file` tool

Three required arguments (the only surface exposed to the AI agent):

| Argument          | Description                                                        |
| ----------------- | ------------------------------------------------------------------ |
| `sourceDirectory` | Absolute path to the directory the file currently lives in.        |
| `targetDirectory` | Absolute path to the directory to move it into (created if absent).|
| `fileName`        | File name including extension, e.g. `report.pdf`.                  |

### Guarantees / checks (each failure returns a clear error message)

1. Both directory paths must be **absolute (rooted / fully qualified)**.
2. Neither path may contain a `.` or `..` segment. The raw string is checked as-is — it is
   never normalized, so `..` is rejected rather than silently collapsed.
3. Paths and the file name must be **sanitized** (no invalid characters); the file name may
   not contain a path separator.
4. Both directories must match the configured **allow-list regex patterns** (see below).
5. The source file must exist and must not be **older than the configured maximum age**
   (based on last-write time).
6. The target directory is **created** if it does not exist.
7. If a file with the same name already exists in the target, the move is **refused** (no
   overwrite).

## Configuration — `appsettings.json`

Settings live under the `FileMove` section:

```json
{
  "FileMove": {
    "AllowedSourcePatterns": [ "^/tmp/mcp/in(/.*)?$" ],
    "AllowedTargetPatterns": [ "^/tmp/mcp/out(/.*)?$" ],
    "MaxFileAgeSeconds": 600
  }
}
```

- **`AllowedSourcePatterns` / `AllowedTargetPatterns`** — lists of regular expressions. A
  directory is allowed if it matches **any** pattern (case-insensitive). An **empty list
  means deny-all** for that side. Anything not matching is blocked with a clear error.
- **`MaxFileAgeSeconds`** — maximum permitted file age in seconds. Default **600** (10 min).

Invalid regexes or a non-positive `MaxFileAgeSeconds` fail fast at startup with a clear
message.

### Path form

The server targets Linux, so the patterns are ordinary POSIX paths with `/` separators and
need no escaping — e.g. `^/tmp/mcp/in(/.*)?$` matches `/tmp/mcp/in` and anything beneath it.
(If you ever point this at Windows paths, remember one literal backslash is `\\` in a regex,
which is `\\\\` inside a JSON string.)

## Developing / running locally

Configure your IDE to run the project directly (VS Code `.vscode/mcp.json` or Visual
Studio `.mcp.json`):

```json
{
  "servers": {
    "FileMcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [ "run", "--project", "FileMcp/FileMcp.csproj" ]
    }
  }
}
```

Then ask the agent to move a file, e.g. *"Move report.pdf from C:\Temp\Mcp\In to
C:\Temp\Mcp\Out"*.

## Tests

`dotnet test` runs the `FileMcp.Tests` project, which covers path validation, the allow-list
policy (including deny-all and case-insensitivity), the max-age boundary (via a fake
`TimeProvider`), and the move mechanics (target creation, name-clash refusal, missing
source, and blocked paths).

## More information

- [Official MCP Documentation](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
