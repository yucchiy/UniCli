# UniCli

## Project Structure

- `src`: Source code directory
    - `src/UniCli.Client`: CLI project for `unicli`
    - `src/UniCli.Unity`: Unity server implementation for `unicli` (Unity project)
        - `src/UniCli.Unity/Packages`: Server package
        - `src/UniCli.Unity/Assets/Samples`: Sample implementations for the server package
- `src/UniCli.Protocol`: Shared type definitions between `UniCli.Client` and `UniCli.Unity`
- `doc`: Documentation directory

## Quick Commands

```bash
# Build Protocol (must be built first to trigger file copy)
dotnet build src/UniCli.Protocol

# Build Client
dotnet build src/UniCli.Client

# Publish Client and test with the built binary
dotnet publish src/UniCli.Client -o .build
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client commands --json
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec Compile --json

# Run Unity EditMode tests
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec TestRunner.RunEditMode --json

# Run Unity PlayMode tests
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec TestRunner.RunPlayMode --json

# Compile Unity project (also serves as a build verification for the server)
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec Compile --json
```

## Testing

When testing CLI behavior, always publish first with `dotnet publish src/UniCli.Client -o .build`, then test with `.build/UniCli.Client` directly. Do not use `dotnet run`.

### Server-side verification (required)

`dotnet build` only verifies the client-side compilation. When modifying server-side code (`Packages/com.yucchiy.unicli-server/`), **always verify with Unity compilation and tests**.

```bash
# 1. Publish the client first
dotnet publish src/UniCli.Client -o .build

# 2. Verify server-side compilation (required)
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec Compile --json

# 3. Run tests
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec TestRunner.RunEditMode --json
UNICLI_PROJECT=src/UniCli.Unity .build/UniCli.Client exec TestRunner.RunPlayMode --json
```

### Tests requiring Unity connection

The `exec` and `commands` subcommands require a connection to the Unity Editor. If the connection fails, retry a few times. If it still fails, ask the user to confirm that Unity Editor is running with the project open.
