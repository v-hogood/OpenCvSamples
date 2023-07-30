using Android.Content.Res;
using OpenCV.Calib3d;
using OpenCV.Core;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;
using Range = OpenCV.Core.Range;

namespace CameraCalibration
{
    abstract class FrameRender
    {
        protected CameraCalibrator mCalibrator;

        public abstract Mat Render(ICvCameraViewFrame inputFrame);
    }
    
    class PreviewFrameRender : FrameRender
    {
        override public Mat Render(ICvCameraViewFrame inputFrame)
        {
            return inputFrame.Rgba();
        }
    }

    class CalibrationFrameRender : FrameRender
    {
        public CalibrationFrameRender(CameraCalibrator calibrator)
        {
            mCalibrator = calibrator;
        }

        override public Mat Render(ICvCameraViewFrame inputFrame)
        {
            Mat rgbaFrame = inputFrame.Rgba();
            Mat grayFrame = inputFrame.Gray();
            mCalibrator.ProcessFrame(grayFrame, rgbaFrame);

            return rgbaFrame;
        }
    }

    class UndistortionFrameRender : FrameRender
    {
        public UndistortionFrameRender(CameraCalibrator calibrator)
        {
            mCalibrator = calibrator;
        }

        override public Mat Render(ICvCameraViewFrame inputFrame)
        {
            Mat renderedFrame = new Mat(inputFrame.Rgba().Size(), inputFrame.Rgba().Type());
            Calib3d.Undistort(inputFrame.Rgba(), renderedFrame,
                    mCalibrator.CameraMatrix, mCalibrator.DistortionCoefficients);

            return renderedFrame;
        }
    }

    class ComparisonFrameRender : FrameRender
    {
        private int mWidth;
        private int mHeight;
        private Resources mResources;
        public ComparisonFrameRender(CameraCalibrator calibrator, int width, int height, Resources resources)
        {
            mCalibrator = calibrator;
            mWidth = width;
            mHeight = height;
            mResources = resources;
        }

        override public Mat Render(ICvCameraViewFrame inputFrame)
        {
            Mat undistortedFrame = new Mat(inputFrame.Rgba().Size(), inputFrame.Rgba().Type());
            Calib3d.Undistort(inputFrame.Rgba(), undistortedFrame,
                mCalibrator.CameraMatrix, mCalibrator.DistortionCoefficients);

            Mat comparisonFrame = inputFrame.Rgba();
            undistortedFrame.ColRange(new Range(0, mWidth / 2)).CopyTo(comparisonFrame.ColRange(new Range(mWidth / 2, mWidth)));
            List<MatOfPoint> border = new List<MatOfPoint>();
            int shift = (int)(mWidth * 0.005);
            border.Add(new MatOfPoint(new Point(mWidth / 2 - shift, 0), new Point(mWidth / 2 + shift, 0),
                new Point(mWidth / 2 + shift, mHeight), new Point(mWidth / 2 - shift, mHeight)));
            Imgproc.FillPoly(comparisonFrame, border, new Scalar(255, 255, 255));

            Imgproc.PutText(comparisonFrame, mResources.GetString(Resource.String.original), new Point(mWidth * 0.1, mHeight * 0.1),
                Imgproc.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));
            Imgproc.PutText(comparisonFrame, mResources.GetString(Resource.String.undistorted), new Point(mWidth * 0.6, mHeight * 0.1),
                Imgproc.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));

            return comparisonFrame;
        }
    }

    class OnCameraFrameRender
    {
        private FrameRender mFrameRender;
        public OnCameraFrameRender(FrameRender frameRender)
        {
            mFrameRender = frameRender;
        }
        public Mat Render(ICvCameraViewFrame inputFrame)
        {
            return mFrameRender.Render(inputFrame);
        }
    }
}
