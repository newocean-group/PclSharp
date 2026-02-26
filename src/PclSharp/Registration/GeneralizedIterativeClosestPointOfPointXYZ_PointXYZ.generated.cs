using System;
using System.Runtime.InteropServices;
using PclSharp.Struct;
using PclSharp.Eigen;
using PclSharp.Std;

namespace PclSharp.Registration
{
	public static partial class Invoke
	{
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern IntPtr registration_gicp_pointxyz_pointxyz_ctor();
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_delete(ref IntPtr ptr);

		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_align(IntPtr ptr, IntPtr output);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_alignGuess(IntPtr ptr, IntPtr output, IntPtr guess);

		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setMaximumIterations(IntPtr ptr, int value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern int registration_gicp_pointxyz_pointxyz_getMaximumIterations(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setUseReciprocalCorrespondences(IntPtr ptr, bool value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern bool registration_gicp_pointxyz_pointxyz_getUseReciprocalCorrespondences(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setMaxCorrespondenceDistance(IntPtr ptr, double value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern double registration_gicp_pointxyz_pointxyz_getMaxCorrespondenceDistance(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setRANSACOutlierRejectionThreshold(IntPtr ptr, double value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern double registration_gicp_pointxyz_pointxyz_getRANSACOutlierRejectionThreshold(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setTransformationEpsilon(IntPtr ptr, double value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern double registration_gicp_pointxyz_pointxyz_getTransformationEpsilon(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setEuclideanFitnessEpsilon(IntPtr ptr, double value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern double registration_gicp_pointxyz_pointxyz_getEuclideanFitnessEpsilon(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setInputSource(IntPtr ptr, IntPtr value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern IntPtr registration_gicp_pointxyz_pointxyz_getInputSource(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_setInputTarget(IntPtr ptr, IntPtr value);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern IntPtr registration_gicp_pointxyz_pointxyz_getInputTarget(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern bool registration_gicp_pointxyz_pointxyz_hasConverged(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern double registration_gicp_pointxyz_pointxyz_getFitnessScore(IntPtr ptr);
		[DllImport(Native.DllName, CallingConvention = Native.CallingConvention)]
		public static extern void registration_gicp_pointxyz_pointxyz_getFinalTransformation(IntPtr ptr, IntPtr output);
	}

	/// <summary>
	/// Generalized ICP (GICP) for PointXYZ to PointXYZ registration.
	/// Uses anisotropic cost functions; often more accurate than standard ICP.
	/// </summary>
	public class GeneralizedIterativeClosestPointOfPointXYZ_PointXYZ : IterativeClosestPoint<PointXYZ, PointXYZ>
	{
		public GeneralizedIterativeClosestPointOfPointXYZ_PointXYZ()
		{
			_ptr = Invoke.registration_gicp_pointxyz_pointxyz_ctor();
		}

		public override int MaximumIterations
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getMaximumIterations(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setMaximumIterations(_ptr, value); }
		}
		public override bool UseReciprocalCorrespondences
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getUseReciprocalCorrespondences(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setUseReciprocalCorrespondences(_ptr, value); }
		}
		public override double MaxCorrespondenceDistance
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getMaxCorrespondenceDistance(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setMaxCorrespondenceDistance(_ptr, value); }
		}
		public override double RANSACOutlierRejectionThreshold
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getRANSACOutlierRejectionThreshold(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setRANSACOutlierRejectionThreshold(_ptr, value); }
		}
		public override double TransformationEpsilon
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getTransformationEpsilon(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setTransformationEpsilon(_ptr, value); }
		}
		public override double EuclideanFitnessEpsilon
		{
			get { return Invoke.registration_gicp_pointxyz_pointxyz_getEuclideanFitnessEpsilon(_ptr); }
			set { Invoke.registration_gicp_pointxyz_pointxyz_setEuclideanFitnessEpsilon(_ptr, value); }
		}
		public override PointCloud<PointXYZ> InputSource
		{
			get { return new PointCloudOfXYZ(Invoke.registration_gicp_pointxyz_pointxyz_getInputSource(_ptr), true); }
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				if (_ptr == IntPtr.Zero) throw new ObjectDisposedException(GetType().Name);
				Invoke.registration_gicp_pointxyz_pointxyz_setInputSource(_ptr, value.Ptr);
			}
		}
		public override PointCloud<PointXYZ> InputTarget
		{
			get { return new PointCloudOfXYZ(Invoke.registration_gicp_pointxyz_pointxyz_getInputTarget(_ptr), true); }
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				if (_ptr == IntPtr.Zero) throw new ObjectDisposedException(GetType().Name);
				Invoke.registration_gicp_pointxyz_pointxyz_setInputTarget(_ptr, value.Ptr);
			}
		}

		public override bool HasConverged
			=> Invoke.registration_gicp_pointxyz_pointxyz_hasConverged(_ptr);

		public override double FitnessScore
			=> Invoke.registration_gicp_pointxyz_pointxyz_getFitnessScore(_ptr);

		public override Matrix4f FinalTransformation
		{
			get
			{
				var m = new Matrix4f();
				Invoke.registration_gicp_pointxyz_pointxyz_getFinalTransformation(_ptr, m.Ptr);
				return m;
			}
		}

		public override double TransformationRotationEpsilon
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override void Align(PointCloud<PointXYZ> output)
			=> Invoke.registration_gicp_pointxyz_pointxyz_align(_ptr, output);

		public override void Align(PointCloud<PointXYZ> output, Matrix4f guess)
			=> Invoke.registration_gicp_pointxyz_pointxyz_alignGuess(_ptr, output, guess);

		public override void SetIndices(VectorOfInt indices)
		{
			throw new NotImplementedException();
		}

		public override void SetInputCloud(PointCloud<PointXYZ> cloud)
		{
			this.InputSource = cloud;
		}

		public override ref PointXYZ this[int idx]
		{
			get { return ref this.Index(idx); }
		}

		protected override void DisposeObject()
		{
			Invoke.registration_gicp_pointxyz_pointxyz_delete(ref _ptr);
		}
	}
}
