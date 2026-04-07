# Phase 4: WPF Application Integration

## Objective

Configure an existing WPF .NET Framework 4.8 project to reference PclSharp.dll, ensure the correct platform target, and wire up the NuGet dependencies.

**Estimated time:** 15–30 minutes  
**Risk:** Low to medium — requires careful platform configuration and path setup

---

## Background

PclSharp uses native DLLs that are architecture-specific (x64 only). The WPF application must be configured as x64 — not AnyCPU — to match. Failure to do this results in `BadImageFormatException` at runtime when the x64 native DLL cannot be loaded into an x86 process.

The `Native` static constructor fires on the first P/Invoke call and triggers `LibraryLoader.LoadLibraries()`. This means all native DLLs must be in the correct locations *before* any PclSharp API is called.

---

## Task 4.1 — Set WPF Project Platform to x64

### Why AnyCPU Does Not Work

When a .NET process compiles as "AnyCPU" and runs on a 64-bit OS, it runs as a 64-bit process. In this case PclSharp would work. However:
- "AnyCPU (Prefer 32-bit)" — common WPF default — runs as 32-bit, which **cannot** load the x64 `PclSharp.Extern.dll`
- AnyCPU makes it ambiguous whether the process will be 32-bit or 64-bit
- Best practice: target x64 explicitly when using x64 native DLLs

### How to Set x64 in Visual Studio

1. Open your WPF project in VS 2022
2. Build → Configuration Manager...
3. In the "Active solution platform" dropdown, if x64 is not listed:
   - Click `<New...>`, select `x64`, copy from `Any CPU`, check "Create new project platforms"
4. For your WPF project row, set Platform = `x64`
5. Click Close, then save

### Direct .csproj edit

In your WPF application `.csproj`, ensure the following exists in each x64 configuration PropertyGroup:

```xml
<!-- Debug x64 -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
  <DebugSymbols>true</DebugSymbols>
  <OutputPath>bin\x64\Debug\</OutputPath>
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <DebugType>full</DebugType>
  <PlatformTarget>x64</PlatformTarget>
  <ErrorReport>prompt</ErrorReport>
</PropertyGroup>

<!-- Release x64 -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
  <OutputPath>bin\x64\Release\</OutputPath>
  <DefineConstants>TRACE</DefineConstants>
  <Optimize>true</Optimize>
  <DebugType>pdbonly</DebugType>
  <PlatformTarget>x64</PlatformTarget>
  <ErrorReport>prompt</ErrorReport>
</PropertyGroup>
```

**Critical:** `<PlatformTarget>x64</PlatformTarget>` must be present. Without it, even if the solution platform is x64, the project may default to AnyCPU.

---

## Task 4.2 — Add PclSharp.dll Reference

### Option A: Direct HintPath Reference (Recommended)

Add to your WPF `.csproj` (inside an `<ItemGroup>`):

```xml
<ItemGroup>
  <Reference Include="PclSharp">
    <HintPath>..\PclSharp\bin\x64\Debug\PclSharp.dll</HintPath>
    <Private>True</Private>
  </Reference>
</ItemGroup>
```

Adjust the `<HintPath>` to be the correct relative path from your WPF project directory to the PclSharp build output.

**Example paths** (adjust based on your folder structure):
- If WPF project is at `C:\MyApp\MyApp.csproj` and PclSharp is at `C:\Users\lamng\...\PclSharp\bin\x64\Debug\PclSharp.dll`:
  - Use an absolute path for initial setup: `<HintPath>C:\Users\lamng\OneDrive\Desktop\Newocean\PclSharp\PclSharp\bin\x64\Debug\PclSharp.dll</HintPath>`

### Option B: Project Reference (if in same solution)

If you add the PclSharp project to your WPF solution:
1. Right-click the WPF project → Add → Project Reference
2. Select PclSharp

Or in `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\PclSharp\src\PclSharp\PclSharp.csproj">
    <Project>{3795CB2E-FF81-4C7D-99C8-0589B7F1385D}</Project>
    <Name>PclSharp</Name>
  </ProjectReference>
</ItemGroup>
```

**Recommendation:** Use Option A if PclSharp is maintained as a separate library. Use Option B if you want a single solution build.

---

## Task 4.3 — Add NuGet Dependency References

PclSharp.dll depends on three NuGet packages at runtime. These must also be deployed with your WPF app. Add them to your WPF project's `packages.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <!-- existing packages... -->
  <package id="System.Numerics.Vectors" version="4.4.0" targetFramework="net48" />
  <package id="System.Runtime.CompilerServices.Unsafe" version="4.4.0" targetFramework="net48" />
  <package id="System.ValueTuple" version="4.4.0" targetFramework="net48" />
</packages>
```

Then add references to your WPF `.csproj`:

```xml
<ItemGroup>
  <Reference Include="System.Numerics.Vectors, Version=4.1.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a">
    <HintPath>..\packages\System.Numerics.Vectors.4.4.0\lib\portable-net45+win8+wp8+wpa81\System.Numerics.Vectors.dll</HintPath>
    <Private>True</Private>
  </Reference>
  <Reference Include="System.Runtime.CompilerServices.Unsafe, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a">
    <HintPath>..\packages\System.Runtime.CompilerServices.Unsafe.4.4.0\lib\netstandard1.0\System.Runtime.CompilerServices.Unsafe.dll</HintPath>
    <Private>True</Private>
  </Reference>
  <Reference Include="System.ValueTuple, Version=4.0.2.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51">
    <HintPath>..\packages\System.ValueTuple.4.4.0\lib\netstandard1.0\System.ValueTuple.dll</HintPath>
    <Private>True</Private>
  </Reference>
</ItemGroup>
```

**Alternative:** If these packages are already present in the GAC on .NET 4.8 (System.ValueTuple is inbox from .NET 4.7+), you may not need to deploy them separately. Test first — if no `FileNotFoundException` or binding errors occur at runtime, they are already available.

---

## Task 4.4 — App.config Configuration

Ensure your WPF `App.config` targets .NET 4.8:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <!-- Add binding redirects only if you see assembly version conflict errors -->
      <!-- Example: -->
      <!--
      <dependentAssembly>
        <assemblyIdentity name="System.ValueTuple" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.0.3.0" newVersion="4.0.2.0" />
      </dependentAssembly>
      -->
    </assemblyBinding>
  </runtime>
</configuration>
```

Only add `<bindingRedirect>` entries if you see `FileLoadException` with "version mismatch" in the message at runtime.

---

## Task 4.5 — Basic Usage Code Examples

### Smoke Test (add to App.xaml.cs OnStartup)

```csharp
using System.Windows;
using PclSharp.PointCloud;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Smoke test: triggers Native static constructor → LibraryLoader
        try
        {
            using (var cloud = new PointCloudOfXYZ())
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PclSharp] Loaded OK. Empty cloud size: {cloud.Size}");
            }
        }
        catch (System.DllNotFoundException ex)
        {
            MessageBox.Show(
                $"Native DLL not found: {ex.Message}\n\n" +
                "Ensure PclSharp.Extern.dll and PCL runtime DLLs are deployed correctly.",
                "PclSharp Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (System.BadImageFormatException ex)
        {
            MessageBox.Show(
                $"DLL architecture mismatch: {ex.Message}\n\n" +
                "Ensure the WPF project targets x64 (not AnyCPU or x86).",
                "PclSharp Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

### Loading a PCD File

```csharp
using PclSharp;
using PclSharp.PointCloud;
using PclSharp.IO;

public void LoadPointCloud(string pcdFilePath)
{
    using (var cloud = new PointCloudOfXYZ())
    using (var reader = new PCDReaderOfXYZ())
    {
        int result = reader.Read(pcdFilePath, cloud);
        if (result < 0)
            throw new System.InvalidOperationException($"Failed to read PCD file: {pcdFilePath}");
        
        int pointCount = cloud.Size;
        System.Diagnostics.Debug.WriteLine($"Loaded {pointCount} points from {pcdFilePath}");
    }
}
```

### VoxelGrid Downsampling

```csharp
using PclSharp.PointCloud;
using PclSharp.Filters;
using PclSharp.Struct;

public PointCloudOfXYZ DownsampleCloud(PointCloudOfXYZ inputCloud, float leafSize = 0.01f)
{
    var filtered = new PointCloudOfXYZ();  // caller must dispose
    using (var voxel = new VoxelGridOfXYZ())
    {
        voxel.SetInputCloud(inputCloud);
        voxel.LeafSize = new PointXYZ { X = leafSize, Y = leafSize, Z = leafSize };
        voxel.Filter(filtered);
    }
    return filtered;
}
```

### ICP Registration

```csharp
using PclSharp.PointCloud;
using PclSharp.Registration;
using PclSharp.Eigen;

public Matrix4 AlignClouds(PointCloudOfXYZ source, PointCloudOfXYZ target)
{
    using (var icp = new ICPOfPointXYZ_PointXYZ())
    using (var aligned = new PointCloudOfXYZ())
    {
        icp.SetInputSource(source);
        icp.SetInputTarget(target);
        icp.MaximumIterations = 50;
        icp.MaxCorrespondenceDistance = 0.05;
        icp.Align(aligned);
        
        bool converged = icp.HasConverged;
        double score = icp.FitnessScore;
        Matrix4 transform = icp.FinalTransformation;
        
        return transform;
    }
}
```

---

## Task 4.6 — Threading Considerations

### Thread Safety

- The `Native` static constructor is thread-safe (guaranteed by CLR static initialization)
- Individual PclSharp wrapper objects are **not** thread-safe — do not share instances across threads
- PCL algorithms can be called from background threads (e.g., `Task.Run`) — just don't share objects

### WPF Threading Pattern

Always perform PCL computation on a background thread, then marshal results to the UI thread:

```csharp
private async Task ProcessPointCloudAsync(string pcdPath)
{
    // Run PCL work on thread pool
    var points = await Task.Run(() =>
    {
        using (var cloud = new PointCloudOfXYZ())
        using (var reader = new PCDReaderOfXYZ())
        {
            reader.Read(pcdPath, cloud);
            // Extract data needed by UI
            return cloud.Size;
        }
    });
    
    // Update UI on dispatcher thread
    PointCountLabel.Text = $"Points: {points}";
}
```

---

## Phase 4 Checklist

- [ ] WPF project has `<PlatformTarget>x64</PlatformTarget>` in both Debug|x64 and Release|x64 PropertyGroups
- [ ] x64 platform added to solution configuration (Configuration Manager)
- [ ] PclSharp.dll reference added with correct HintPath
- [ ] System.ValueTuple.dll, System.Numerics.Vectors.dll, System.Runtime.CompilerServices.Unsafe.dll referenced
- [ ] App.config targets .NET Framework 4.8
- [ ] Smoke test code added to App.OnStartup
- [ ] Project builds without errors in x64|Debug configuration
