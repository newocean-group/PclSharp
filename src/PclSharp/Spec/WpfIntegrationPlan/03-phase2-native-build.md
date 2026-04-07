# Phase 2: Building the Native C++ Layer (PclSharp.Extern.dll)

## Objective

Compile `PclSharp.Extern.vcxproj` to produce `PclSharp.Extern.dll` — the native x64 DLL that wraps PCL algorithms and exposes them as C-style exports.

**Estimated time:** 5–20 minutes (depending on CPU; incremental builds are fast)  
**Risk:** Medium — requires correct VS 2022 C++ toolchain; errors are well-diagnosed below

---

## Background

`PclSharp.Extern.dll` is a Win64 DLL built with MSVC. It exports functions using the pattern:

```cpp
// export.h
#define EXPORT(T) extern "C" __declspec(dllexport) T __cdecl

// Example export (filters/VoxelGridOfXYZ.generated.cpp)
EXPORT(void*) filters_voxelGrid_xyz_ctor() {
    return new pcl::VoxelGrid<pcl::PointXYZ>();
}
EXPORT(void) filters_voxelGrid_xyz_delete(void** ptr) {
    delete static_cast<pcl::VoxelGrid<pcl::PointXYZ>*>(*ptr);
    *ptr = nullptr;
}
```

All PCL headers and runtime libraries come from the vendored vcpkg installation at `src/PclSharp.Extern/vcpkg_installed/x64-windows-openmp/`. No system-wide PCL install is needed.

---

## Task 2.1 — Verify Toolset Configuration

> This task depends on Task 1.3 from Phase 1. Confirm `PlatformToolset` is v143 for both Debug|x64 and Release|x64 before building.

Run from the repo root:
```cmd
findstr "PlatformToolset" src\PclSharp.Extern\PclSharp.Extern.vcxproj
```

Expected output:
```
<PlatformToolset>v143</PlatformToolset>   ← Debug|Win32
<PlatformToolset>v143</PlatformToolset>   ← Release|Win32
<PlatformToolset>v143</PlatformToolset>   ← Debug|x64
<PlatformToolset>v143</PlatformToolset>   ← Release|x64
```

If any line still shows `v141`, apply the fix from Phase 1 Task 1.3.

---

## Task 2.2 — Verify vcpkg Packages

The vcpkg packages must be present at `src/PclSharp.Extern/vcpkg_installed/x64-windows-openmp/`.

```cmd
dir "src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\" | findstr "pcl_"
```

Expected: lists all `pcl_*_release.dll` files.

```cmd
dir "src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\include\pcl\"
```

Expected: lists PCL header subdirectories (`common`, `filters`, `io`, `kdtree`, etc.).

**If packages are missing:** The repository was cloned incompletely. See `01-prerequisites.md` for re-cloning instructions.

---

## Task 2.3 — Build PclSharp.Extern.dll

Open a **VS 2022 Developer Command Prompt** and navigate to the repo root:

```cmd
cd C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp
```

### Option A: Debug|x64 build (recommended for development)

```cmd
msbuild src\PclSharp.Extern\PclSharp.Extern.vcxproj ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /m ^
    /v:minimal
```

**Output:** `bin\x64\Debug\PclSharp.Extern.dll`

### Option B: Release|x64 build (for production deployment)

```cmd
msbuild src\PclSharp.Extern\PclSharp.Extern.vcxproj ^
    /p:Configuration=Release ^
    /p:Platform=x64 ^
    /m ^
    /v:minimal
```

**Output:** `bin\x64\Release\PclSharp.Extern.dll`

### Option C: Build the entire solution (C++ and C# together)

```cmd
msbuild src\PclSharp.sln ^
    /p:Configuration=Debug ^
    /p:Platform=x64 ^
    /m ^
    /v:minimal
```

This builds all projects in dependency order. Useful for a clean full build.

**Flags explained:**
- `/m` — parallel build (uses all CPU cores)
- `/v:minimal` — minimal verbosity (shows only warnings and errors)
- Omit `/v:minimal` for verbose output if diagnosing build errors

---

## Task 2.4 — Verify Build Output

```cmd
dir bin\x64\Debug\PclSharp.Extern.dll
```

Expected: file exists, size > 1 MB.

Verify key exports are present:
```cmd
dumpbin /exports bin\x64\Debug\PclSharp.Extern.dll | findstr "pointcloud_xyz"
```

Expected output includes:
```
pointcloud_xyz_ctor
pointcloud_xyz_delete
pointcloud_xyz_size
pointcloud_xyz_at
pointcloud_xyz_push_back
```

Additional checks:
```cmd
dumpbin /exports bin\x64\Debug\PclSharp.Extern.dll | findstr "filters_voxelGrid"
dumpbin /exports bin\x64\Debug\PclSharp.Extern.dll | findstr "io_pcdreader"
dumpbin /exports bin\x64\Debug\PclSharp.Extern.dll | findstr "registration_icp"
```

---

## Task 2.5 — Common Build Errors and Solutions

### Error: "Platform toolset v141 not found"

```
error MSB8020: The build tools for v141 (Platform Toolset = 'v141') cannot be found.
```

**Cause:** Phase 1 Task 1.3 was not applied.  
**Fix:** Change `PlatformToolset` in `Release|x64` config from `v141` to `v143`.

---

### Error: "Cannot open include file: 'pcl/...'"

```
fatal error C1083: Cannot open include file: 'pcl/filters/voxel_grid.h': No such file or directory
```

**Cause:** vcpkg_installed directory is incomplete or missing.  
**Fix:** Verify the include directory exists:
```cmd
dir "src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\include\pcl\filters\"
```
If missing, re-clone the repository fully.

---

### Error: "Cannot open source file '*.generated.cpp'"

```
error C1083: Cannot open source file: 'filters/VoxelGridOfXYZ.generated.cpp'
```

**Cause:** T4-generated files are missing from the repo.  
**Fix:** These files should already be present in the repo (they are committed, not generated at build time). Check if they exist:
```cmd
dir src\PclSharp.Extern\filters\*.generated.cpp
```
If missing, run the T4 templates in VS 2022: right-click each `.tt` file in the PclSharp C# project → "Run Custom Tool". This regenerates both `.cs` and `.generated.cpp` files.

---

### Error: Linker errors for PCL symbols (LNK2019)

```
error LNK2019: unresolved external symbol "..." referenced in function "..."
```

**Cause:** The vcpkg .lib files are not found by the linker.  
**Diagnosis:** Check that `Paths.props` includes the vcpkg lib directory. The vcpkg MSBuild integration (via `vcpkg integrate install`) should handle this automatically.  
**Fix:** Run from the vcpkg directory:
```cmd
src\PclSharp.Extern\vcpkg_installed\vcpkg\vcpkg.exe integrate install
```
Then rebuild.

---

### Warning: OpenNI2 not found (safe to ignore)

```
warning: cannot find OpenNI2.dll in x64/ — skipping
```

This is not a build warning but a runtime behavior: `LibraryLoader` silently skips `OpenNI2.dll` if it doesn't exist. OpenNI2 is only needed for Kinect depth cameras.

---

## Task 2.6 — Understanding the Generated Code

All C++ source files in `PclSharp.Extern` follow this pattern:

**Module directory:** `{module}/`  
**File naming:** `{ClassName}Of{PointType}.generated.cpp`  
**Example:** `filters/VoxelGridOfXYZ.generated.cpp`

Each file is generated by the corresponding T4 template in the C# project (`src/PclSharp/Filters/VoxelGrid.tt`). The T4 template produces **both** the C# wrapper code and the C++ export code simultaneously.

**Do not hand-edit `.generated.cpp` files** — they will be overwritten if the T4 template is re-run.

---

## Phase 2 Checklist

- [ ] VS 2022 Developer Command Prompt opened
- [ ] Navigated to repo root (`cd C:\Users\...\PclSharp`)
- [ ] vcpkg_installed packages verified (PCL headers and DLLs present)
- [ ] PlatformToolset confirmed as v143 for all configurations
- [ ] `msbuild` command completed with 0 errors
- [ ] `bin\x64\Debug\PclSharp.Extern.dll` exists and is > 1 MB
- [ ] `dumpbin /exports` confirms key PCL function exports present
