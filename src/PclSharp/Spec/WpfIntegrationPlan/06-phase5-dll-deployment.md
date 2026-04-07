# Phase 5: Native DLL Deployment

## Objective

Ensure `PclSharp.Extern.dll` and all PCL runtime DLLs are copied to the correct locations in the WPF application output directory so that `LibraryLoader` can find and preload them at runtime.

**Estimated time:** 20–45 minutes (one-time setup; automated after that)  
**Risk:** Medium — missing or misplaced DLLs cause runtime failures; the errors are well-diagnosed

---

## Background: How DLLs Are Found at Runtime

When the WPF app starts and the first PclSharp API is called, the CLR triggers the `Native` static constructor, which calls `LibraryLoader.LoadLibraries()`:

```csharp
string dir = IntPtr.Size == 8 ? "x64/" : "x86/";   // always "x64/" for x64 app

// After Phase 1 bug fix:
loader.LoadLibrary("x64/PclSharp.Extern.dll");       // relative to CWD

// For each PCL runtime DLL:
if (File.Exists("x64/pcl_common_release.dll"))
    loader.LoadLibrary("x64/pcl_common_release.dll");
// ... (silently skips missing DLLs)
```

**Critical facts:**
1. Paths are **relative to the current working directory** — for WPF apps launched normally, CWD = the directory containing the exe
2. `PclSharp.Extern.dll` must be in the `x64/` subdirectory (after Phase 1 fix) **AND/OR** in the exe directory (for DllImport fallback)
3. PCL runtime DLLs must be in `x64/` subdirectory — the loader looks only there
4. Missing PCL runtime DLLs are silently skipped by LibraryLoader, but `PclSharp.Extern.dll` will fail to load at `DllImport` time with `DllNotFoundException`

---

## Required DLL Layout

```
YourWpfApp\bin\x64\Debug\           (or Release\)
├── YourWpfApp.exe
├── PclSharp.dll                     ← managed assembly (from bin\x64\Debug\)
├── PclSharp.pdb                     ← optional debug symbols
├── System.ValueTuple.dll            ← NuGet dep
├── System.Numerics.Vectors.dll      ← NuGet dep
├── System.Runtime.CompilerServices.Unsafe.dll  ← NuGet dep
└── x64\
    ├── PclSharp.Extern.dll          ← native wrapper (preloaded by LibraryLoader)
    ├── pcl_common_release.dll
    ├── pcl_filters_release.dll
    ├── pcl_io_release.dll
    ├── pcl_io_ply_release.dll
    ├── pcl_kdtree_release.dll
    ├── pcl_octree_release.dll
    ├── pcl_search_release.dll
    ├── pcl_features_release.dll
    ├── pcl_ml_release.dll
    ├── pcl_sample_consensus_release.dll
    ├── pcl_segmentation_release.dll
    ├── pcl_surface_release.dll
    └── (Boost and FLANN transitive DLLs — see Task 5.1)
```

---

## Task 5.1 — Inventory All Required DLLs

### Primary: PclSharp.Extern.dll

**Source:** `C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\bin\x64\Debug\PclSharp.Extern.dll`  
**Destination:** `YourWpfApp\bin\x64\Debug\x64\PclSharp.Extern.dll`

### PCL Core DLLs

**Source directory:** `C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\`

| DLL Name | Purpose |
|----------|---------|
| `pcl_common_release.dll` | Core PCL data structures |
| `pcl_octree_release.dll` | Octree spatial indexing |
| `pcl_kdtree_release.dll` | KD-tree search |
| `pcl_search_release.dll` | Search framework |
| `pcl_filters_release.dll` | VoxelGrid, StatisticalOutlierRemoval, etc. |
| `pcl_io_release.dll` | PCD file reader/writer |
| `pcl_io_ply_release.dll` | PLY file support (dependency of io) |
| `pcl_features_release.dll` | FPFH, normal estimation |
| `pcl_ml_release.dll` | Machine learning (dependency of features) |
| `pcl_sample_consensus_release.dll` | RANSAC, SAC models |
| `pcl_segmentation_release.dll` | Euclidean clustering, supervoxel |
| `pcl_surface_release.dll` | Convex hull, mesh construction |

### Transitive Dependencies (Boost, FLANN)

PCL depends on Boost and FLANN at runtime. These are also in `vcpkg_installed\x64-windows-openmp\bin\`. To find them, run:

```cmd
dumpbin /dependents "bin\x64\Debug\PclSharp.Extern.dll"
```

This lists the immediate DLL dependencies. Then for each listed DLL that starts with `boost_` or `flann`, it must also be deployed to `x64\`.

Common Boost DLLs required by PCL:
- `boost_system-vc143-mt-x64-1_XX.dll`
- `boost_filesystem-vc143-mt-x64-1_XX.dll`
- `boost_thread-vc143-mt-x64-1_XX.dll`
- `boost_date_time-vc143-mt-x64-1_XX.dll`
- `boost_chrono-vc143-mt-x64-1_XX.dll`
- `boost_atomic-vc143-mt-x64-1_XX.dll`
- `boost_regex-vc143-mt-x64-1_XX.dll`
- `boost_iostreams-vc143-mt-x64-1_XX.dll`

(where `XX` is the Boost version number from vcpkg)

**Tip:** Rather than listing each Boost DLL individually, use a wildcard copy: `xcopy /Y boost_*.dll "$(OutDir)x64\"`.

### Optional: OpenNI2

`OpenNI2.dll` is only needed for Kinect depth camera integration (`PclSharp.Kinect`). It is **not** required for basic `PclSharp.dll` usage. If you see this listed in `AdditionalLibraries`, it is safely skipped when the file doesn't exist.

### Note on "release" vs "debug" PCL DLLs

PCL libraries are built in Release mode by vcpkg (the `x64-windows-openmp` triplet). There are no debug variants. **Use the `_release.dll` files even in your Debug build configuration.** This is intentional — PCL's debug builds are extremely slow due to many assertions.

---

## Task 5.2 — Method A: Content Items in WPF .csproj (Recommended)

Add `<Content>` items to your WPF `.csproj`. This is the most reliable method — files are automatically copied on every build.

```xml
<ItemGroup>
  <!-- PclSharp.Extern.dll → x64\ subfolder -->
  <Content Include="C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\bin\x64\Debug\PclSharp.Extern.dll">
    <Link>x64\PclSharp.Extern.dll</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>

  <!-- PCL runtime DLLs → x64\ subfolder -->
  <Content Include="C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\pcl_common_release.dll">
    <Link>x64\pcl_common_release.dll</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="...\pcl_filters_release.dll">
    <Link>x64\pcl_filters_release.dll</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <!-- Repeat for all PCL DLLs listed in Task 5.1 -->

  <!-- Boost DLLs → x64\ subfolder (use actual filenames from vcpkg\bin\) -->
  <Content Include="...\boost_system-vc143-mt-x64-1_XX.dll">
    <Link>x64\boost_system-vc143-mt-x64-1_XX.dll</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <!-- Repeat for all Boost DLLs -->
</ItemGroup>
```

**Explanation of `<Link>`:** The `<Link>` element defines the virtual path within the output directory. Even though `<Include>` points to an absolute source path, the file appears in `$(OutDir)x64\` in the output.

**Advantage:** Declarative, version-controlled, works with both VS IDE and MSBuild CLI.

---

## Task 5.3 — Method B: Post-Build Event (Alternative)

Add a post-build event to the WPF project that xcopy the DLLs after each build. This is easier to maintain when the DLL list is large.

In VS 2022: Right-click WPF project → Properties → Build Events → Post-build event:

```cmd
if not exist "$(OutDir)x64\" mkdir "$(OutDir)x64\"

xcopy /Y /D "C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\bin\x64\$(Configuration)\PclSharp.Extern.dll" "$(OutDir)x64\"

xcopy /Y /D "C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\pcl_common_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_filters_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_io_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_io_ply_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_kdtree_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_octree_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_search_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_features_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_ml_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_sample_consensus_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_segmentation_release.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\pcl_surface_release.dll" "$(OutDir)x64\"

REM Boost DLLs (use wildcard — adjust path to vcpkg bin)
xcopy /Y /D "C:\...\vcpkg_installed\x64-windows-openmp\bin\boost_*.dll" "$(OutDir)x64\"
xcopy /Y /D "C:\...\vcpkg_installed\x64-windows-openmp\bin\flann*.dll" "$(OutDir)x64\"
```

In `.csproj` (PostBuildEvent):
```xml
<PropertyGroup>
  <PostBuildEvent>
    ... (script above) ...
  </PostBuildEvent>
</PropertyGroup>
```

---

## Task 5.4 — Method C: Manual Copy (Quick Testing)

For initial testing before automating, manually create the `x64\` folder and copy DLLs:

```cmd
set WPFOUT=C:\MyApp\bin\x64\Debug
set PCLBIN=C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp

mkdir "%WPFOUT%\x64"

copy "%PCLBIN%\bin\x64\Debug\PclSharp.Extern.dll" "%WPFOUT%\x64\"
copy "%PCLBIN%\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\pcl_*.dll" "%WPFOUT%\x64\"
copy "%PCLBIN%\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\boost_*.dll" "%WPFOUT%\x64\"
copy "%PCLBIN%\src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\flann*.dll" "%WPFOUT%\x64\"
```

---

## Task 5.5 — Diagnosing Missing DLLs

### Tool: Dependencies (modern Dependency Walker)

Download from: https://github.com/lucasg/Dependencies (free, open source)

```cmd
dependencies.exe -chain bin\x64\Debug\PclSharp.Extern.dll
```

This shows the full import chain and highlights any missing DLLs in red.

### Tool: Process Monitor (procmon.exe)

From Sysinternals Suite. Filter on:
- Process Name: `YourWpfApp.exe`
- Result: `NAME NOT FOUND` or `PATH NOT FOUND`
- Path contains: `.dll`

This shows exactly which DLL Windows tried to load and couldn't find.

### Runtime error interpretation

| Exception | Meaning | Fix |
|-----------|---------|-----|
| `DllNotFoundException: PclSharp.Extern.dll` | `PclSharp.Extern.dll` not in `x64\` or exe dir | Copy DLL to `x64\` |
| `DllNotFoundException: pcl_common_release.dll` | PCL runtime DLL missing from `x64\` | Copy PCL DLLs |
| `BadImageFormatException` | DLL is wrong architecture | Ensure x64 DLLs, x64 process |
| `EntryPointNotFoundException` | Function not exported from DLL | Rebuild C++ project |

---

## Phase 5 Checklist

- [ ] `YourWpfApp\bin\x64\Debug\x64\` directory exists after build
- [ ] `x64\PclSharp.Extern.dll` present (from PclSharp build output)
- [ ] All 12 `pcl_*_release.dll` files present in `x64\`
- [ ] Boost DLLs present in `x64\` (check with `dir x64\boost_*.dll`)
- [ ] FLANN DLL(s) present in `x64\`
- [ ] WPF app launches without `DllNotFoundException`
- [ ] `Environment.Is64BitProcess` returns `true` at runtime
