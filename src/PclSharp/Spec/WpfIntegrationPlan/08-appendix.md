# Appendix: Reference Material

## A. Complete PclSharp API Surface

All classes are in the `PclSharp` namespace tree. All implement `IDisposable` and must be used in `using` statements.

### PointCloud Module (`PclSharp.PointCloud`)

| Class | Description |
|-------|-------------|
| `PointCloudOfXYZ` | `pcl::PointCloud<pcl::PointXYZ>` |
| `PointCloudOfXYZRGBA` | `pcl::PointCloud<pcl::PointXYZRGBA>` |
| `PointCloudOfXYZL` | `pcl::PointCloud<pcl::PointXYZL>` |
| `PointCloudOfNormal` | `pcl::PointCloud<pcl::Normal>` |
| `PointCloudOfPointNormal` | `pcl::PointCloud<pcl::PointNormal>` |

Key methods: `Size`, `At(int)`, `PushBack(PointT)`, `Clear()`, `IsOrganized`, `Width`, `Height`

### Filters Module (`PclSharp.Filters`)

| Class | Description |
|-------|-------------|
| `VoxelGridOfXYZ` | Downsamples cloud using voxel grid |
| `StatisticalOutlierRemovalOfXYZ` | Removes statistical outliers |
| `ExtractIndicesOfXYZ` | Extracts a subset by index list |
| `HeightMap2DOfXYZ` | Creates a 2D height map |

### IO Module (`PclSharp.IO`)

| Class | Description |
|-------|-------------|
| `PCDReaderOfXYZ` | Reads `.pcd` files into `PointCloudOfXYZ` |
| `PCDReaderOfXYZRGBA` | Reads `.pcd` files into `PointCloudOfXYZRGBA` |
| `PCDWriterOfXYZ` | Writes `PointCloudOfXYZ` to `.pcd` |

### Search Module (`PclSharp.Search`)

| Class | Description |
|-------|-------------|
| `KdTreeOfXYZ` | KD-tree for XYZ nearest-neighbor search |
| `OrganizedNeighborOfXYZ` | Organized neighbor search (depth images) |

### Segmentation Module (`PclSharp.Segmentation`)

| Class | Description |
|-------|-------------|
| `EuclideanClusterExtractionOfXYZ` | Euclidean distance-based clustering |
| `SACSegmentationOfXYZ` | RANSAC-based model fitting |
| `SupervoxelClusteringOfXYZ` | Supervoxel segmentation |
| `SupervoxelClusteringOfXYZRGBA` | Supervoxel with color |
| `LCCPSegmentationOfXYZ` | Locally Convex Connected Patches |
| `CPCSegmentationOfXYZ` | Cut, Patch, and Connect segmentation |

### Registration Module (`PclSharp.Registration`)

| Class | Description |
|-------|-------------|
| `ICPOfPointXYZ_PointXYZ` | Iterative Closest Point (point-to-point) |
| `GICPOfPointXYZ_PointXYZ` | Generalized ICP |
| `TransformationEstimationPointToPlaneLLSOfPointNormal_PointNormal` | Point-to-plane LLS |

### Features Module (`PclSharp.Features`)

| Class | Description |
|-------|-------------|
| `FPFHEstimationOfPointXYZAndNormal` | Fast Point Feature Histograms |
| `IntegralImageNormalEstimationOfPointXYZAndNormal` | Normal estimation for organized clouds |

### Common Module (`PclSharp.Common`)

| Class | Description |
|-------|-------------|
| `PCAOfXYZ` | Principal Component Analysis |
| `ModelCoefficients` | Model coefficients (from SAC segmentation) |

### SampleConsensus Module (`PclSharp.SampleConsensus`)

| Class | Description |
|-------|-------------|
| `SampleConsensusModelPlaneOfXYZ` | Plane model for RANSAC |

### Surface Module (`PclSharp.Surface`)

| Class | Description |
|-------|-------------|
| `ConvexHullOfXYZ` | Convex hull computation |

### Eigen Module (`PclSharp.Eigen`)

| Class | Description |
|-------|-------------|
| `Matrix4` | 4x4 float matrix (transformation matrices) |
| `VectorXf` | Variable-length float vector |
| `Vector3i` | 3D integer vector |

### Std Module (`PclSharp.Std`)

| Class | Description |
|-------|-------------|
| `VectorOfXYZ` | `std::vector<pcl::PointXYZ>` |
| `VectorOfXYZRGBA` | `std::vector<pcl::PointXYZRGBA>` |
| `VectorOfFloat` | `std::vector<float>` |
| `VectorOfInt` | `std::vector<int>` |
| `VectorOfPointIndices` | `std::vector<pcl::PointIndices>` |
| `MultiMapOfuintAnduint` | `std::multimap<uint, uint>` |

### Point Structs (`PclSharp.Struct`)

Fixed-layout structs matching PCL memory layout (`[StructLayout(LayoutKind.Explicit)]`):

| Struct | Size | Fields |
|--------|------|--------|
| `PointXYZ` | 16 bytes | `float X, Y, Z` (+ 4 bytes padding) |
| `PointXYZRGBA` | 32 bytes | `float X, Y, Z` + `uint RGBA` |
| `PointXYZL` | 32 bytes | `float X, Y, Z` + `uint Label` |
| `Normal` | 32 bytes | `float NormalX, NormalY, NormalZ, Curvature` |
| `PointNormal` | 48 bytes | XYZ + Normal combined |

---

## B. C Export Naming Convention

All exported C functions follow the pattern: `{module}_{classname}_{method}` in `snake_case`.

**Examples:**

| C# Method | C Export |
|-----------|----------|
| `new PointCloudOfXYZ()` | `pointcloud_xyz_ctor()` |
| `cloud.Size` | `pointcloud_xyz_size(ptr)` |
| `cloud.At(0)` | `pointcloud_xyz_at(ptr, 0)` |
| `new VoxelGridOfXYZ()` | `filters_voxelGrid_xyz_ctor()` |
| `voxel.Filter(output)` | `filters_voxelGrid_xyz_filter(ptr, output)` |
| `new PCDReaderOfXYZ()` | `io_pcdreader_xyz_ctor()` |
| `reader.Read(path, cloud)` | `io_pcdreader_read_xyz(ptr, path, cloud)` |

This convention is enforced by the T4 `InvokeTemplate.ttinclude` — it tracks function names across C# and C++ declarations and throws at generation time if they don't match.

---

## C. T4 Template System

The majority of PclSharp's code is generated via T4 templates.

### Template pairs

Each `.tt` file produces **two** outputs simultaneously:
1. `{Class}.cs` — C# `[DllImport]` declarations and wrapper class
2. `../PclSharp.Extern/{module}/{Class}.generated.cpp` — C++ export implementations

### Infrastructure files

| File | Purpose |
|------|---------|
| `Manager.ttinclude` | Enables multi-file output from a single T4 template |
| `InvokeTemplate.ttinclude` | Defines `InvokeT` helper; generates matched C#/C++ pairs; validates name parity |

### Adding a new module

1. Create `{Module}/{Class}.tt` following the pattern in `Filters/VoxelGrid.tt`
2. Run the T4 template in VS 2022 (right-click `.tt` file → "Run Custom Tool")
3. Add the generated `{Class}.cs` to `PclSharp.csproj`
4. Add the generated `{Class}.generated.cpp` to `PclSharp.Extern.vcxproj`
5. Rebuild both projects

### When to regenerate

T4 templates only need to be run when **adding new PCL functionality**. For a standard build from the existing source, the generated `.cs` and `.generated.cpp` files are already committed and do not need regeneration.

---

## D. Build Command Reference

| Task | Command |
|------|---------|
| Full solution (Debug\|x64) | `msbuild src\PclSharp.sln /p:Configuration=Debug /p:Platform=x64 /m` |
| Full solution (Release\|x64) | `msbuild src\PclSharp.sln /p:Configuration=Release /p:Platform=x64 /m` |
| C++ only | `msbuild src\PclSharp.Extern\PclSharp.Extern.vcxproj /p:Configuration=Debug /p:Platform=x64` |
| C# only | `msbuild src\PclSharp\PclSharp.csproj /p:Configuration=Debug /p:Platform=x64` |
| Run tests | `vstest.console.exe src\PclSharp.Test\bin\x64\Debug\PclSharp.Test.dll /Platform:x64` |
| Check exports | `dumpbin /exports bin\x64\Debug\PclSharp.Extern.dll` |
| Check DLL deps | `dumpbin /dependents bin\x64\Debug\PclSharp.Extern.dll` |
| Verify framework | `findstr "TargetFrameworkVersion" src\PclSharp\PclSharp.csproj` |

---

## E. References

- [Point Cloud Library (PCL) Documentation](https://pointclouds.org/documentation/)
- [vcpkg Documentation](https://vcpkg.io/en/docs/README.html)
- [Original PclSharp NuGet Package](https://www.nuget.org/packages/PclSharp/)
- [MSBuild Reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reference)
- [T4 Text Templates](https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates)
- [P/Invoke in C#](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
