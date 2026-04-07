# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

PclSharp is a C# P/Invoke binding for the [Point Cloud Library (PCL)](https://pointclouds.org/). It exposes PCL functionality to C# via a native DLL bridge.

- **`PclSharp.Extern`** — C++ DLL (vcxproj) that wraps PCL with `extern "C"` exports using the `EXPORT(rettype)` macro from `export.h`. Built with Visual Studio 2022 (v143 toolset for Debug), targeting x64.
- **`PclSharp`** — C# class library (.NET 4.8) that P/Invokes into `PclSharp.Extern.dll`. The `Native` class holds the DLL name and calling convention used by all `[DllImport]` declarations.
- **`PclSharp.Test`** — MSTest project targeting x64.
- **`PclSharp.Vis` / `PclSharp.ExternVis`** — Visualization layer (VTK-based), same pattern as above.
- **`PclSharp.Kinect`** — Kinect sensor integration.

## Build

Open `src/PclSharp.sln` in **Visual Studio 2022** and build. The C++ project (`PclSharp.Extern`) must be built first for x64; the C# test project copies the resulting DLLs via post-build xcopy.

The C++ native dependencies are managed via **vcpkg** (manifest mode). The `vcpkg.json` declares `pcl` as a dependency. The active triplet for Debug|x64 is `x64-windows-openmp`. vcpkg restores automatically on first build if `VcpkgManifestInstall=true`.

Output DLLs go to `bin\x64\Debug\` or `bin\x64\Release\` (relative to repo root, i.e., one level above `src\`).

## Running Tests

Run via Visual Studio Test Explorer using `test.runsettings` (already set to x64). To run from CLI:

```
vstest.console src\PclSharp.Test\bin\Debug\PclSharp.Test.dll /Settings:src\PclSharp.Test\test.runsettings
```

## Code Generation with T4 Templates

Most glue code is **generated** — do not hand-edit `.generated.cs` or `.generated.cpp` files. Instead edit the corresponding `.tt` template and re-run it (right-click → "Run Custom Tool" in VS, or use `TextTransform.exe`).

Key templates and what they generate:
- `PclSharp/PclBaseOf.tt` → `PclBaseOfXYZ.generated.cs` + `PclSharp.Extern/common/PclBaseOfXYZ.generated.cpp`
- `PclSharp/PointCloud.tt` → `PointCloudOfXYZ.generated.cs`, etc.
- `PclSharp/Registration/ICP.tt` → `IterativeClosestPointOf*.generated.cs` + corresponding `.generated.cpp` in `PclSharp.Extern/registration/`
- `PclSharp/IO/PCDReader.tt`, `PCDWriter.tt` → reader/writer generated files

The `InvokeTemplate.ttinclude` file defines the `InvokeT` helper class used in all templates. It generates matching pairs of:
- C#: `[DllImport]` declarations in `Invoke` static class + extension methods on the managed wrapper
- C++: `EXPORT(rettype) funcprefix_funcname(WrapperType* ptr, ...)` implementations

**Naming convention for exported functions**: `{classname}_{funcname}`, e.g. `registration_icp_pointxyz_pointxyz_align`.

## Architecture

```
C# (PclSharp)                          C++ (PclSharp.Extern.dll)
──────────────────────────────────     ──────────────────────────────────────
UnmanagedObject (_ptr: IntPtr)    ←→   PCL object allocated on C++ heap
  └── PointCloud<T>                     pcl::PointCloud<PointXYZ>*, etc.
  └── PclBase<T>
  └── Registration<S,T>

Invoke.*  [DllImport]             ←→   extern "C" EXPORT functions
Native.DllName = "PclSharp.Extern.dll"
```

Point types (`PointXYZ`, `PointXYZRGBA`, `Normal`, `PointNormal`, `PointXYZL`) are C# `struct`s with `[StructLayout(LayoutKind.Explicit)]` that must match the PCL C++ struct layouts exactly. `PointSizes.cpp` / `PointSizeTest.cs` validate this at test time.

`LibraryLoader` (called from `Native` static constructor) uses `LoadLibrary` to explicitly load `PclSharp.Extern.dll` and its PCL dependency DLLs from an `x64/` or `x86/` subdirectory relative to the working directory before any P/Invoke occurs.

## Adding a New PCL Wrapper

1. Create (or copy) a `.tt` template file in the appropriate `PclSharp/` subdirectory.
2. Use `InvokeT` to declare the function signatures — this ensures both the C# `[DllImport]` stub and the C++ export signature are generated consistently.
3. Run the template to produce `.generated.cs` and `.generated.cpp`.
4. Add the `.generated.cpp` to `PclSharp.Extern.vcxproj` (the filters file too).
5. Implement any non-generated abstract methods in a hand-written `.cs` companion file.
