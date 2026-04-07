# Phase 3: Building the Managed C# Layer (PclSharp.dll)

## Objective

Build `PclSharp.dll` targeting .NET Framework 4.8, x64. This is the managed assembly that WPF applications reference.

**Estimated time:** 1–3 minutes  
**Risk:** Low — pure C# compilation, no native toolchain involved  
**Prerequisite:** Phase 1 (framework upgrade) must be applied first

---

## Background

`PclSharp.dll` is a pure C# class library. It contains:
- `[DllImport]` declarations pointing to `PclSharp.Extern.dll` (collected in `static partial class Invoke`)
- C# wrapper classes that hold an `IntPtr _ptr` to underlying native objects
- Base classes: `DisposableObject` → `UnmanagedObject` → `PclBase<PointT>`
- Point structs: `PointXYZ`, `PointXYZRGBA`, `PointXYZL`, `Normal`, `PointNormal`
- `LibraryLoader` — preloads native DLLs on first use

The majority of source files are **generated** by T4 templates (`.tt` → `.cs` + `.generated.cpp`). The generated `.cs` files are already committed to the repository and do **not** need to be regenerated for a standard build.

---

## Task 3.1 — NuGet Package Restore

NuGet packages must be restored before building. MSBuild 17.x handles this automatically during build, but it can be triggered explicitly if needed.

### Automatic (happens during msbuild)

MSBuild 17.x automatically restores `packages.config` NuGet packages before building. No manual action is required in most cases.

### Manual restore (if automatic fails)

Using MSBuild:
```cmd
msbuild src\PclSharp.sln /t:Restore /p:Configuration=Debug /p:Platform=x64
```

Using NuGet CLI (if `nuget.exe` is available):
```cmd
nuget restore src\PclSharp.sln
```

### Verify restore

```cmd
dir "src\packages\System.ValueTuple.4.4.0\lib\netstandard1.0\System.ValueTuple.dll"
dir "src\packages\System.Numerics.Vectors.4.4.0\lib\portable-net45+win8+wp8+wpa81\System.Numerics.Vectors.dll"
dir "src\packages\System.Runtime.CompilerServices.Unsafe.4.4.0\lib\netstandard1.0\System.Runtime.CompilerServices.Unsafe.dll"
```

All three files must exist.

---

## Task 3.2 — Build PclSharp.dll

Open a **VS 2022 Developer Command Prompt** and navigate to the repo root.

### Debug|x64 build (recommended for development)

```cmd
msbuild src\PclSharp\PclSharp.csproj ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /v:minimal
```

**Output:** `bin\x64\Debug\PclSharp.dll` and `bin\x64\Debug\PclSharp.pdb`

### Release|x64 build (for production)

```cmd
msbuild src\PclSharp\PclSharp.csproj ^
    /p:Configuration=Release ^
    /p:Platform=x64 ^
    /v:minimal
```

**Output:** `bin\x64\Release\PclSharp.dll`

### Full solution build (C++ + C# together)

```cmd
msbuild src\PclSharp.sln ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /m ^
    /v:minimal
```

This is the recommended single command for a clean build — it builds the C++ and C# projects in the correct dependency order.

---

## Task 3.3 — Verify Build Output

```cmd
dir bin\x64\Debug\PclSharp.dll
```

Expected: file exists, approximately 200–600 KB.

Verify the correct .NET framework target using PowerShell:

```powershell
[System.Reflection.Assembly]::ReflectionOnlyLoadFrom(
    (Resolve-Path "bin\x64\Debug\PclSharp.dll").Path
).ImageRuntimeVersion
```

Expected output: `v4.0.30319` (the CLR version for all .NET Framework 4.x).

To confirm specifically that it targets .NET 4.8, inspect with ILDASM or check in VS:
1. Open `bin\x64\Debug\PclSharp.dll` in Visual Studio → Object Browser
2. Expand PclSharp assembly → right-click → Properties
3. Shows "Target framework: .NET Framework 4.8"

Or use PowerShell:
```powershell
$bytes = [System.IO.File]::ReadAllBytes((Resolve-Path "bin\x64\Debug\PclSharp.dll").Path)
# The TargetFrameworkAttribute will be visible in strings:
[System.Text.Encoding]::UTF8.GetString($bytes) | Select-String "v4.8"
```

---

## Task 3.4 — Files in the Build Output

After a successful build, `bin\x64\Debug\` contains:

| File | Description |
|------|-------------|
| `PclSharp.dll` | The managed assembly to reference from WPF |
| `PclSharp.pdb` | Debug symbols (include for debugging; optional for production) |
| `System.ValueTuple.dll` | NuGet dependency (must be deployed with the app) |
| `System.Numerics.Vectors.dll` | NuGet dependency (must be deployed) |
| `System.Runtime.CompilerServices.Unsafe.dll` | NuGet dependency (must be deployed) |

**Note:** `PclSharp.Extern.dll` is **not** in this directory — it is built by the C++ project and placed here by the C++ build (if the full solution is built together). If only the C# project was built, copy it from wherever the C++ build placed it.

---

## Task 3.5 — Common C# Build Errors and Solutions

### Error: "The type or namespace 'ValueTuple' could not be found"

**Cause:** NuGet packages not restored.  
**Fix:** Run `msbuild src\PclSharp.sln /t:Restore` then rebuild.

---

### Error: HintPath not found for NuGet packages

```
warning: Could not resolve this reference. Could not locate the assembly "System.ValueTuple..."
```

**Cause:** `src\packages\` directory is missing or incomplete.  
**Fix:** Run NuGet restore. If it still fails, check `src\PclSharp\packages.config` has the correct package IDs and versions.

---

### Error: "TargetFrameworkVersion v4.8 is not installed"

```
error MSB3644: The reference assemblies for .NETFramework,Version=v4.8 were not found.
```

**Cause:** .NET Framework 4.8 targeting pack is not installed.  
**Fix:** Open VS Installer → Modify VS 2022 → Individual components → search for ".NET Framework 4.8 targeting pack" → install.  
Or install standalone from Microsoft's website.

---

### Error: T4 template compilation errors

T4 templates (`.tt` files) run only in the VS IDE — they are not executed during MSBuild CLI builds. The generated `.cs` files are already in the repository. If you see errors about `.tt` files, ensure you haven't accidentally modified or deleted a generated `.cs` file. The `.cs` files are the build inputs, not the `.tt` templates.

---

## Task 3.6 — Debug vs Release Builds

| Aspect | Debug | Release |
|--------|-------|---------|
| Optimization | None (`/Od`) | Full (`/O2` equivalent) |
| Debug symbols | Full PDB | `pdbonly` PDB |
| `DEBUG` constant | Defined | Not defined |
| Output size | Larger | Smaller |
| Use case | Development, testing | Production deployment |

For WPF integration testing, use **Debug|x64** during development. Switch to **Release|x64** for production builds.

---

## Phase 3 Checklist

- [ ] NuGet packages restored (three packages present in `src\packages\`)
- [ ] `msbuild` completed with 0 errors
- [ ] `bin\x64\Debug\PclSharp.dll` exists
- [ ] `bin\x64\Debug\System.ValueTuple.dll` exists
- [ ] `bin\x64\Debug\System.Numerics.Vectors.dll` exists
- [ ] `bin\x64\Debug\System.Runtime.CompilerServices.Unsafe.dll` exists
- [ ] Assembly targets .NET Framework 4.8 (verified via PowerShell or VS)
