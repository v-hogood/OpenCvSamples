using Android.Util;
using OpenCV.Calib3d;
using OpenCV.Core;
using OpenCV.ImgProc;
using Size = OpenCV.Core.Size;

namespace CameraCalibration
{
    public class CameraCalibrator
    {
        private const string Tag = "OCV::CameraCalibrator";

        private static readonly Size mPatternSize = new Size(4, 11);
        private static readonly int mCornersSize = (int)(mPatternSize.Width * mPatternSize.Height);
        private bool mPatternWasFound = false;
        private MatOfPoint2f mCorners = new MatOfPoint2f();
        private List<Mat> mCornersBuffer = new List<Mat>();
        private bool mIsCalibrated = false;

        private Mat mCameraMatrix = new Mat();
        private Mat mDistortionCoefficients = new Mat();
        private int mFlags;
        private double mRms;
        private double mSquareSize = 0.0181;
        private Size mImageSize;

        public CameraCalibrator(int width, int height)
        {
            mImageSize = new Size(width, height);
            mFlags = Calib3d.CalibFixPrincipalPoint +
                     Calib3d.CalibZeroTangentDist +
                     Calib3d.CalibFixAspectRatio +
                     Calib3d.CalibFixK4 +
                     Calib3d.CalibFixK5;
            Mat.Eye(3, 3, CvType.Cv64fc1).CopyTo(mCameraMatrix);
            mCameraMatrix.Put(0, 0, 1.0);
            Mat.Zeros(5, 1, CvType.Cv64fc1).CopyTo(mDistortionCoefficients);
            Log.Info(Tag, "Instantiated new " + this.GetType());
        }

        public void ProcessFrame(Mat grayFrame, Mat rgbaFrame)
        {
            FindPattern(grayFrame);
            RenderFrame(rgbaFrame);
        }

        public void Calibrate()
        {
            List<Mat> rvecs = new List<Mat>();
            List<Mat> tvecs = new List<Mat>();
            Mat reprojectionErrors = new Mat();
            List<Mat> objectPoints = new List<Mat>();
            objectPoints.Add(Mat.Zeros(mCornersSize, 1, CvType.Cv32fc3));
            CalcBoardCornerPositions(objectPoints[0]);
            for (int i = 1; i < mCornersBuffer.Count; i++)
            {
                objectPoints.Add(objectPoints[0]);
            }

            Calib3d.CalibrateCamera(objectPoints, mCornersBuffer, mImageSize,
                mCameraMatrix, mDistortionCoefficients, rvecs, tvecs, mFlags);

            mIsCalibrated = Core.CheckRange(mCameraMatrix) &&
                Core.CheckRange(mDistortionCoefficients);

            mRms = ComputeReprojectionErrors(objectPoints, rvecs, tvecs, reprojectionErrors);
            Log.Info(Tag, String.Format("Average re-projection error: %f", mRms));
            Log.Info(Tag, "Camera matrix: " + mCameraMatrix.Dump());
            Log.Info(Tag, "Distortion coefficients: " + mDistortionCoefficients.Dump());
        }

        public void ClearCorners()
        {
            mCornersBuffer.Clear();
        }

        private void CalcBoardCornerPositions(Mat corners)
        {
            const int cn = 3;
            float[] positions = new float[mCornersSize * cn];

            for (int i = 0; i < mPatternSize.Height; i++)
            {
                for (int j = 0; j < mPatternSize.Width * cn; j += cn)
                {
                    positions[(int) (i * mPatternSize.Width * cn + j + 0)] =
                        (2 * (j / cn) + i % 2) * (float) mSquareSize;
                    positions[(int) (i * mPatternSize.Width * cn + j + 1)] =
                        i * (float) mSquareSize;
                    positions[(int) (i * mPatternSize.Width * cn + j + 2)] = 0;
                }
            }
            corners.Create(mCornersSize, 1, CvType.Cv32fc3);
            corners.Put(0, 0, positions);
        }

        private double ComputeReprojectionErrors(List<Mat> objectPoints,
            List<Mat> rvecs, List<Mat> tvecs, Mat perViewErrors)
        {
            MatOfPoint2f cornersProjected = new MatOfPoint2f();
            double totalError = 0;
            double error;
            float[] viewErrors = new float[objectPoints.Count];

            MatOfDouble distortionCoefficients = new MatOfDouble(mDistortionCoefficients);
            int totalPoints = 0;
            for (int i = 0; i < objectPoints.Count; i++)
            {
                MatOfPoint3f points = new MatOfPoint3f(objectPoints[i]);
                Calib3d.ProjectPoints(points, rvecs[i], tvecs[i],
                    mCameraMatrix, distortionCoefficients, cornersProjected);
                error = Core.Norm(mCornersBuffer[i], cornersProjected, Core.NormL2);

                int n = objectPoints[i].Rows();
                viewErrors[i] = (float) Math.Sqrt(error * error / n);
                totalError  += error * error;
                totalPoints += n;
            }
            perViewErrors.Create(objectPoints.Count, 1, CvType.Cv32fc1);
            perViewErrors.Put(0, 0, viewErrors);

            return Math.Sqrt(totalError / totalPoints);
        }

        private void FindPattern(Mat grayFrame)
        {
            mPatternWasFound = Calib3d.FindCirclesGrid(grayFrame, mPatternSize,
                mCorners, Calib3d.CalibCbAsymmetricGrid);
        }

        public void AddCorners()
        {
            if (mPatternWasFound)
            {
                mCornersBuffer.Add(mCorners.Clone());
            }
        }

        private void DrawPoints(Mat rgbaFrame)
        {
            Calib3d.DrawChessboardCorners(rgbaFrame, mPatternSize, mCorners, mPatternWasFound);
        }

        private void RenderFrame(Mat rgbaFrame)
        {
            DrawPoints(rgbaFrame);

            Imgproc.PutText(rgbaFrame, "Captured: " + mCornersBuffer.Count, new Point(rgbaFrame.Cols() / 3 * 2, rgbaFrame.Rows() * 0.1),
                Imgproc.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));
        }

        public Mat CameraMatrix => mCameraMatrix;

        public Mat DistortionCoefficients => mDistortionCoefficients;

        public int CornersBufferSize => mCornersBuffer.Count;

        public double AvgReprojectionError => mRms;

        public bool IsCalibrated => mIsCalibrated;

        public void SetCalibrated() => mIsCalibrated = true;
    }
}
