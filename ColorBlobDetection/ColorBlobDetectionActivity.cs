using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;
using Size = OpenCV.Core.Size;

namespace ColorBlobDetection
{
    [Activity(Name = "org.opencv.samples.colorblobdetection.ColorBlobDetectionActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class ColorBlobDetectionActivity : CameraActivity,
        View.IOnTouchListener,
        CameraBridgeViewBase.ICvCameraViewListener2
    {
        private const string Tag = "OCVSample::Activity";

        private bool              mIsColorSelected = false;
        private Mat               mRgba;
        private Scalar            mBlobColorRgba;
        private Scalar            mBlobColorHsv;
        private ColorBlobDetector mDetector;
        private Mat               mSpectrum;
        private Size              SpectrumSize;
        private Scalar            ContourColor;

        private CameraBridgeViewBase mOpenCvCameraView;

        public ColorBlobDetectionActivity()
        {
            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            if (OpenCVLoader.InitLocal())
            {
                Log.Info(Tag, "OpenCV loaded successfully");
            }
            else
            {
                Log.Error(Tag, "OpenCV initialization failed!");
                Toast.MakeText(this, "OpenCV initialization failed!", ToastLength.Long).Show();
                return;
            }

            SetContentView(Resource.Layout.color_blob_detection_surface_view);

            mOpenCvCameraView = (CameraBridgeViewBase) FindViewById(Resource.Id.color_blob_detection_activity_surface_view);
            mOpenCvCameraView.Visibility = ViewStates.Visible;
            mOpenCvCameraView.SetCvCameraViewListener2(this);
        }

        override protected void OnPause()
        {
            base.OnPause();
            if (mOpenCvCameraView != null)
                mOpenCvCameraView.DisableView();
        }

        override protected void OnResume()
        {
            base.OnResume();
            if (mOpenCvCameraView != null)
            {
                mOpenCvCameraView.SetOnTouchListener(this);
                mOpenCvCameraView.EnableView();
            }
        }

        override protected IList<CameraBridgeViewBase> CameraViewList =>
            new List<CameraBridgeViewBase>(1) { mOpenCvCameraView };

        override protected void OnDestroy()
        {
            base.OnDestroy();
            if (mOpenCvCameraView != null)
                mOpenCvCameraView.DisableView();
        }

        public void OnCameraViewStarted(int width, int height)
        {
            mRgba = new Mat(height, width, CvType.Cv8uc4);
            mDetector = new ColorBlobDetector();
            mSpectrum = new Mat();
            mBlobColorRgba = new Scalar(255);
            mBlobColorHsv = new Scalar(255);
            SpectrumSize = new Size(200, 64);
            ContourColor = new Scalar(255,0,0,255);
        }

        public void OnCameraViewStopped()
        {
            mRgba.Release();
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            int cols = mRgba.Cols();
            int rows = mRgba.Rows();

            int xOffset = (mOpenCvCameraView.Width - cols) / 2;
            int yOffset = (mOpenCvCameraView.Height - rows) / 2;

            int x = (int)e.GetX() - xOffset;
            int y = (int)e.GetY() - yOffset;

            Log.Info(Tag, "Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows)) return false;

            Rect touchedRect = new Rect();

            touchedRect.X = (x > 4) ? x - 4 : 0;
            touchedRect.Y = (y > 4) ? y - 4 : 0;

            touchedRect.Width = (x + 4 < cols) ? x + 4 - touchedRect.X : cols - touchedRect.X;
            touchedRect.Height = (y + 4 < rows) ? y + 4 - touchedRect.Y : rows - touchedRect.Y;

            Mat touchedRegionRgba = mRgba.Submat(touchedRect);

            Mat touchedRegionHsv = new Mat();
            Imgproc.CvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.ColorRgb2hsvFull);

            // Calculate average color of touched region
            mBlobColorHsv = Core.SumElems(touchedRegionHsv);
            int pointCount = touchedRect.Width * touchedRect.Height;
            for (int i = 0; i < mBlobColorHsv.Val.Count; i++)
                mBlobColorHsv.Val[i] /= pointCount;

            mBlobColorRgba = ConvertScalarHsv2Rgba(mBlobColorHsv);

            Log.Info(Tag, "Touched rgba color: (" + mBlobColorRgba.Val[0] + ", " + mBlobColorRgba.Val[1] +
                ", " + mBlobColorRgba.Val[2] + ", " + mBlobColorRgba.Val[3] + ")");

            mDetector.SetHsvColor(mBlobColorHsv);

            Imgproc.Resize(mDetector.Spectrum, mSpectrum, SpectrumSize, 0, 0, Imgproc.InterLinearExact);

            mIsColorSelected = true;

            touchedRegionRgba.Release();
            touchedRegionHsv.Release();

            return false; // don't need subsequent touch events
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            mRgba = inputFrame.Rgba();

            if (mIsColorSelected)
            {
                mDetector.Process(mRgba);
                IList<MatOfPoint> contours = mDetector.Contours;
                Log.Info(Tag, "Contours count: " + contours.Count);
                Imgproc.DrawContours(mRgba, contours, -1, ContourColor);

                Mat colorLabel = mRgba.Submat(4, 68, 4, 68);
                colorLabel.SetTo(mBlobColorRgba);

                Mat spectrumLabel = mRgba.Submat(4, 4 + mSpectrum.Rows(), 70, 70 + mSpectrum.Cols());
                mSpectrum.CopyTo(spectrumLabel);
            }

            return mRgba;
        }

        private Scalar ConvertScalarHsv2Rgba(Scalar hsvColor)
        {
            Mat pointMatRgba = new Mat();
            Mat pointMatHsv = new Mat(1, 1, CvType.Cv8uc3, hsvColor);
            Imgproc.CvtColor(pointMatHsv, pointMatRgba, Imgproc.ColorHsv2rgbFull, 4);

            return new Scalar(pointMatRgba.Get(0, 0));
        }
    }
}
