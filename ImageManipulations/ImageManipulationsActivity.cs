using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;
using Size = OpenCV.Core.Size;

namespace ImageManipulations
{
    [Activity(Name = "org.opencv.samples.imagemanipulations.ImageManipulationsActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class ImageManipulationsActivity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener2
    {
        private const string Tag = "OCVSample::Activity";

        public const int             ViewModeRgba      = 0;
        public const int             ViewModeHist      = 1;
        public const int             ViewModeCanny     = 2;
        public const int             ViewModeSepia     = 3;
        public const int             ViewModeSobel     = 4;
        public const int             ViewModeZoom      = 5;
        public const int             ViewModePixelize  = 6;
        public const int             ViewModePosterize = 7;

        private IMenuItem            mItemPreviewRGBA;
        private IMenuItem            mItemPreviewHist;
        private IMenuItem            mItemPreviewCanny;
        private IMenuItem            mItemPreviewSepia;
        private IMenuItem            mItemPreviewSobel;
        private IMenuItem            mItemPreviewZoom;
        private IMenuItem            mItemPreviewPixelize;
        private IMenuItem            mItemPreviewPosterize;
        private CameraBridgeViewBase mOpenCvCameraView;

        private Size                 mSize0;

        private Mat                  mIntermediateMat;
        private Mat                  mMat0;
        private MatOfInt[]           mChannels;
        private MatOfInt             mHistSize;
        private int                  mHistSizeNum = 25;
        private MatOfFloat           mRanges;
        private Scalar[]             mColorsRGB;
        private Scalar[]             mColorsHue;
        private Scalar               mWhilte;
        private Point                mP1;
        private Point                mP2;
        private float[]              mBuff;
        private Mat                  mSepiaKernel;

        public static int            viewMode = ViewModeRgba;

        public ImageManipulationsActivity()
        {
            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);
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

            SetContentView(Resource.Layout.image_manipulations_surface_view);

            mOpenCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.image_manipulations_activity_surface_view);
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
                mOpenCvCameraView.EnableView();
        }

        override protected IList<CameraBridgeViewBase> CameraViewList =>
            new List<CameraBridgeViewBase>(1) { mOpenCvCameraView };

        override protected void OnDestroy()
        {
            base.OnDestroy();
            if (mOpenCvCameraView != null)
                mOpenCvCameraView.DisableView();
        }

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            Log.Info(Tag, "called OnCreateOptionsMenu");
            mItemPreviewRGBA  = menu.Add("Preview RGBA");
            mItemPreviewHist  = menu.Add("Histograms");
            mItemPreviewCanny = menu.Add("Canny");
            mItemPreviewSepia = menu.Add("Sepia");
            mItemPreviewSobel = menu.Add("Sobel");
            mItemPreviewZoom  = menu.Add("Zoom");
            mItemPreviewPixelize  = menu.Add("Pixelize");
            mItemPreviewPosterize = menu.Add("Posterize");
            return true;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            Log.Info(Tag, "called OnOptionsItemSelected; selected item: " + item);
            if (item == mItemPreviewRGBA)
                viewMode = ViewModeRgba;
            if (item == mItemPreviewHist)
                viewMode = ViewModeHist;
            else if (item == mItemPreviewCanny)
                viewMode = ViewModeCanny;
            else if (item == mItemPreviewSepia)
                viewMode = ViewModeSepia;
            else if (item == mItemPreviewSobel)
                viewMode = ViewModeSobel;
            else if (item == mItemPreviewZoom)
                viewMode = ViewModeZoom;
            else if (item == mItemPreviewPixelize)
                viewMode = ViewModePixelize;
            else if (item == mItemPreviewPosterize)
                viewMode = ViewModePosterize;
            return true;
        }

        public void OnCameraViewStarted(int width, int height)
        {
            mIntermediateMat = new Mat();
            mSize0 = new Size();
            mChannels = new MatOfInt[] { new MatOfInt(0), new MatOfInt(1), new MatOfInt(2) };
            mBuff = new float[mHistSizeNum];
            mHistSize = new MatOfInt(mHistSizeNum);
            mRanges = new MatOfFloat(0f, 256f);
            mMat0  = new Mat();
            mColorsRGB = new Scalar[] { new Scalar(200, 0, 0, 255), new Scalar(0, 200, 0, 255), new Scalar(0, 0, 200, 255) };
            mColorsHue = new Scalar[] {
                    new Scalar(255, 0, 0, 255),   new Scalar(255, 60, 0, 255),  new Scalar(255, 120, 0, 255), new Scalar(255, 180, 0, 255), new Scalar(255, 240, 0, 255),
                    new Scalar(215, 213, 0, 255), new Scalar(150, 255, 0, 255), new Scalar(85, 255, 0, 255),  new Scalar(20, 255, 0, 255),  new Scalar(0, 255, 30, 255),
                    new Scalar(0, 255, 85, 255),  new Scalar(0, 255, 150, 255), new Scalar(0, 255, 215, 255), new Scalar(0, 234, 255, 255), new Scalar(0, 170, 255, 255),
                    new Scalar(0, 120, 255, 255), new Scalar(0, 60, 255, 255),  new Scalar(0, 0, 255, 255),   new Scalar(64, 0, 255, 255),  new Scalar(120, 0, 255, 255),
                    new Scalar(180, 0, 255, 255), new Scalar(255, 0, 255, 255), new Scalar(255, 0, 215, 255), new Scalar(255, 0, 85, 255),  new Scalar(255, 0, 0, 255)
            };
            mWhilte = Scalar.All(255);
            mP1 = new Point();
            mP2 = new Point();

            // Fill sepia kernel
            mSepiaKernel = new Mat(4, 4, CvType.Cv32f);
            mSepiaKernel.Put(0, 0, /* R */0.189f, 0.769f, 0.393f, 0f);
            mSepiaKernel.Put(1, 0, /* G */0.168f, 0.686f, 0.349f, 0f);
            mSepiaKernel.Put(2, 0, /* B */0.131f, 0.534f, 0.272f, 0f);
            mSepiaKernel.Put(3, 0, /* A */0.000f, 0.000f, 0.000f, 1f);
        }

        public void OnCameraViewStopped()
        {
            // Explicitly deallocate Mats
            if (mIntermediateMat != null)
                mIntermediateMat.Release();

            mIntermediateMat = null;
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            Mat rgba = inputFrame.Rgba();
            Size sizeRgba = rgba.Size();

            Mat rgbaInnerWindow;

            int rows = (int) sizeRgba.Height;
            int cols = (int) sizeRgba.Width;

            int left = cols / 8;
            int top = rows / 8;

            int width = cols * 3 / 4;
            int height = rows * 3 / 4;

            switch (ImageManipulationsActivity.viewMode)
            {
                case ImageManipulationsActivity.ViewModeRgba:
                    break;

                case ImageManipulationsActivity.ViewModeHist:
                    Mat hist = new Mat();
                    int thikness = (int) (sizeRgba.Width / (mHistSizeNum + 10) / 5);
                    if(thikness > 5) thikness = 5;
                    int offset = (int) ((sizeRgba.Width - (5*mHistSizeNum + 4*10)*thikness)/2);
                    // RGB
                    for(int c=0; c<3; c++)
                    {
                        Imgproc.CalcHist(new List<Mat>() { rgba }, mChannels[c], mMat0, hist, mHistSize, mRanges);
                        Core.Normalize(hist, hist, sizeRgba.Height/2, 0, Core.NormInf);
                        hist.Get(0, 0, mBuff);
                        for(int h=0; h<mHistSizeNum; h++)
                        {
                            mP1.X = mP2.X = offset + (c * (mHistSizeNum + 10) + h) * thikness;
                            mP1.Y = sizeRgba.Height-1;
                            mP2.Y = mP1.Y - 2 - (int)mBuff[h];
                            Imgproc.Line(rgba, mP1, mP2, mColorsRGB[c], thikness);
                        }
                    }
                    // Value and Hue
                    Imgproc.CvtColor(rgba, mIntermediateMat, Imgproc.ColorRgb2hsvFull);
                    // Value
                    Imgproc.CalcHist(new List<Mat>() { mIntermediateMat }, mChannels[2], mMat0, hist, mHistSize, mRanges);
                    Core.Normalize(hist, hist, sizeRgba.Height/2, 0, Core.NormInf);
                    hist.Get(0, 0, mBuff);
                    for(int h=0; h<mHistSizeNum; h++)
                    {
                        mP1.X = mP2.X = offset + (3 * (mHistSizeNum + 10) + h) * thikness;
                        mP1.Y = sizeRgba.Height-1;
                        mP2.Y = mP1.Y - 2 - (int)mBuff[h];
                        Imgproc.Line(rgba, mP1, mP2, mWhilte, thikness);
                    }
                    // Hue
                    Imgproc.CalcHist(new List<Mat>() { mIntermediateMat }, mChannels[0], mMat0, hist, mHistSize, mRanges);
                    Core.Normalize(hist, hist, sizeRgba.Height/2, 0, Core.NormInf);
                    hist.Get(0, 0, mBuff);
                    for(int h=0; h<mHistSizeNum; h++)
                    {
                        mP1.X = mP2.X = offset + (4 * (mHistSizeNum + 10) + h) * thikness;
                        mP1.Y = sizeRgba.Height-1;
                        mP2.Y = mP1.Y - 2 - (int)mBuff[h];
                        Imgproc.Line(rgba, mP1, mP2, mColorsHue[h], thikness);
                    }
                    break;

                case ImageManipulationsActivity.ViewModeCanny:
                    rgbaInnerWindow = rgba.Submat(top, top + height, left, left + width);
                    Imgproc.Canny(rgbaInnerWindow, mIntermediateMat, 80, 90);
                    Imgproc.CvtColor(mIntermediateMat, rgbaInnerWindow, Imgproc.ColorGray2bgra, 4);
                    rgbaInnerWindow.Release();
                    break;

                case ImageManipulationsActivity.ViewModeSobel:
                    Mat gray = inputFrame.Gray();
                    Mat grayInnerWindow = gray.Submat(top, top + height, left, left + width);
                    rgbaInnerWindow = rgba.Submat(top, top + height, left, left + width);
                    Imgproc.Sobel(grayInnerWindow, mIntermediateMat, CvType.Cv8u, 1, 1);
                    Core.ConvertScaleAbs(mIntermediateMat, mIntermediateMat, 10, 0);
                    Imgproc.CvtColor(mIntermediateMat, rgbaInnerWindow, Imgproc.ColorGray2bgra, 4);
                    grayInnerWindow.Release();
                    rgbaInnerWindow.Release();
                    break;

                case ImageManipulationsActivity.ViewModeSepia:
                    rgbaInnerWindow = rgba.Submat(top, top + height, left, left + width);
                    Core.Transform(rgbaInnerWindow, rgbaInnerWindow, mSepiaKernel);
                    rgbaInnerWindow.Release();
                    break;

                case ImageManipulationsActivity.ViewModeZoom:
                    Mat zoomCorner = rgba.Submat(0, rows / 2 - rows / 10, 0, cols / 2 - cols / 10);
                    Mat mZoomWindow = rgba.Submat(rows / 2 - 9 * rows / 100, rows / 2 + 9 * rows / 100, cols / 2 - 9 * cols / 100, cols / 2 + 9 * cols / 100);
                    Imgproc.Resize(mZoomWindow, zoomCorner, zoomCorner.Size(), 0, 0, Imgproc.InterLinearExact);
                    Size wsize = mZoomWindow.Size();
                    Imgproc.Rectangle(mZoomWindow, new Point(1, 1), new Point(wsize.Width - 2, wsize.Height - 2), new Scalar(255, 0, 0, 255), 2);
                    zoomCorner.Release();
                    mZoomWindow.Release();
                    break;

                case ImageManipulationsActivity.ViewModePixelize:
                    rgbaInnerWindow = rgba.Submat(top, top + height, left, left + width);
                    Imgproc.Resize(rgbaInnerWindow, mIntermediateMat, mSize0, 0.1, 0.1, Imgproc.InterNearest);
                    Imgproc.Resize(mIntermediateMat, rgbaInnerWindow, rgbaInnerWindow.Size(), 0, 0, Imgproc.InterNearest);
                    rgbaInnerWindow.Release();
                    break;

                case ImageManipulationsActivity.ViewModePosterize:
                    // Imgproc.CvtColor(rgbaInnerWindow, mIntermediateMat, Imgproc.ColorRgba2rgb);
                    // Imgproc.PyrMeanShiftFiltering(mIntermediateMat, mIntermediateMat, 5, 50);
                    // Imgproc.CvtColor(mIntermediateMat, rgbaInnerWindow, Imgproc.ColorRgb2rgba);
                    rgbaInnerWindow = rgba.Submat(top, top + height, left, left + width);
                    Imgproc.Canny(rgbaInnerWindow, mIntermediateMat, 80, 90);
                    rgbaInnerWindow.SetTo(new Scalar(0, 0, 0, 255), mIntermediateMat);
                    Core.ConvertScaleAbs(rgbaInnerWindow, mIntermediateMat, 1.0/16, 0);
                    Core.ConvertScaleAbs(mIntermediateMat, rgbaInnerWindow, 16, 0);
                    rgbaInnerWindow.Release();
                    break;
            }

            return rgba;
        }
    }
}
