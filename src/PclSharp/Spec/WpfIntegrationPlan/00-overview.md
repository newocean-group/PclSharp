# PclSharp WPF Integration — Implementation Plan Overview

## Purpose

This implementation plan documents all steps required to:
1. Build PclSharp (a C# P/Invoke wrapper around the Point Cloud Library) from source
2. Upgrade it from .NET 4.5.2 to .NET Framework 4.8
3. Integrate the resulting DLLs into a WPF .NET Framework 4.8 application

This document is intended as a handover reference for the development team. All decisions, technical rationale, and step-by-step instructions are fully documented across the phase files in this folder.

---

## What PclSharp Provides

PclSharp exposes the following PCL algorithm families to .NET:

| Module | Classes |
|--------|---------|
| PointCloud | PointCloudOfXYZ, PointCloudOfXYZRGBA, PointCloudOfXYZL, PointCloudOfNormal, PointCloudOfPointNormal |
| Filters | VoxelGridOfXYZ, StatisticalOutlierRemovalOfXYZ, ExtractIndicesOfXYZ, HeightMap2DOfXYZ |
| IO | PCDReaderOfXYZ, PCDReaderOfXYZRGBA, PCDWriterOfXYZ |
| Search | KdTreeOfXYZ, OrganizedNeighborOfXYZ |
| Segmentation | EuclideanClusterExtractionOfXYZ, SACSegmentationOfXYZ, SupervoxelClusteringOfXYZ/XYZRGBA, LCCPSegmentationOfXYZ, CPCSegmentationOfXYZ |
| Registration | ICPOfPointXYZ_PointXYZ, GICPOfPointXYZ_PointXYZ, TransformationEstimationPointToPlaneLLS |
| Features | FPFHEstimationOfPointXYZAndNormal, IntegralImageNormalEstimationOfPointXYZAndNormal |
| Common | PCAOfXYZ, ModelCoefficients |
| SampleConsensus | SampleConsensusModelPlaneOfXYZ |
| Surface | ConvexHullOfXYZ |
| Eigen | Matrix4, VectorXf, Vector3i |
| Std | VectorOfXYZ, VectorOfXYZRGBA, VectorOfPointIndices, VectorOfFloat, MultiMap |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   WPF Application (.exe)                 │
│                   .NET Framework 4.8 / x64               │
└──────────────────────────┬──────────────────────────────┘
                           │ references
┌──────────────────────────▼──────────────────────────────┐
│                     PclSharp.dll                         │
│              C# Managed Wrapper Library                  │
│              .NET Framework 4.8 / x64                    │
│  • [DllImport] declarations (static partial class Invoke)│
│  • C# wrapper classes (IntPtr _ptr, IDisposable)         │
│  • LibraryLoader (preloads native DLLs on first use)     │
└──────────────────────────┬──────────────────────────────┘
                           │ P/Invoke (DllImport)
┌──────────────────────────▼──────────────────────────────┐
│                 PclSharp.Extern.dll                       │
│              Native C++ Export Layer                     │
│              Win64 / MSVC v143                           │
│  • C-style EXPORT(T) functions                           │
│  • Naming: {module}_{classname}_{method}                 │
│  • Wraps PCL C++ classes via raw pointer (new/delete)    │
└──────────────────────────┬──────────────────────────────┘
                           │ links against
┌──────────────────────────▼──────────────────────────────┐
│              PCL Runtime DLLs (x64)                      │
│  pcl_common_release.dll, pcl_filters_release.dll,        │
│  pcl_io_release.dll, pcl_kdtree_release.dll,             │
│  pcl_search_release.dll, pcl_segmentation_release.dll,   │
│  pcl_features_release.dll, pcl_surface_release.dll,      │
│  pcl_registration (via ICP), + Boost, FLANN transitive   │
│  Source: vcpkg (x64-windows-openmp triplet)              │
└─────────────────────────────────────────────────────────┘
```

---

## Key Decisions

### Decision 1: Target x64 only
**Rationale:** The vcpkg packages are built with the `x64-windows-openmp` triplet (enables OpenMP for PCL performance). An x86 triplet is not present. Building x86 would require re-running vcpkg with a new triplet — significant effort with no benefit for a modern WPF app.

### Decision 2: Upgrade PclSharp.csproj to .NET 4.8
**Rationale:** The WPF application targets .NET 4.8. While .NET 4.5.2 assemblies are technically compatible with .NET 4.8 hosts, upgrading PclSharp itself to 4.8 ensures: (a) consistent TargetFrameworkVersion across the solution, (b) no assembly binding redirect surprises, (c) access to .NET 4.8 BCL improvements.

### Decision 3: Fix LibraryLoader double-extension bug
**Rationale:** `Native.DllName = "PclSharp.Extern.dll"` already includes the `.dll` extension. LibraryLoader appends another `.dll`, making the preload path `x64/PclSharp.Extern.dll.dll` which doesn't exist. The fix removes the redundant suffix. Without the fix, `PclSharp.Extern.dll` is still found by normal Windows DLL search path — but the fix makes explicit preloading work correctly and is cleaner.

### Decision 4: Do not build AnyCPU
**Rationale:** `LibraryLoader` selects the `x64/` or `x86/` subdirectory based on `IntPtr.Size` at runtime. `PclSharp.Extern.dll` is a native x64 DLL — it cannot be loaded by an x86 process. WPF projects must have `<PlatformTarget>x64</PlatformTarget>` explicitly set.

### Decision 5: Use vcpkg pre-installed packages (do not re-run vcpkg)
**Rationale:** The `vcpkg_installed/` directory is already present in the repo with all PCL dependencies compiled. Re-running vcpkg would download and rebuild PCL (~1+ hour) unnecessarily. Developers should use the vendored packages as-is.

### Decision 6: v143 toolset for both Debug and Release builds
**Rationale:** The Release|x64 configuration in the vcxproj originally used `PlatformToolset v141` (VS 2017). VS 2022 installs v143. Using v141 for Release requires installing the VS 2017 Build Tools separately. The plan updates Release|x64 to use v143 to match the installed toolset.

---

## Phases Summary

| Phase | File | Description |
|-------|------|-------------|
| Prerequisites | `01-prerequisites.md` | Tools, VS workloads, environment setup |
| Phase 1 | `02-phase1-framework-upgrade.md` | Upgrade PclSharp to .NET 4.8, fix LibraryLoader bug |
| Phase 2 | `03-phase2-native-build.md` | Build PclSharp.Extern.dll (C++ native layer) |
| Phase 3 | `04-phase3-managed-build.md` | Build PclSharp.dll (C# managed layer) |
| Phase 4 | `05-phase4-wpf-integration.md` | Configure WPF project: references, platform, App.config |
| Phase 5 | `06-phase5-dll-deployment.md` | Deploy native DLLs to correct output directory layout |
| Phase 6 | `07-phase6-testing-verification.md` | Run tests, smoke test, troubleshoot |
| Appendix | `08-appendix.md` | API reference, naming conventions, T4 system |

---

## Constraints and Assumptions

- OS: Windows 10 or Windows 11, 64-bit
- Visual Studio 2022 (any edition) with "Desktop development with C++" and ".NET desktop development" workloads installed
- The repository is fully cloned including the large `vcpkg_installed/` directory (confirm with `dir src\PclSharp.Extern\vcpkg_installed\x64-windows-openmp\bin\ | find "pcl_"`)
- No internet access is required during the build (all dependencies are vendored)
- The WPF application is an existing project targeting .NET Framework 4.8 — not .NET 5/6/7/8
- OpenNI2 (Kinect depth camera) is not required for the base integration

---

## Repository Structure Reference

```
PclSharp/
├── src/
│   ├── PclSharp.sln                    ← Solution file (open in VS 2022)
│   ├── PclSharp/                       ← C# managed project
│   │   ├── PclSharp.csproj             ← MODIFY: upgrade to .NET 4.8
│   │   ├── Native.cs                   ← DllName constant, static constructor
│   │   ├── Utils/LibraryLoader.cs      ← MODIFY: fix double-extension bug
│   │   ├── Struct/PointTypes.cs        ← Fixed-layout point structs
│   │   ├── {Module}/*.cs               ← Generated + handwritten C# wrappers
│   │   └── Spec/WpfIntegrationPlan/    ← THIS PLAN
│   ├── PclSharp.Extern/                ← C++ native project
│   │   ├── PclSharp.Extern.vcxproj     ← MODIFY: v141→v143 for Release|x64
│   │   ├── vcpkg_installed/            ← Pre-built PCL + Boost + FLANN DLLs
│   │   └── {module}/*.generated.cpp    ← C export implementations
│   ├── PclSharp.Test/                  ← MSTest unit tests
│   └── packages/                       ← NuGet packages (packages.config)
└── bin/
    └── x64/
        ├── Debug/                      ← Build output (Debug)
        └── Release/                    ← Build output (Release)
```
