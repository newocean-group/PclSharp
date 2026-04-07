# PclSharp
Point Cloud Library P/Invoke binding for C#

This project is a fork of the original PclSharp repository.

## Updates in this Fork
- Updated to compile successfully with **Visual Studio 2022**
- Added fixes for compatibility with modern toolchains
- Implemented missing features and improvements for **ICP (Iterative Closest Point)** functionality
- General stability and build fixes

## Original Notes
The original project used C# 7 features and required Visual Studio 2017 to build.  
This fork removes those limitations and supports newer development environments.

## Installing via NuGet (Team)

PclSharp is distributed via the **Newocean private NuGet feed** as two packages:

| Package | Description |
|---|---|
| `PclSharp` | C# managed library (.NET 4.8) |
| `PclSharp.Runtime.x64` | Native x64 DLLs (auto-installed as dependency) |

Install `PclSharp` only — the runtime package is pulled in automatically.

### 1. Add the private feed

In Visual Studio, go to **Tools → NuGet Package Manager → Package Sources**, click **+** and set:

- **Name**: `Newocean`
- **Source**: ask your team lead for the feed URL

Click **Update**, then **OK**.

### 2. Install the package

```powershell
Install-Package PclSharp -Version 1.0.0 -Source Newocean
```

Or use the Visual Studio Package Manager UI, selecting the **Newocean** source.

### 3. Set your project to x64

The native DLLs are x64-only. In your `.csproj`:

```xml
<PlatformTarget>x64</PlatformTarget>
```

> For full details — building new versions, publishing, and troubleshooting — see [`docs/NuGet-Package-Usage.md`](docs/NuGet-Package-Usage.md).

---

If you encounter issues or want to contribute improvements, feel free to open an issue or pull request.
