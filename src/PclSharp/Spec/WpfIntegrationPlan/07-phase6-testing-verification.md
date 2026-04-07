# Phase 6: Testing and Verification

## Objective

Verify the complete build and integration works end-to-end: run unit tests, smoke test the WPF integration, and confirm the full PCL pipeline operates correctly.

---

## Task 6.1 — Run PclSharp Unit Tests

The `PclSharp.Test` project (MSTest) provides unit tests covering core PclSharp functionality. Run these first to confirm the base library works correctly before the WPF integration.

### Build the test project

```cmd
msbuild src\PclSharp.Test\PclSharp.Test.csproj ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /v:minimal
```

The post-build event in `PclSharp.Test.csproj` automatically copies native DLLs to the test output directory.

### Run tests

```cmd
vstest.console.exe src\PclSharp.Test\bin\x64\Debug\PclSharp.Test.dll ^
    /Platform:x64 ^
    /logger:trx
```

Or using dotnet test (if VS Test host is configured):
```cmd
dotnet test src\PclSharp.Test\PclSharp.Test.csproj -c Debug -p:Platform=x64
```

### Expected result

```
Test run for PclSharp.Test.dll (.NETFramework,Version=v4.8)
Passed! - Failed: 0, Passed: N, Skipped: 0, Total: N
```

If tests fail, see the troubleshooting table in Task 6.5.

---

## Task 6.2 — WPF Smoke Test

Run the WPF application with the smoke test code from Phase 4 Task 4.5. Verify:

1. Application starts without crash
2. The `Debug.WriteLine` message appears in VS Output window: `[PclSharp] Loaded OK. Empty cloud size: 0`
3. No `MessageBox` with error appears

If a `DllNotFoundException` message box appears → go to Phase 5 and re-check DLL deployment.  
If a `BadImageFormatException` message box appears → check that `<PlatformTarget>x64</PlatformTarget>` is in the WPF project and that you're running the x64 build.

---

## Task 6.3 — Verify Runtime Architecture

Add this to your startup code and check the VS Output window:

```csharp
System.Diagnostics.Debug.WriteLine($"Is64BitProcess: {Environment.Is64BitProcess}");
System.Diagnostics.Debug.WriteLine($"IntPtr.Size: {IntPtr.Size}");  // Should be 8
System.Diagnostics.Debug.WriteLine($"OS: {Environment.OSVersion}");
```

Expected output:
```
Is64BitProcess: True
IntPtr.Size: 8
OS: Microsoft Windows NT 10.0.XXXXX.0
```

If `Is64BitProcess` is `False`, the project is not configured as x64. Re-apply Phase 4 Task 4.1.

---

## Task 6.4 — End-to-End PCD File Test

This test validates loading a PCD file, applying a filter, and accessing results — the full PCL pipeline.

### Get a test PCD file

Option A: Use any `.pcd` file from PCL's sample data repository.  
Option B: Create a simple synthetic point cloud in code:

```csharp
using PclSharp.PointCloud;
using PclSharp.Filters;
using PclSharp.Struct;

// Create a synthetic cloud with 1000 random points
using (var cloud = new PointCloudOfXYZ())
{
    var rng = new Random(42);
    for (int i = 0; i < 1000; i++)
    {
        cloud.PushBack(new PointXYZ
        {
            X = (float)(rng.NextDouble() * 2 - 1),
            Y = (float)(rng.NextDouble() * 2 - 1),
            Z = (float)(rng.NextDouble() * 2 - 1)
        });
    }
    
    Debug.Assert(cloud.Size == 1000, "Cloud should have 1000 points");
    
    // Apply VoxelGrid filter
    using (var filtered = new PointCloudOfXYZ())
    using (var voxel = new VoxelGridOfXYZ())
    {
        voxel.SetInputCloud(cloud);
        voxel.LeafSize = new PointXYZ { X = 0.1f, Y = 0.1f, Z = 0.1f };
        voxel.Filter(filtered);
        
        // Filtered cloud should have fewer points
        Debug.Assert(filtered.Size < cloud.Size, 
            $"VoxelGrid should reduce point count. Got: {filtered.Size}");
        System.Diagnostics.Debug.WriteLine(
            $"VoxelGrid: {cloud.Size} → {filtered.Size} points");
    }
}
```

Expected: Filter reduces point count (exact number depends on spatial distribution).

---

## Task 6.5 — Troubleshooting Guide

### Common Runtime Errors

| Error | Most Likely Cause | Fix |
|-------|------------------|-----|
| `DllNotFoundException: PclSharp.Extern.dll` | DLL not in `x64\` subfolder | Copy `PclSharp.Extern.dll` to `$(OutDir)x64\` |
| `DllNotFoundException: pcl_common_release.dll` | PCL runtime DLLs not in `x64\` | Copy all PCL DLLs from vcpkg_installed to `x64\` |
| `DllNotFoundException: boost_system-...dll` | Boost DLLs missing from `x64\` | Copy all `boost_*.dll` from vcpkg_installed\bin\ to `x64\` |
| `BadImageFormatException` | x86/x64 mismatch | Set `<PlatformTarget>x64</PlatformTarget>` in WPF .csproj |
| `EntryPointNotFoundException` | C++ function not exported | Rebuild `PclSharp.Extern.vcxproj`; check with `dumpbin /exports` |
| `AccessViolationException` | Using disposed native object | Check all `using` blocks; don't pass disposed objects to PCL functions |
| `InvalidOperationException: has been disposed` | Double-dispose | Review `IDisposable` usage; use `using` statements consistently |
| No `x64\` folder created | Post-build event didn't run | Build the WPF project (not just PclSharp); check post-build event output |

### Managed Assembly Loading Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `FileNotFoundException: PclSharp.dll` | HintPath is wrong | Check `<HintPath>` in WPF .csproj points to correct location |
| `FileLoadException: version mismatch` | Assembly version conflict | Add `<bindingRedirect>` to App.config |
| `FileNotFoundException: System.ValueTuple.dll` | NuGet dep not deployed | Add package reference to WPF packages.config |

### Debugging DLL Loading

**Step 1:** Enable Fusion Log (managed assembly binding):
```
HKLM\SOFTWARE\Microsoft\Fusion
LogFailures = 1 (DWORD)
LogPath = C:\FusionLog\ (String)
```
Run the app, then check `C:\FusionLog\` for binding failure logs.

**Step 2:** Use Process Monitor for native DLL failures:
- Filter: Process = your app, Result = "NAME NOT FOUND"
- Look for paths like `...\x64\pcl_common_release.dll` with "PATH NOT FOUND"

**Step 3:** Use `Dependencies.exe` to inspect `PclSharp.Extern.dll`'s full import chain before running.

---

## Task 6.6 — Build Matrix

| Configuration | C++ Toolset | .NET Target | Status |
|--------------|-------------|-------------|--------|
| Debug\|x64 | v143 (VS 2022) | .NET 4.8 | **Primary — use for development** |
| Release\|x64 | v143 (after Phase 1 fix) | .NET 4.8 | Supported — use for production |
| Debug\|x86 | N/A | N/A | **Not supported** (no x86 vcpkg triplet) |
| Release\|x86 | N/A | N/A | **Not supported** |
| AnyCPU | N/A | N/A | **Not supported** (native DLL is x64 only) |

---

## Phase 6 Checklist

- [ ] All PclSharp.Test unit tests pass (0 failures)
- [ ] WPF app starts without DLL exceptions
- [ ] Smoke test output visible in VS Debug Output window
- [ ] `Environment.Is64BitProcess` == `true`
- [ ] VoxelGrid filter test produces fewer points than input
- [ ] No `AccessViolationException` during normal usage
- [ ] Process Monitor shows no "PATH NOT FOUND" for DLL loads
