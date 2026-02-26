# Custom vcpkg triplets

## x64-windows-openmp

Builds for 64-bit Windows with **OpenMP** enabled so PCL can use parallelization (e.g. GICP).

### One-time setup

1. **Use this triplet when installing PCL**

   From `PclSharp.Extern` (or from repo root, adjust the overlay path):

   ```powershell
   vcpkg install pcl --triplet x64-windows-openmp --overlay-triplets=triplets
   ```

   If you use **manifest mode** (vcpkg reads `vcpkg.json` in this folder), set the overlay and triplet before running install, for example:

   ```powershell
   $env:VCPKG_OVERLAY_TRIPLETS = "$(Get-Location)\triplets"
   $env:VCPKG_DEFAULT_TRIPLET = "x64-windows-openmp"
   vcpkg install
   ```

2. **If PCL was already built without OpenMP**

   Remove it and reinstall so it is built with the new triplet:

   ```powershell
   vcpkg remove pcl
   vcpkg install pcl --triplet x64-windows-openmp --overlay-triplets=triplets
   ```

3. **Point your Extern project at the OpenMP build**

   Build/install using the same triplet (e.g. `x64-windows-openmp`) and use that vcpkg output when building PclSharp.Extern so it links against the OpenMP-enabled PCL.

After this, the PCL message *"Parallelization is requested, but OpenMP is not available!"* should stop and GICP will use multiple threads when available.
