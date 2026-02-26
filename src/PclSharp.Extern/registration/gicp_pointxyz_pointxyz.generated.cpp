#pragma once
#include "..\export.h"
#include <stdio.h>
#include <pcl/point_types.h>
#include <pcl/registration/gicp.h>
#include <memory>

using namespace pcl;
using namespace pcl::registration;
using namespace std;
typedef Eigen::Matrix<float, 4, 4> Matrix4;
typedef GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ> classType;

#ifdef __cplusplus
extern "C" {
#endif

EXPORT(classType*) registration_gicp_pointxyz_pointxyz_ctor()
{ return new classType(); }

EXPORT(void) registration_gicp_pointxyz_pointxyz_delete(classType** ptr)
{
	delete *ptr;
	*ptr = NULL;
}

EXPORT(void) registration_gicp_pointxyz_pointxyz_align(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, PointCloud<PointXYZ>* output)
{ ptr->align(*output); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_alignGuess(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, PointCloud<PointXYZ>* output, Matrix4* guess)
{ ptr->align(*output, *guess); }

EXPORT(void) registration_gicp_pointxyz_pointxyz_setMaximumIterations(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, int value)
{ ptr->setMaximumIterations(value); }
EXPORT(int) registration_gicp_pointxyz_pointxyz_getMaximumIterations(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getMaximumIterations(); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_setUseReciprocalCorrespondences(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, int value)
{ ptr->setUseReciprocalCorrespondences(value != 0); }
EXPORT(int) registration_gicp_pointxyz_pointxyz_getUseReciprocalCorrespondences(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getUseReciprocalCorrespondences() ? 1 : 0; }
EXPORT(void) registration_gicp_pointxyz_pointxyz_setMaxCorrespondenceDistance(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, double value)
{ ptr->setMaxCorrespondenceDistance(value); }
EXPORT(double) registration_gicp_pointxyz_pointxyz_getMaxCorrespondenceDistance(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getMaxCorrespondenceDistance(); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_setRANSACOutlierRejectionThreshold(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, double value)
{ ptr->setRANSACOutlierRejectionThreshold(value); }
EXPORT(double) registration_gicp_pointxyz_pointxyz_getRANSACOutlierRejectionThreshold(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getRANSACOutlierRejectionThreshold(); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_setTransformationEpsilon(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, double value)
{ ptr->setTransformationEpsilon(value); }
EXPORT(double) registration_gicp_pointxyz_pointxyz_getTransformationEpsilon(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getTransformationEpsilon(); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_setEuclideanFitnessEpsilon(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, double value)
{ ptr->setEuclideanFitnessEpsilon(value); }
EXPORT(double) registration_gicp_pointxyz_pointxyz_getEuclideanFitnessEpsilon(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getEuclideanFitnessEpsilon(); }

EXPORT(void) registration_gicp_pointxyz_pointxyz_setInputSource(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, PointCloud<PointXYZ>* cloud)
{ ptr->setInputSource(std::shared_ptr<PointCloud<PointXYZ>>(cloud, [](PointCloud<PointXYZ>*) {})); }

EXPORT(PointCloud<PointXYZ>*) registration_gicp_pointxyz_pointxyz_getInputSource(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return std::const_pointer_cast<PointCloud<PointXYZ>>(ptr->getInputSource()).get(); }

EXPORT(void) registration_gicp_pointxyz_pointxyz_setInputTarget(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, PointCloud<PointXYZ>* cloud)
{ ptr->setInputTarget(std::shared_ptr<PointCloud<PointXYZ>>(cloud, [](PointCloud<PointXYZ>*) {})); }

EXPORT(PointCloud<PointXYZ>*) registration_gicp_pointxyz_pointxyz_getInputTarget(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return std::const_pointer_cast<PointCloud<PointXYZ>>(ptr->getInputTarget()).get(); }

EXPORT(bool) registration_gicp_pointxyz_pointxyz_hasConverged(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->hasConverged(); }
EXPORT(double) registration_gicp_pointxyz_pointxyz_getFitnessScore(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr)
{ return ptr->getFitnessScore(); }
EXPORT(void) registration_gicp_pointxyz_pointxyz_getFinalTransformation(GeneralizedIterativeClosestPoint<PointXYZ, PointXYZ>* ptr, Matrix4* output)
{ *output = ptr->getFinalTransformation(); }

#ifdef __cplusplus
}
#endif
