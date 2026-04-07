# Prerequisites

This document describes all tools and environment setup required before beginning the build phases.

---

## 1. Required Tools

### 1.1 Visual Studio 2022

**Edition:** Community, Professional, or Enterprise (all editions work)
**Download:** https://visualstudio.microsoft.com/vs/

**Required Workloads** (select during installation or modify via VS Installer):

#### "Desktop development with C++"
This installs:
- MSVC v143 compiler toolset (required for building PclSharp.Extern.dll)
- Windows 10/11 SDK (WindowsTargetPlatformVersion 10.0)
- CMake tools for Windows (not strictly required but useful)
- C++ ATL/MFC (not required)

To verify it is installed:
1. Open VS 2022
2. Help → About → Installed Products: look for "VC++ 2022 Latest v143 tools"
Or via command line:
```
"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
```
If the batch file exists, the C++ tools are installed.

#### ".NET desktop development"
This installs:
- .NET Framework 4.8 targeting pack (required after upgrading PclSharp.csproj)
- WPF designer support

To verify .NET 4.8 targeting pack is installed:
```
dir "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
```
Should list `mscorlib.dll`, `System.dll`, `PresentationFramework.dll`, etc.

### 1.2 MSBuild 17.x

**Included with VS 2022.** Do not install a separate MSBuild — always use the one from VS 2022.

**Path:** `C:\Program Files\Microsoft Visual Studio\2022\<Edition>\MSBuild\Current\Bin\MSBuild.exe`

Easiest way to use it: open **VS 2022 Developer Command Prompt** (Start menu → "Developer Command Prompt for VS 2022"). This sets up PATH, INCLUDE, LIB, and all MSVC environment variables automatically.

To verify:
```
msbuild -version
```
Expected output starts with `17.x.x.x`.

### 1.3 NuGet CLI (optional)

NuGet package restore is handled automatically by MSBuild 17.x for `packages.config` projects. A separate NuGet CLI is only needed if automatic restore fails.

If needed, download `nuget.exe` from https://www.nuget.org/downloads and place it on PATH.

---

## 2. What Is NOT Required

| Tool | Why not needed |
|------|----------------|
| vcpkg (standalone install) | All PCL dependencies are pre-built in `vcpkg_installed/` |
| CMake | PCL's CMake build system is not used — vcpkg handles it |
| PCL SDK (system install) | Not required — vcpkg packages are self-contained |
| OpenNI2 SDK | Only needed for Kinect integration; not required for base PclSharp |
| VTK SDK (system install) | vcpkg provides VTK for visualization; not needed for basic PclSharp.dll |
| .NET 5/6/7/8 SDK | The project targets .NET Framework 4.8 only |
| Python | Not required |

---

## 3. Verifying the Repository Clone

The `vcpkg_installed/` directory is large (several GB). Verify it is complete:

```cmd
dir "src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\" | findstr "pcl_"
```

Expected output includes lines like:
```
pcl_common_release.dll
pcl_features_release.dll
pcl_filters_release.dll
pcl_io_release.dll
pcl_io_ply_release.dll
pcl_kdtree_release.dll
pcl_ml_release.dll
pcl_octree_release.dll
pcl_sample_consensus_release.dll
pcl_search_release.dll
pcl_segmentation_release.dll
pcl_surface_release.dll
```

If this directory is empty or missing, the repository was cloned without LFS or was a shallow clone. Re-clone with:
```
git clone --depth=0 <repo-url>
```
Or if LFS is used:
```
git lfs pull
```

---

## 4. Disk Space

| Component | Approximate Size |
|-----------|-----------------|
| vcpkg_installed (x64-windows-openmp) | 2–4 GB |
| vcpkg_installed (x64-windows baseline) | 500 MB |
| Build output (bin\x64\) | 200–500 MB |
| VS 2022 C++ workload | 5–10 GB |
| Total (excluding VS 2022) | ~5 GB |

Ensure at least 10 GB free on the drive containing the repository.

---

## 5. Opening a VS 2022 Developer Command Prompt

All build commands in this plan must be run from a **VS 2022 Developer Command Prompt** (not a regular cmd or PowerShell window), which sets all MSVC and MSBuild environment variables.

**Steps:**
1. Press Start, type "Developer Command Prompt"
2. Select "Developer Command Prompt for VS 2022"
3. Navigate to the repository root:
   ```
   cd C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp
   ```

**Verify environment:**
```cmd
msbuild -version
cl.exe /?
```
Both commands should succeed. `cl.exe` is the MSVC compiler.

---

## 6. Environment Setup Checklist

- [ ] VS 2022 installed with "Desktop development with C++" workload
- [ ] VS 2022 installed with ".NET desktop development" workload
- [ ] .NET Framework 4.8 targeting pack present in Reference Assemblies
- [ ] Repository fully cloned (vcpkg_installed/ is populated)
- [ ] At least 10 GB free disk space on repo drive
- [ ] VS 2022 Developer Command Prompt opens successfully
- [ ] `msbuild -version` returns 17.x in Developer Command Prompt
- [ ] `cl.exe /?` runs without error in Developer Command Prompt
