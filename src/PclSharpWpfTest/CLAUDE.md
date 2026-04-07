# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

`PclSharpWpfTest` is a WPF application (.NET 4.8, x64) that serves as an interactive test harness and demo for the `PclSharp` library. It lets you load/save point clouds, run algorithms interactively, and visualize results in a built-in 3D viewport.

## Build

Part of `src/PclSharp.sln` — build from Visual Studio 2022 targeting **x64**. The post-build step xcopy's all native DLLs from `bin\x64\$(Configuration)\` into the output `x64\` subdirectory so `LibraryLoader` can find them at runtime.

## Architecture

**MainWindow** (`MainWindow.xaml` / `MainWindow.xaml.cs`) is the single window — no MVVM, no separate view models.

**Left panel** — grouped buttons that directly call `PclSharp` APIs and append results to the output log (`TxtOutput`).

**Right panel** — a software-rendered 3D viewport backed by a `WriteableBitmap` (`_bitmap`) and a float z-buffer (`_zBuf`). Rendering is done entirely in unsafe C# with a perspective projection (orbit camera: azimuth/elevation/distance). Points are colored by Z using a rainbow colormap; near points get a 2×2 blob, far points a single pixel.

**Camera state**: `_azimuth`, `_elevation`, `_distance`, `_cloudCenter`. `RecomputeCenter()` resets the camera to fit the loaded cloud. Mouse drag orbits, scroll wheel zooms.

**Algorithms exercised** (buttons 1–9):
1. Create synthetic cloud (1000 pts random sphere)
2. VoxelGrid downsampling (leaf = 0.05)
3. Statistical Outlier Removal
4. RANSAC plane segmentation
5. Euclidean clustering
6. Cloud statistics (min/max/mean XYZ)
7. Convex hull
8. ICP self-align (source = slightly perturbed copy of `_cloud`)
9. ICP source → separately loaded target (`_icpTargetCloud`)

**File I/O**: PCD load (`PCDReader`), OBJ vertex-only load (hand-parsed), PCD save (`PCDWriter`).

## Key Points

- `_cloud` is the current working cloud displayed in the viewport; most algorithm buttons mutate it in-place and call `RenderCloud()` afterwards.
- `AppendLog` / `SetStatus` are the only output mechanisms — no message boxes, no exceptions surfaced to the user outside of try/catch blocks.
- The viewport renderer decimates large clouds: renders at most ~200 000 points (`step = Max(1, n / 200_000)`).
- There are no tests for this project; it is purely a manual/exploratory test app.
