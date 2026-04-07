# Phase 1: Framework Upgrade and Bug Fix

## Objective

Upgrade `PclSharp.csproj` from .NET 4.5.2 to .NET Framework 4.8, and fix a bug in `LibraryLoader.cs` where the native DLL preload path has a double `.dll` extension.

**Estimated time:** 5 minutes  
**Risk:** Low — the changes are minimal and backward-compatible

---

## Background

PclSharp currently targets .NET 4.5.2. The WPF application targets .NET 4.8. While .NET 4.5.2 assemblies *can* run on a .NET 4.8 host, upgrading PclSharp to 4.8 ensures framework version consistency, eliminates potential binding redirect issues, and aligns with the other projects in the solution (PclSharp.Test, PclSharp.Vis, PclSharp.Kinect already target 4.8).

The `LibraryLoader` bug is pre-existing: `Native.DllName = "PclSharp.Extern.dll"` already includes the `.dll` extension, but the loader appends another `.dll`, making the path `x64/PclSharp.Extern.dll.dll`. This silently fails — Windows ignores the bad path and `DllImport` still resolves `PclSharp.Extern.dll` via the normal DLL search path. However, fixing it enables explicit preloading from `x64/`, which is the intended behavior.

---

## Task 1.1 — Upgrade TargetFrameworkVersion

**File:** `src/PclSharp/PclSharp.csproj`  
**Line:** 12

### Change

```xml
<!-- Before -->
<TargetFrameworkVersion>v4.5.2</TargetFrameworkVersion>

<!-- After -->
<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
```

### How to apply

**Option A — Edit in Visual Studio:**
1. Open `src/PclSharp.sln` in VS 2022
2. Right-click PclSharp project → Properties
3. Application tab → Target framework → select ".NET Framework 4.8"
4. Save (VS updates the csproj automatically)

**Option B — Edit the file directly:**
Open `src/PclSharp/PclSharp.csproj` in any text editor and change line 12.

### What changes

Only this one line changes. The `<AllowUnsafeBlocks>`, `<OutputPath>`, `<PlatformTarget>`, and all `<Compile>` / `<Reference>` items remain identical. The code itself requires no changes — there are no .NET 4.5.2-specific APIs in use.

### NuGet package compatibility

All three NuGet packages used by PclSharp are compatible with .NET 4.8:

| Package | Version | Compat |
|---------|---------|--------|
| System.Numerics.Vectors | 4.4.0 | `portable-net45+win8+wp8+wpa81` target — works on 4.8 |
| System.Runtime.CompilerServices.Unsafe | 4.4.0 | `netstandard1.0` target — works on 4.8 |
| System.ValueTuple | 4.4.0 | `netstandard1.0` target — works on 4.8 |

No package version updates are required.

**Note on packages.config:** The `targetFramework` attribute in `src/PclSharp/packages.config` currently reads `net452`. Update this to `net48` for correctness (it does not affect runtime behavior, only NuGet tooling):

```xml
<!-- src/PclSharp/packages.config — update targetFramework attributes -->
<packages>
  <package id="System.Numerics.Vectors" version="4.4.0" targetFramework="net48" />
  <package id="System.Runtime.CompilerServices.Unsafe" version="4.4.0" targetFramework="net48" />
  <package id="System.ValueTuple" version="4.4.0" targetFramework="net48" />
</packages>
```

---

## Task 1.2 — Fix LibraryLoader Double-Extension Bug

**File:** `src/PclSharp/Utils/LibraryLoader.cs`  
**Line:** 42

### Root Cause

```csharp
// Native.cs
public const string DllName = "PclSharp.Extern.dll";  // already has .dll

// LibraryLoader.cs — line 42 (current, buggy)
loader.LoadLibrary($"{dir}{Native.DllName}.dll");
// Evaluates to: "x64/PclSharp.Extern.dll.dll"  ← does not exist!
```

`LoadLibrary("x64/PclSharp.Extern.dll.dll")` returns `IntPtr.Zero` (failure) but no exception is thrown. Windows then defers DLL resolution to `DllImport`, which finds `PclSharp.Extern.dll` in the application directory via the standard search path.

### Fix

```csharp
// LibraryLoader.cs — line 42 (corrected)
loader.LoadLibrary($"{dir}{Native.DllName}");
// Evaluates to: "x64/PclSharp.Extern.dll"  ← correct
```

### Full corrected LoadLibraries() method

```csharp
internal static void LoadLibraries()
{
    //windows only for now...

    DllLoadUtils loader = new DllLoadUtilsWindows();

    string dir;
    dir = IntPtr.Size == 8 ? "x64/" : "x86/";

    //by loading the library before dllimport use, we can effectively remap them to wherever we've loaded it from.
    loader.LoadLibrary($"{dir}{Native.DllName}");  // FIX: removed extra .dll

    foreach (var name in AdditionalLibraries)
    {
        var filePath = $"{dir}{name}.dll";
        if (File.Exists(filePath))
            loader.LoadLibrary(filePath);
    }
}
```

### Impact after fix

After this fix:
- `LoadLibrary("x64/PclSharp.Extern.dll")` succeeds if the DLL is present in the `x64/` subdirectory
- PCL runtime DLLs continue to load from `x64/<name>.dll` (unchanged — these names don't include `.dll`)
- The fix enables explicit preloading, which means `PclSharp.Extern.dll` can be placed in `x64/` (in addition to, or instead of, the exe root directory)

### Behavior without fix (for reference)

The library still works without this fix because:
1. `LoadLibrary("x64/PclSharp.Extern.dll.dll")` → fails silently (returns IntPtr.Zero)
2. First `[DllImport("PclSharp.Extern.dll")]` call → Windows searches for `PclSharp.Extern.dll` in:
   - The application directory (where the exe is)
   - Directories on PATH
   - System directories (System32, SysWOW64)
3. If `PclSharp.Extern.dll` is in the exe directory → load succeeds

---

## Task 1.3 — Update vcxproj Release|x64 Toolset (Related Prep)

> **Note:** This is a preparation step for Phase 2. Document it here because it is a source code change.

**File:** `src/PclSharp.Extern/PclSharp.Extern.vcxproj`

The `Release|x64` configuration currently uses `<PlatformToolset>v141</PlatformToolset>` (VS 2017 toolset). VS 2022 installs v143. Building Release|x64 with v141 requires the VS 2017 Build Tools to be separately installed — which is an unnecessary dependency.

### Change

Find the PropertyGroup for `Release|x64` (approximately line 47):

```xml
<!-- Before -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
  <ConfigurationType>DynamicLibrary</ConfigurationType>
  <UseDebugLibraries>false</UseDebugLibraries>
  <PlatformToolset>v141</PlatformToolset>
  <WholeProgramOptimization>true</WholeProgramOptimization>
  <CharacterSet>Unicode</CharacterSet>
</PropertyGroup>

<!-- After -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
  <ConfigurationType>DynamicLibrary</ConfigurationType>
  <UseDebugLibraries>false</UseDebugLibraries>
  <PlatformToolset>v143</PlatformToolset>
  <WholeProgramOptimization>true</WholeProgramOptimization>
  <CharacterSet>Unicode</CharacterSet>
</PropertyGroup>
```

---

## Verification

After applying all changes in this phase:

1. Verify `PclSharp.csproj` line 12 reads `v4.8`:
   ```
   findstr "TargetFrameworkVersion" src\PclSharp\PclSharp.csproj
   ```
   Expected: `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`

2. Verify `LibraryLoader.cs` fix:
   ```
   findstr "DllName" src\PclSharp\Utils\LibraryLoader.cs
   ```
   Expected: `loader.LoadLibrary($"{dir}{Native.DllName}");` (no trailing `.dll`)

3. Verify vcxproj toolset:
   ```
   findstr "v141" src\PclSharp.Extern\PclSharp.Extern.vcxproj
   ```
   Expected: no output (all v141 references removed).

---

## Rollback

If any issues arise:
- Revert `PclSharp.csproj` line 12 to `v4.5.2`
- Revert `LibraryLoader.cs` line 42 to the original string interpolation
- Revert `PclSharp.Extern.vcxproj` v143 back to v141

All three files are tracked in git. Use `git diff` to review before and after.

---

## Phase 1 Checklist

- [ ] `PclSharp.csproj` TargetFrameworkVersion changed to v4.8
- [ ] `packages.config` targetFramework attributes updated to net48
- [ ] `LibraryLoader.cs` line 42 extra `.dll` removed
- [ ] `PclSharp.Extern.vcxproj` Release|x64 PlatformToolset changed to v143
- [ ] `findstr "TargetFrameworkVersion"` confirms v4.8
- [ ] `findstr "v141"` confirms no remaining v141 references
