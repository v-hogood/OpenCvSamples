using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;
using Rect = OpenCV.Core.Rect;
using Size = OpenCV.Core.Size;
using View = Android.Views.View;

namespace ColorBlobDetection
{
    [Activity(Name = "org.opencv.samples.colorblobdetection.ColorBlobDetectionActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class ColorBlobDetectionActivity : CameraActivity,
        View.IOnTouchListener,
        CameraBridgeViewBase.ICvCameraViewListener2
    {
        private const string Tag = "OCVSample::Activity";

        private bool                 isColorSelected = false;
        private Mat                  rgba;
        private Scalar               blobColorRgba;
        private Scalar               blobColorHsv;
        private ColorBlobDetector    detector;
        private Mat                  spectrum;
        private Size                 spectrumSize;
        private Scalar               contourColor;

        private CameraBridgeViewBase openCvCameraView;

        public ColorBlobDetectionActivity()
        {
            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);

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

            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.color_blob_detection_surface_view);

            openCvCameraView = (CameraBridgeViewBase) FindViewById(Resource.Id.color_blob_detection_activity_surface_view);
            openCvCameraView.Visibility = ViewStates.Visible;
            openCvCameraView.SetCvCameraViewListener2(this);
        }

        override protected void OnPause()
        {
            base.OnPause();
            if (openCvCameraView != null)
                openCvCameraView.DisableView();
        }

        override protected void OnResume()
        {
            base.OnResume();
            if (openCvCameraView != null)
            {
                openCvCameraView.EnableView();
                openCvCameraView.SetOnTouchListener(this);
            }
        }

        override protected IList<CameraBridgeViewBase> CameraViewList =>
            new List<CameraBridgeViewBase>(1) { openCvCameraView };

        override protected void OnDestroy()
        {
            base.OnDestroy();
            if (openCvCameraView != null)
                openCvCameraView.DisableView();
        }

        public void OnCameraViewStarted(int width, int height)
        {
            rgba = new(height, width, CvType.Cv8uc4);
            detector = new();
            spectrum = new();
            blobColorRgba = new(255);
            blobColorHsv = new(255);
            spectrumSize = new(200, 64);
            contourColor = new(255,0,0,255);
        }

        public void OnCameraViewStopped()
        {
            rgba.Release();
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            int cols = rgba.Cols();
            int rows = rgba.Rows();

            int xOffset = (openCvCameraView.Width - cols) / 2;
            int yOffset = (openCvCameraView.Height - rows) / 2;

            int x = (int)e.GetX() - xOffset;
            int y = (int)e.GetY() - yOffset;

            Log.Info(Tag, "Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows)) return false;

            Rect touchedRect = new();

            touchedRect.X = (x > 4) ? x - 4 : 0;
            touchedRect.Y = (y > 4) ? y - 4 : 0;

            touchedRect.Width = (x + 4 < cols) ? x + 4 - touchedRect.X : cols - touchedRect.X;
            touchedRect.Height = (y + 4 < rows) ? y + 4 - touchedRect.Y : rows - touchedRect.Y;

            Mat touchedRegionRgba = rgba.Submat(touchedRect);

            Mat touchedRegionHsv = new Mat();
            Imgproc.CvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.ColorRgb2hsvFull);

            // Calculate average color of touched region
            blobColorHsv = Core.SumElems(touchedRegionHsv);
            int pointCount = touchedRect.Width * touchedRect.Height;
            for (int i = 0; i < blobColorHsv.Val.Count; i++)
                blobColorHsv.Val[i] /= pointCount;

            blobColorRgba = ConvertScalarHsv2Rgba(blobColorHsv);

            Log.Info(Tag, "Touched rgba color: (" + blobColorRgba.Val[0] + ", " + blobColorRgba.Val[1] +
                ", " + blobColorRgba.Val[2] + ", " + blobColorRgba.Val[3] + ")");

            detector.SetHsvColor(blobColorHsv);

            Imgproc.Resize(detector.Spectrum, spectrum, spectrumSize, 0, 0, Imgproc.InterLinearExact);

            isColorSelected = true;

            touchedRegionRgba.Release();
            touchedRegionHsv.Release();

            return false; // don't need subsequent touch events
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            rgba = inputFrame.Rgba();

            if (isColorSelected)
            {
                detector.Process(rgba);
                IList<MatOfPoint> contours = detector.Contours;
                Log.Info(Tag, "Contours count: " + contours.Count);
                Imgproc.DrawContours(rgba, contours, -1, contourColor);

                Mat colorLabel = rgba.Submat(4, 68, 4, 68);
                colorLabel.SetTo(blobColorRgba);

                Mat spectrumLabel = rgba.Submat(4, 4 + spectrum.Rows(), 70, 70 + spectrum.Cols());
                spectrum.CopyTo(spectrumLabel);
            }

            return rgba;
        }

        private Scalar ConvertScalarHsv2Rgba(Scalar hsvColor)
        {
            Mat pointMatRgba = new();
            Mat pointMatHsv = new(1, 1, CvType.Cv8uc3, hsvColor);
            Imgproc.CvtColor(pointMatHsv, pointMatRgba, Imgproc.ColorHsv2rgbFull, 4);

            return new(pointMatRgba.Get(0, 0));
        }
    }
}
