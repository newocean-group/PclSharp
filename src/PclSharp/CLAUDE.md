# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

PclSharp is a C# P/Invoke wrapper around the [Point Cloud Library (PCL)](https://pointclouds.org/). It wraps native PCL functionality (compiled into `PclSharp.Extern.dll`) so it can be called from .NET 4.5.2+.

## Building

Open `src/PclSharp.sln` in Visual Studio. The solution must be built as **x86** or **x64** — the main `PclSharp` project does not support AnyCPU because native DLL loading selects `x86/` or `x64/` subdirectories at runtime.

Build from the command line:
```
msbuild src\PclSharp.sln /p:Configuration=Release /p:Platform=x64
```

Output goes to `bin\x64\Release\` (or `bin\x86\Debug\`, etc.).

## Running tests

Tests use MSTest in `src/PclSharp.Test`. The post-build event copies the required native DLLs into the test output directory. Run tests via Visual Studio Test Explorer, or:
```
vstest.console.exe src\PclSharp.Test\bin\x64\Debug\PclSharp.Test.dll
```

## Architecture

### Two-layer structure

Every PCL object is exposed through two layers:

1. **C++ export layer** (`src/PclSharp.Extern/`): thin C-style wrapper functions (`EXPORT(T) prefix_funcname(...)`) that wrap PCL classes and are compiled into `PclSharp.Extern.dll`.

2. **C# P/Invoke layer** (`src/PclSharp/`): `[DllImport]` declarations collected in `static partial class Invoke`, plus C# wrapper classes that hold an `IntPtr _ptr` to the underlying native object.

### Base class hierarchy

- `DisposableObject` — implements `IDisposable` with `DisposeObject()` abstract method
- `UnmanagedObject : DisposableObject` — holds `protected IntPtr _ptr`; all PCL wrappers extend this
- `PclBase<PointT> : UnmanagedObject` — abstract base for algorithm classes that take an input cloud and indices

### Point types (`Struct/PointTypes.cs`)

Fixed-layout structs with `[StructLayout(LayoutKind.Explicit)]` matching PCL's memory layout:
- `PointXYZ` (16 bytes), `PointXYZRGBA` (32 bytes), `PointXYZL` (32 bytes)
- `Normal` (32 bytes), `PointNormal` (48 bytes)

### Native library loading (`Utils/LibraryLoader.cs`)

On first use of `Native` (via its static constructor), `LibraryLoader.LoadLibraries()` preloads `PclSharp.Extern.dll` and optional PCL runtime DLLs from the `x64/` or `x86/` subdirectory relative to the executing assembly. This ensures DllImport resolves to the correct architecture.

## T4 code generation

Most files are generated pairs. Each `.tt` template produces **both**:
- A `.cs` file (C# `Invoke` declarations + wrapper class)
- A `.generated.cpp` file in `../PclSharp.Extern/<module>/` (C++ export implementations)

The T4 infrastructure is in two shared includes:
- `Manager.ttinclude` — enables multi-file output from a single template
- `InvokeTemplate.ttinclude` — defines `InvokeT`, a helper that generates matched pairs of `[DllImport]` and `EXPORT(...)` declarations

`InvokeT` tracks every function name declared via `Func`/`FuncI`/`Prop` and removes each from the tracking set when its `Export`/`ExportI` counterpart is generated. If the sets don't match at the end (when `Dispose()` is called), T4 throws, catching C#/C++ mismatches at generation time rather than runtime.

### Regenerating templates

In Visual Studio: right-click a `.tt` file → **Run Custom Tool**. This regenerates both the `.cs` and the `.generated.cpp` output files. Both must be included in their respective projects.

### Adding a new module

1. Create `<Module>/<Class>.tt` following the existing pattern (see `Filters/VoxelGrid.tt` as a simple example)
2. Run the T4 template — it produces `<Class>.cs` and `../PclSharp.Extern/<module>/<Class>.generated.cpp`
3. Include the generated `.cs` in `PclSharp.csproj`
4. Include the generated `.cpp` in the `PclSharp.Extern` Visual C++ project

### Naming convention

C export function names follow `{module}_{classname}_{method}` using snake_case, e.g.:
- `pointcloud_xyz_ctor`, `pointcloud_xyz_size`, `pointcloud_xyz_at_colrow`
- `io_pcdreader_read_xyz`
- `pclbase_xyz_setInputCloud`
