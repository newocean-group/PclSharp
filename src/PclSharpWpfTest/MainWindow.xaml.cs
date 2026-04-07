using System;
using System.IO;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PclSharp;
using PclSharp.Struct;
using PclSharp.Filters;
using PclSharp.Segmentation;
using PclSharp.SampleConsensus;
using PclSharp.Common;
using PclSharp.Search;
using PclSharp.Std;
using PclSharp.IO;
using PclSharp.Registration;
using PclSharp.Surface;

namespace PclSharpWpfTest
{
    public partial class MainWindow : Window
    {
        // ----------------------------------------------------------------
        // State
        // ----------------------------------------------------------------

        private PointCloudOfXYZ _cloud;
        private PointCloudOfXYZ _icpTargetCloud;

        // ----------------------------------------------------------------
        // 3D Viewport state
        // ----------------------------------------------------------------

        private WriteableBitmap _bitmap;
        private float[]         _zBuf;
        private double _azimuth   = 45.0;
        private double _elevation = 30.0;
        private double _distance  = 3.0;
        private Vector3 _cloudCenter;
        private Point _dragStart;
        private bool _isDragging;

        // ----------------------------------------------------------------
        // Init
        // ----------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private void AppendLog(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            TxtOutput.AppendText(line + Environment.NewLine);
            OutputScrollViewer.ScrollToBottom();
        }

        private void SetStatus(string text, bool ok)
        {
            TxtStatus.Text = text;
            TxtStatus.Foreground = ok
                ? new SolidColorBrush(Color.FromRgb(0x10, 0x7C, 0x10))
                : new SolidColorBrush(Color.FromRgb(0xC4, 0x23, 0x23));
        }

        // ----------------------------------------------------------------
        // 3D Viewport — rendering
        // ----------------------------------------------------------------

        /// <summary>
        /// Recomputes the cloud's centroid and auto-fits the camera distance.
        /// Must be called after loading a new cloud.
        /// </summary>
        private unsafe void RecomputeCenter()
        {
            if (_cloud == null || _cloud.Count == 0)
            {
                _cloudCenter = Vector3.Zero;
                _distance    = 3.0;
                return;
            }

            int n = _cloud.Count;
            PointXYZ* d = _cloud.Data;

            double sx = 0, sy = 0, sz = 0;
            for (int i = 0; i < n; i++) { sx += d[i].X; sy += d[i].Y; sz += d[i].Z; }

            _cloudCenter = new Vector3((float)(sx / n), (float)(sy / n), (float)(sz / n));

            float maxR = 0;
            for (int i = 0; i < n; i++)
            {
                float r = Vector3.Distance(new Vector3(d[i].X, d[i].Y, d[i].Z), _cloudCenter);
                if (r > maxR) maxR = r;
            }

            _distance = maxR * 2.5 + 0.1;
        }

        /// <summary>
        /// Software perspective projection of the current cloud into the WriteableBitmap.
        /// </summary>
        private unsafe void RenderCloud()
        {
            int W = (int)ViewportGrid.ActualWidth;
            int H = (int)ViewportGrid.ActualHeight;

            if (W < 10 || H < 10) return;

            if (_cloud == null || _cloud.Count == 0)
            {
                TxtNoCloud.Visibility    = Visibility.Visible;
                TxtCameraInfo.Text       = "—";
                CloudView.Source         = null;
                return;
            }

            TxtNoCloud.Visibility = Visibility.Collapsed;

            // Recreate bitmap and z-buffer if size changed
            if (_bitmap == null || _bitmap.PixelWidth != W || _bitmap.PixelHeight != H)
            {
                _bitmap = new WriteableBitmap(W, H, 96, 96, PixelFormats.Bgr32, null);
                _zBuf   = new float[W * H];
            }

            _bitmap.Lock();
            try
            {
                // Clear to dark background and reset z-buffer
                int* pBack = (int*)_bitmap.BackBuffer;
                int total  = W * H;
                for (int i = 0; i < total; i++) pBack[i] = 0x111111;
                for (int i = 0; i < total; i++) _zBuf[i]  = float.MaxValue;

                // Orbit camera
                double az  = _azimuth   * Math.PI / 180.0;
                double el  = _elevation * Math.PI / 180.0;
                float cosEl = (float)Math.Cos(el);
                float sinEl = (float)Math.Sin(el);
                float cosAz = (float)Math.Cos(az);
                float sinAz = (float)Math.Sin(az);

                var eye = new Vector3(
                    _cloudCenter.X + (float)_distance * cosEl * sinAz,
                    _cloudCenter.Y + (float)_distance * sinEl,
                    _cloudCenter.Z + (float)_distance * cosEl * cosAz);

                var fwd   = Vector3.Normalize(_cloudCenter - eye);
                var right = Vector3.Normalize(Vector3.Cross(fwd, Vector3.UnitY));
                var up    = Vector3.Cross(right, fwd);

                float focal = (float)Math.Min(W, H) * 0.9f;
                float halfW = W * 0.5f;
                float halfH = H * 0.5f;

                int n    = _cloud.Count;
                int step = Math.Max(1, n / 200000);   // decimate for performance
                PointXYZ* data = _cloud.Data;

                // First pass: find Z extents for color mapping
                float zMin = float.MaxValue, zMax = float.MinValue;
                for (int i = 0; i < n; i += step)
                {
                    float z = data[i].Z;
                    if (z < zMin) zMin = z;
                    if (z > zMax) zMax = z;
                }
                float zRange = zMax - zMin;
                if (zRange < 1e-6f) zRange = 1f; // guard against flat cloud

                for (int i = 0; i < n; i += step)
                {
                    var p = new Vector3(data[i].X, data[i].Y, data[i].Z) - eye;
                    float depth = Vector3.Dot(p, fwd);
                    if (depth < 0.001f) continue;

                    float inv = focal / depth;
                    int px = (int)(Vector3.Dot(p, right) * inv + halfW);
                    int py = (int)(-Vector3.Dot(p, up)   * inv + halfH);

                    if ((uint)px < (uint)W && (uint)py < (uint)H)
                    {
                        // Z-value rainbow colormap: blue (low) → cyan → green → yellow → red (high)
                        float t = (data[i].Z - zMin) / zRange;
                        float cr, cg, cb;
                        if (t < 0.25f) {
                            float s = t / 0.25f;
                            cr = 0f; cg = s; cb = 1f;
                        } else if (t < 0.5f) {
                            float s = (t - 0.25f) / 0.25f;
                            cr = 0f; cg = 1f; cb = 1f - s;
                        } else if (t < 0.75f) {
                            float s = (t - 0.5f) / 0.25f;
                            cr = s; cg = 1f; cb = 0f;
                        } else {
                            float s = (t - 0.75f) / 0.25f;
                            cr = 1f; cg = 1f - s; cb = 0f;
                        }

                        // Scale brightness relative to camera distance so it works at any scale
                        float dRef = (float)_distance;
                        float brightness = Math.Min(1.0f, (dRef * 0.75f) / (depth + dRef * 0.1f));
                        int r = (int)(cr * 255 * brightness);
                        int g = (int)(cg * 255 * brightness);
                        int b = (int)(cb * 255 * brightness);
                        int color = (r << 16) | (g << 8) | b;

                        // Depth-tested write: near points win over far points (z-buffer)
                        // Near points get a larger blob; far points a single pixel for extra depth cue
                        int blob = depth < dRef * 0.8f ? 2 : 1;
                        for (int dy2 = 0; dy2 < blob; dy2++)
                        {
                            int qy = py + dy2;
                            if ((uint)qy >= (uint)H) continue;
                            for (int dx2 = 0; dx2 < blob; dx2++)
                            {
                                int qx = px + dx2;
                                if ((uint)qx >= (uint)W) continue;
                                int idx = qy * W + qx;
                                if (depth < _zBuf[idx])
                                {
                                    _zBuf[idx]  = depth;
                                    pBack[idx]  = color;
                                }
                            }
                        }
                    }
                }

                _bitmap.AddDirtyRect(new Int32Rect(0, 0, W, H));
            }
            finally
            {
                _bitmap.Unlock();
            }

            CloudView.Source = _bitmap;

            int rendered = Math.Max(1, _cloud.Count / Math.Max(1, _cloud.Count / 200000));
            TxtCameraInfo.Text = $"Pts: {_cloud.Count:N0}  az={_azimuth:F1}°  el={_elevation:F1}°  dist={_distance:F2}";
        }

        // ----------------------------------------------------------------
        // 3D Viewport — mouse events
        // ----------------------------------------------------------------

        private void CloudView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _dragStart  = e.GetPosition(CloudView);
                _isDragging = true;
                CloudView.CaptureMouse();
            }
        }

        private void CloudView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var pos   = e.GetPosition(CloudView);
            double dx = pos.X - _dragStart.X;
            double dy = pos.Y - _dragStart.Y;
            _dragStart = pos;

            _azimuth   += dx * 0.5;
            _elevation  = Math.Max(-89.0, Math.Min(89.0, _elevation - dy * 0.5));

            RenderCloud();
        }

        private void CloudView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            CloudView.ReleaseMouseCapture();
        }

        private void CloudView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _distance *= (e.Delta > 0) ? 0.9 : 1.1;
            _distance  = Math.Max(0.001, _distance);
            RenderCloud();
        }

        private void CloudView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _bitmap = null;
            RenderCloud();
        }

        private void ViewportGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _bitmap = null;
            RenderCloud();
        }

        private void BtnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _azimuth   = 45.0;
            _elevation = 30.0;
            RecomputeCenter();
            RenderCloud();
        }

        // ----------------------------------------------------------------
        // 1. Create Point Cloud
        // ----------------------------------------------------------------

        private void BtnCreateCloud_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rng   = new Random(42);
                var cloud = new PointCloudOfXYZ();

                for (int i = 0; i < 1000; i++)
                {
                    cloud.Add(new PointXYZ
                    {
                        X = (float)rng.NextDouble(),
                        Y = (float)rng.NextDouble(),
                        Z = (float)rng.NextDouble()
                    });
                }

                _cloud?.Dispose();
                _cloud = cloud;

                AppendLog($"[OK] Created point cloud with {_cloud.Count} points.");
                SetStatus("Cloud created.", true);
                RecomputeCenter();
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Create Point Cloud: {ex.Message}");
                SetStatus("Create Point Cloud failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 2. VoxelGrid Filter
        // ----------------------------------------------------------------

        private void BtnVoxelGrid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded. Run 'Create Cloud' first.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                int before = _cloud.Count;
                PointCloudOfXYZ filtered;

                using (var vg = new VoxelGridOfXYZ())
                {
                    vg.SetInputCloud(_cloud);
                    vg.LeafSize = new PointXYZ { X = 0.05f, Y = 0.05f, Z = 0.05f };
                    filtered = new PointCloudOfXYZ();
                    vg.filter(filtered);
                }

                _cloud.Dispose();
                _cloud = filtered;

                AppendLog($"[OK] VoxelGrid: {before} → {_cloud.Count} points (leaf=0.05).");
                SetStatus("VoxelGrid filter applied.", true);
                RecomputeCenter();
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] VoxelGrid Filter: {ex.Message}");
                SetStatus("VoxelGrid filter failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 3. Statistical Outlier Removal
        // ----------------------------------------------------------------

        private void BtnSOR_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded. Run 'Create Cloud' first.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                int before = _cloud.Count;
                PointCloudOfXYZ clean;

                using (var sor = new StatisticalOutlierRemovalOfXYZ())
                {
                    sor.SetInputCloud(_cloud);
                    sor.MeanK           = 50;
                    sor.StdDevMulThresh = 1.0;
                    clean = new PointCloudOfXYZ();
                    sor.filter(clean);
                }

                _cloud.Dispose();
                _cloud = clean;

                AppendLog($"[OK] StatOutlierRemoval: {before} → {_cloud.Count} pts (meanK=50, stddev=1.0).");
                SetStatus("Statistical Outlier Removal applied.", true);
                RecomputeCenter();
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Statistical Outlier Removal: {ex.Message}");
                SetStatus("Statistical Outlier Removal failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 4. RANSAC Plane Segmentation
        // ----------------------------------------------------------------

        private void BtnRANSAC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                using (var seg    = new SACSegmentationOfXYZ())
                using (var inliers = new PointIndices())
                using (var coeffs  = new ModelCoefficients())
                {
                    seg.SetInputCloud(_cloud);
                    seg.OptimizeCoefficients = true;
                    seg.ModelType            = SACModel.Plane;
                    seg.MethodType           = SACMethod.RANSAC;
                    seg.DistanceThreshold    = 0.01;
                    seg.MaxIterations        = 100;
                    seg.Segment(inliers, coeffs);

                    int inlierCount = inliers.Indices.Count;

                    if (coeffs.Values.Count >= 4)
                    {
                        float a = coeffs.Values[0], b = coeffs.Values[1],
                              c = coeffs.Values[2], dd = coeffs.Values[3];
                        AppendLog($"[OK] RANSAC plane: {a:F4}x + {b:F4}y + {c:F4}z + {dd:F4} = 0, inliers={inlierCount}.");
                    }
                    else
                    {
                        AppendLog($"[OK] RANSAC ran — no coefficients returned (inliers={inlierCount}).");
                    }

                    if (inlierCount == 0)
                        AppendLog("[INFO] Zero inliers — cloud may be too sparse for this threshold.");
                }

                SetStatus("RANSAC segmentation complete.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] RANSAC Plane Segmentation: {ex.Message}");
                SetStatus("RANSAC segmentation failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 5. Euclidean Cluster Extraction
        // ----------------------------------------------------------------

        private void BtnClustering_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                int clusterCount;

                using (var tree     = new KdTreeOfXYZ())
                using (var ece      = new EuclideanClusterExtractionOfXYZ())
                using (var clusters = new VectorOfPointIndices())
                {
                    tree.SetInputCloud(_cloud);

                    ece.SetInputCloud(_cloud);
                    ece.SetSearchMethod(tree);
                    ece.ClusterTolerance = 0.02;
                    ece.MinClusterSize   = 10;
                    ece.MaxClusterSize   = 25000;
                    ece.Extract(clusters);

                    clusterCount = clusters.Count;
                }

                AppendLog($"[OK] Euclidean clustering: {clusterCount} cluster(s) (tol=0.02, min=10, max=25000).");
                SetStatus("Euclidean clustering complete.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Euclidean Clustering: {ex.Message}");
                SetStatus("Euclidean clustering failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // File I/O — Browse PCD
        // ----------------------------------------------------------------

        private void BtnBrowsePcd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title          = "Select a PCD file",
                    Filter         = "PCD files (*.pcd)|*.pcd|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() == true)
                {
                    TxtPcdPath.Text = dlg.FileName;
                    AppendLog($"[OK] Selected: {dlg.FileName}");
                    SetStatus("PCD file selected.", true);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Browse PCD: {ex.Message}");
                SetStatus("Browse PCD failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // File I/O — Load PCD
        // ----------------------------------------------------------------

        private void BtnLoadPcd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = TxtPcdPath.Text?.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    AppendLog("[INFO] No PCD path specified. Use Browse to pick a file.");
                    SetStatus("No PCD path provided.", false);
                    return;
                }

                var loaded = new PointCloudOfXYZ();
                int result;

                using (var reader = new PCDReader())
                    result = reader.Read(path, loaded);

                if (result == 0)
                {
                    _cloud?.Dispose();
                    _cloud = loaded;
                    AppendLog($"[OK] Loaded PCD '{System.IO.Path.GetFileName(path)}': {_cloud.Count} points.");
                    SetStatus("PCD loaded.", true);
                    RecomputeCenter();
                    RenderCloud();
                }
                else
                {
                    loaded.Dispose();
                    AppendLog($"[FAIL] Load PCD '{path}': PCDReader returned error code {result}.");
                    SetStatus("PCD load returned error.", false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Load PCD: {ex.Message}");
                SetStatus("Load PCD failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // File I/O — Browse OBJ
        // ----------------------------------------------------------------

        private void BtnBrowseObj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title          = "Select an OBJ file",
                    Filter         = "OBJ files (*.obj)|*.obj|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() == true)
                {
                    TxtObjPath.Text = dlg.FileName;
                    AppendLog($"[OK] Selected: {dlg.FileName}");
                    SetStatus("OBJ file selected.", true);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Browse OBJ: {ex.Message}");
                SetStatus("Browse OBJ failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // File I/O — Load OBJ (C# parser)
        // ----------------------------------------------------------------

        private void BtnLoadObj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = TxtObjPath.Text?.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    AppendLog("[INFO] No OBJ path specified. Use Browse to pick a file.");
                    SetStatus("No OBJ path provided.", false);
                    return;
                }

                var loaded = ParseObjFile(path);

                if (loaded.Count == 0)
                {
                    loaded.Dispose();
                    AppendLog("[WARN] OBJ file parsed but contained 0 vertex ('v') lines.");
                    SetStatus("OBJ load: zero vertices.", false);
                    return;
                }

                _cloud?.Dispose();
                _cloud = loaded;
                AppendLog($"[OK] Loaded OBJ '{System.IO.Path.GetFileName(path)}': {_cloud.Count} vertices.");
                SetStatus("OBJ loaded.", true);
                RecomputeCenter();
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Load OBJ: {ex.Message}");
                SetStatus("Load OBJ failed.", false);
            }
        }

        /// <summary>
        /// Parses an OBJ file and returns a PointCloudOfXYZ containing all 'v' vertices.
        /// Faces, normals, texture coords, and other directives are ignored.
        /// </summary>
        private PointCloudOfXYZ ParseObjFile(string path)
        {
            var cloud = new PointCloudOfXYZ();
            var sep   = new char[] { ' ', '\t' };

            using (var sr = File.OpenText(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;

                    var tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 4 || tokens[0] != "v") continue;

                    float x, y, z;
                    if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                        !float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                        !float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                    {
                        AppendLog($"[WARN] OBJ: skipped malformed vertex line: {line}");
                        continue;
                    }

                    cloud.Add(new PointXYZ { X = x, Y = y, Z = z });
                }
            }

            return cloud;
        }

        // ----------------------------------------------------------------
        // File I/O — Save as PCD
        // ----------------------------------------------------------------

        private void BtnSavePcd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null || _cloud.Count == 0)
                {
                    AppendLog("[INFO] No cloud to save. Load or create a cloud first.");
                    SetStatus("No cloud to save.", false);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title    = "Save cloud as PCD file",
                    Filter   = "PCD files (*.pcd)|*.pcd|All files (*.*)|*.*",
                    FileName = "output.pcd"
                };

                if (dlg.ShowDialog() != true) return;

                string path = dlg.FileName;
                int result;

                using (var writer = new PCDWriter())
                    result = writer.Write(path, _cloud);

                if (result == 0)
                {
                    TxtSavePath.Text = path;
                    AppendLog($"[OK] Saved PCD '{System.IO.Path.GetFileName(path)}': {_cloud.Count} points.");
                    SetStatus("PCD saved.", true);
                }
                else
                {
                    AppendLog($"[FAIL] Save PCD '{path}': PCDWriter returned error code {result}.");
                    SetStatus("PCD save returned error.", false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Save PCD: {ex.Message}");
                SetStatus("Save PCD failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 6. Cloud Statistics
        // ----------------------------------------------------------------

        private unsafe void BtnCloudStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                int n = _cloud.Count;
                if (n == 0) { AppendLog("[WARN] Cloud is empty."); return; }

                PointXYZ* data = _cloud.Data;

                float minX = data[0].X, maxX = data[0].X;
                float minY = data[0].Y, maxY = data[0].Y;
                float minZ = data[0].Z, maxZ = data[0].Z;
                double sumX = 0, sumY = 0, sumZ = 0;

                for (int i = 0; i < n; i++)
                {
                    float x = data[i].X, y = data[i].Y, z = data[i].Z;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                    if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
                    sumX += x; sumY += y; sumZ += z;
                }

                AppendLog($"[OK] Cloud statistics ({n:N0} points):");
                AppendLog($"     X: [{minX:F4}, {maxX:F4}]  range={maxX - minX:F4}");
                AppendLog($"     Y: [{minY:F4}, {maxY:F4}]  range={maxY - minY:F4}");
                AppendLog($"     Z: [{minZ:F4}, {maxZ:F4}]  range={maxZ - minZ:F4}");
                AppendLog($"     Centroid: ({sumX / n:F4}, {sumY / n:F4}, {sumZ / n:F4})");

                SetStatus("Cloud statistics computed.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Cloud Statistics: {ex.Message}");
                SetStatus("Cloud statistics failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 7. Convex Hull
        // ----------------------------------------------------------------

        private void BtnConvexHull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                using (var hull    = new ConvexHullOfXYZ())
                using (var mesh    = new PolygonMesh())
                using (var indices = new PointIndices())
                {
                    hull.SetInputCloud(_cloud);
                    hull.ComputeAreaVolume = true;
                    hull.Reconstruct(mesh);
                    hull.GetHullPointIndices(indices);

                    int    hullPts = indices.Indices.Count;
                    double area    = hull.TotalArea;
                    double volume  = hull.TotalVolume;

                    AppendLog($"[OK] Convex hull: hull_points={hullPts}, area={area:F4}, volume={volume:F4}");
                }

                SetStatus("Convex hull computed.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Convex Hull: {ex.Message}");
                SetStatus("Convex hull failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 8. ICP Self-Align Test
        // ----------------------------------------------------------------

        private unsafe void BtnIcpSelfTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No cloud loaded.");
                    SetStatus("No cloud available.", false);
                    return;
                }

                // Build a target that is the source shifted +0.05 on X
                var target  = new PointCloudOfXYZ();
                int n       = _cloud.Count;
                PointXYZ* d = _cloud.Data;
                for (int i = 0; i < n; i++)
                    target.Add(new PointXYZ { X = d[i].X + 0.05f, Y = d[i].Y, Z = d[i].Z });

                var aligned = new PointCloudOfXYZ();

                using (var icp = new IterativeClosestPointOfPointXYZ_PointXYZ())
                {
                    icp.MaximumIterations        = 50;
                    icp.MaxCorrespondenceDistance = 0.1;
                    icp.TransformationEpsilon     = 1e-8;
                    icp.EuclideanFitnessEpsilon   = 1e-6;
                    icp.InputSource = _cloud;
                    icp.InputTarget = target;
                    icp.Align(aligned);

                    bool   conv  = icp.HasConverged;
                    double score = icp.FitnessScore;
                    AppendLog($"[OK] ICP self-align: converged={conv}, fitnessScore={score:F6}");

                    var tf = icp.FinalTransformation;
                    AppendLog($"     T[0]: [{tf[0,0]:F4} {tf[0,1]:F4} {tf[0,2]:F4} {tf[0,3]:F4}]");
                    AppendLog($"     T[1]: [{tf[1,0]:F4} {tf[1,1]:F4} {tf[1,2]:F4} {tf[1,3]:F4}]");
                    AppendLog($"     T[2]: [{tf[2,0]:F4} {tf[2,1]:F4} {tf[2,2]:F4} {tf[2,3]:F4}]");
                    AppendLog($"     T[3]: [{tf[3,0]:F4} {tf[3,1]:F4} {tf[3,2]:F4} {tf[3,3]:F4}]");
                    tf.Dispose();
                }

                target.Dispose();
                aligned.Dispose();

                SetStatus("ICP self-align complete.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] ICP self-align: {ex.Message}");
                SetStatus("ICP self-align failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // Load ICP Target cloud
        // ----------------------------------------------------------------

        private void BtnLoadIcpTarget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title          = "Select ICP target cloud (PCD or OBJ)",
                    Filter         = "Point cloud files (*.pcd;*.obj)|*.pcd;*.obj|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() != true) return;

                string path = dlg.FileName;
                string ext  = System.IO.Path.GetExtension(path).ToLowerInvariant();
                PointCloudOfXYZ loaded;

                if (ext == ".pcd")
                {
                    loaded = new PointCloudOfXYZ();
                    int result;
                    using (var reader = new PCDReader())
                        result = reader.Read(path, loaded);
                    if (result != 0)
                    {
                        loaded.Dispose();
                        AppendLog($"[FAIL] Load ICP target PCD: error code {result}.");
                        SetStatus("ICP target load failed.", false);
                        return;
                    }
                }
                else if (ext == ".obj")
                {
                    loaded = ParseObjFile(path);
                }
                else
                {
                    AppendLog($"[INFO] Unsupported file type '{ext}'. Use .pcd or .obj.");
                    SetStatus("Unsupported file type.", false);
                    return;
                }

                if (loaded.Count == 0)
                {
                    loaded.Dispose();
                    AppendLog("[WARN] ICP target cloud has 0 points.");
                    SetStatus("ICP target is empty.", false);
                    return;
                }

                _icpTargetCloud?.Dispose();
                _icpTargetCloud = loaded;
                AppendLog($"[OK] ICP target loaded '{System.IO.Path.GetFileName(path)}': {_icpTargetCloud.Count} points.");
                SetStatus("ICP target loaded.", true);
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Load ICP target: {ex.Message}");
                SetStatus("ICP target load failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // 9. Run ICP (Source → Target)
        // ----------------------------------------------------------------

        private void BtnRunIcp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cloud == null)
                {
                    AppendLog("[INFO] No source cloud. Load or create a cloud first.");
                    SetStatus("No source cloud.", false);
                    return;
                }
                if (_icpTargetCloud == null)
                {
                    AppendLog("[INFO] No ICP target. Use 'Load ICP Target...' first.");
                    SetStatus("No ICP target.", false);
                    return;
                }

                var aligned = new PointCloudOfXYZ();

                using (var icp = new IterativeClosestPointOfPointXYZ_PointXYZ())
                {
                    icp.MaximumIterations        = 50;
                    icp.MaxCorrespondenceDistance = 0.05;
                    icp.TransformationEpsilon     = 1e-8;
                    icp.EuclideanFitnessEpsilon   = 1e-6;
                    icp.InputSource = _cloud;
                    icp.InputTarget = _icpTargetCloud;
                    icp.Align(aligned);

                    bool   conv  = icp.HasConverged;
                    double score = icp.FitnessScore;
                    AppendLog($"[OK] ICP: source={_cloud.Count:N0} pts, target={_icpTargetCloud.Count:N0} pts");
                    AppendLog($"     converged={conv}, fitnessScore={score:F6}");

                    var tf = icp.FinalTransformation;
                    AppendLog($"     T[0]: [{tf[0,0]:F4} {tf[0,1]:F4} {tf[0,2]:F4} {tf[0,3]:F4}]");
                    AppendLog($"     T[1]: [{tf[1,0]:F4} {tf[1,1]:F4} {tf[1,2]:F4} {tf[1,3]:F4}]");
                    AppendLog($"     T[2]: [{tf[2,0]:F4} {tf[2,1]:F4} {tf[2,2]:F4} {tf[2,3]:F4}]");
                    AppendLog($"     T[3]: [{tf[3,0]:F4} {tf[3,1]:F4} {tf[3,2]:F4} {tf[3,3]:F4}]");
                    tf.Dispose();
                }

                aligned.Dispose();

                SetStatus("ICP registration complete.", true);
                RenderCloud();
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Run ICP: {ex.Message}");
                SetStatus("ICP registration failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // Clear Output
        // ----------------------------------------------------------------

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtOutput.Clear();
                SetStatus("Output cleared.", true);
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Clear: {ex.Message}");
                SetStatus("Clear failed.", false);
            }
        }

        // ----------------------------------------------------------------
        // Run All Tests
        // ----------------------------------------------------------------

        private void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendLog("=== Run All: starting full test suite ===");

                BtnCreateCloud_Click(sender, e);
                BtnVoxelGrid_Click(sender, e);
                BtnSOR_Click(sender, e);
                BtnRANSAC_Click(sender, e);
                BtnClustering_Click(sender, e);
                BtnCloudStats_Click(sender, e);
                BtnConvexHull_Click(sender, e);
                BtnIcpSelfTest_Click(sender, e);

                AppendLog("=== Run All: complete ===");
                SetStatus("All tests completed.", true);
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Run All: {ex.Message}");
                SetStatus("Run All failed.", false);
            }
        }
    }
}
