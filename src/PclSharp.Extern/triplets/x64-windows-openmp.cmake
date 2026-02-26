# Custom triplet: x64-windows with OpenMP enabled for PCL (e.g. GICP parallelization).
# Full copy of vcpkg's x64-windows.cmake plus /openmp so PCL's FindOpenMP enables OpenMP.
#
# Usage (from PclSharp.Extern):
#   vcpkg install --triplet x64-windows-openmp --overlay-triplets=triplets
#
# To force a clean rebuild of PCL with OpenMP (if pcl was already built without):
#   vcpkg remove pcl
#   vcpkg install --triplet x64-windows-openmp --overlay-triplets=triplets

# From vcpkg triplets/x64-windows.cmake
set(VCPKG_TARGET_ARCHITECTURE x64)
set(VCPKG_CRT_LINKAGE dynamic)
set(VCPKG_LIBRARY_LINKAGE dynamic)

# Enable OpenMP so PCL (GICP, etc.) can use parallelization; avoids
# "[pcl::...::setNumberOfThreads] OpenMP is not available!" warning.
set(VCPKG_CXX_FLAGS "${VCPKG_CXX_FLAGS} /openmp")
set(VCPKG_C_FLAGS "${VCPKG_C_FLAGS} /openmp")
