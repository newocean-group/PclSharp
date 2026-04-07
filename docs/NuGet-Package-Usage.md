# PclSharp NuGet Packages

PclSharp is distributed as two NuGet packages hosted on the Newocean private feed:

| Package | Description |
|---|---|
| `PclSharp` | C# managed library (.NET 4.8). Automatically pulls in the runtime package. |
| `PclSharp.Runtime.x64` | Native x64 DLLs (`PclSharp.Extern.dll` and all PCL/Boost dependencies). |

You only need to install `PclSharp` — the runtime package is a declared dependency and will be installed automatically.

---

## 1. Add the Private NuGet Feed

### Option A: Visual Studio

1. Go to **Tools → NuGet Package Manager → Package Manager Settings → Package Sources**.
2. Click **+** and set:
   - **Name**: Newocean
   - **Source**: `<YOUR_FEED_URL>` *(ask your team lead for the URL)*
3. Click **Update**, then **OK**.

### Option B: `nuget.config` (per-repository)

Add or update `nuget.config` at the solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Newocean" value="<YOUR_FEED_URL>" />
  </packageSources>
</configuration>
```

Commit this file so all team members pick it up automatically.

---

## 2. Install the Package

### Via Visual Studio Package Manager UI

1. Right-click your project → **Manage NuGet Packages**.
2. Select the **Newocean** feed from the source dropdown.
3. Search for `PclSharp`, select version `1.0.0`, and click **Install**.

### Via Package Manager Console

```powershell
Install-Package PclSharp -Version 1.0.0 -Source Newocean
```

### Via `packages.config` (manual)

```xml
<packages>
  <package id="PclSharp" version="1.0.0" targetFramework="net48" />
  <package id="PclSharp.Runtime.x64" version="1.0.0" targetFramework="net48" />
</packages>
```

---

## 3. Project Configuration

Your project **must target x64** (not AnyCPU). In Visual Studio:

1. Open **Project Properties → Build**.
2. Set **Platform target** to `x64`.

Or in your `.csproj`:

```xml
<PlatformTarget>x64</PlatformTarget>
```

> **Why?** The native DLLs are x64-only. Running as AnyCPU on a 64-bit machine will cause a `BadImageFormatException` when loading `PclSharp.Extern.dll`.

---

## 4. How Native DLLs Are Deployed

When you build your project, the `PclSharp.Runtime.x64.targets` MSBuild file (imported automatically by NuGet) copies all native DLLs into an `x64\` subfolder of your output directory:

```
YourProject\bin\x64\Debug\
├── YourApp.exe
├── PclSharp.dll
└── x64\
    ├── PclSharp.Extern.dll   ← C++ PCL bridge
    ├── pcl_common.dll
    ├── pcl_filters.dll
    ├── boost_filesystem-...dll
    └── ... (all PCL/Boost dependencies)
```

`PclSharp`'s `LibraryLoader` automatically picks up DLLs from this `x64\` subfolder at runtime — no manual path setup required.

---

## 5. Basic Usage

```csharp
using PclSharp;
using PclSharp.Struct;

// Create a point cloud
var cloud = new PointCloud<PointXYZ>();
cloud.Add(new PointXYZ { X = 1f, Y = 2f, Z = 3f });
Console.WriteLine($"Points: {cloud.Size}");

// Load from PCD file
var reader = new IO.PCDReader<PointXYZ>();
var loaded = new PointCloud<PointXYZ>();
reader.Read("mycloud.pcd", loaded);
```

---

## 6. Building and Publishing New Package Versions

Do this whenever you update the C++ bindings or the C# wrapper.

### Prerequisites

- Visual Studio 2022 with v143 toolset installed
- vcpkg set up (manifest mode — restores automatically on first build)
- [nuget.exe](https://www.nuget.org/downloads) downloaded and placed somewhere accessible (e.g. `C:\tools\nuget.exe`)

---

### Step 1 — Build Release|x64 in Visual Studio

1. Open `src\PclSharp.sln` in Visual Studio 2022.
2. In the toolbar, set the configuration dropdowns to **Release** and **x64**.
3. Go to **Build → Build Solution** (or press `Ctrl+Shift+B`).
4. Confirm there are no errors. The output DLLs land in `bin\x64\Release\`.

> Build `PclSharp.Extern` first if you only changed the C++ side:
> right-click **PclSharp.Extern** in Solution Explorer → **Build**.

---

### Step 2 — Pack the NuGet packages

Open a **Command Prompt** or **PowerShell** window, navigate to the repo root, then run:

```
nuget.exe pack nuget\PclSharp.Runtime.x64.nuspec -OutputDirectory nuget\out
nuget.exe pack nuget\PclSharp.nuspec -OutputDirectory nuget\out
```

This produces two `.nupkg` files in `nuget\out\`:
- `PclSharp.Runtime.x64.1.0.0.nupkg`
- `PclSharp.1.0.0.nupkg`

**Optional sanity check:** rename either `.nupkg` to `.zip` and open it in Windows Explorer to verify the DLL files are inside `build\x64\`.

---

### Step 3 — Push to the private feed

Push the **runtime package first** (it must exist in the feed before the managed package can resolve its dependency):

```
nuget.exe push nuget\out\PclSharp.Runtime.x64.1.0.0.nupkg -Source <FEED_URL> -ApiKey <API_KEY>
nuget.exe push nuget\out\PclSharp.1.0.0.nupkg             -Source <FEED_URL> -ApiKey <API_KEY>
```

Replace `<FEED_URL>` and `<API_KEY>` with your private NuGet server URL and credentials.

> Always push `PclSharp.Runtime.x64` **before** `PclSharp`.

### Bumping the version

Edit both nuspec files and change the `<version>` and the dependency version:

- `nuget/PclSharp.Runtime.x64.nuspec` — `<version>`
- `nuget/PclSharp.nuspec` — `<version>` **and** the `<dependency id="PclSharp.Runtime.x64" version="..."/>` line

Use [Semantic Versioning](https://semver.org/):
- Patch (`1.0.x`): bug fixes, no API changes
- Minor (`1.x.0`): new PCL features exposed
- Major (`x.0.0`): breaking API changes

---

## 7. Troubleshooting

| Problem | Likely Cause | Fix |
|---|---|---|
| `DllNotFoundException: PclSharp.Extern.dll` | Native DLLs not in `x64\` subfolder | Confirm project targets x64, not AnyCPU. Clean and rebuild. |
| `BadImageFormatException` | Architecture mismatch | Set `PlatformTarget=x64` in your project. |
| `FileNotFoundException` on a PCL DLL | Missing dependency in the runtime package | Rebuild the runtime package after a clean Release build. |
| Package not found in feed | Wrong feed URL or missing credentials | Verify `nuget.config` feed URL and authentication. |
