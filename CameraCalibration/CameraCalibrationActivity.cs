// This sample is based on "Camera calibration With OpenCV" tutorial:
// https://docs.opencv.org/4.x/d4/d94/tutorial_camera_calibration.html
//
// It uses standard OpenCV asymmetric circles grid pattern 11x4:
// https://github.com/opencv/opencv/blob/4.x/doc/acircles_pattern.png
// The results are the camera matrix and 5 distortion coefficients.
//
// Tap on highlighted pattern to capture pattern corners for calibration.
// Move pattern along the whole screen and capture data.
//
// When you've captured necessary amount of pattern corners (usually ~20 are enough),
// press "Calibrate" button for performing camera calibration.

using Android.Content.Res;
using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using static OpenCV.Android.CameraBridgeViewBase;

namespace CameraCalibration
{
    [Activity(Name = "org.opencv.samples.cameracalibration.CameraCalibrationActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class CameraCalibrationActivity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener2,
        View.IOnTouchListener
    {
        private const string Tag = "OCVSample::Activity";

        private CameraBridgeViewBase mOpenCvCameraView;
        private CameraCalibrator mCalibrator;
        private OnCameraFrameRender mOnCameraFrameRender;
        private IMenu mMenu;
        private int mWidth;
        private int mHeight;

        public class LoaderCallback : BaseLoaderCallback
        {
            public LoaderCallback(CameraCalibrationActivity activity) : base(activity)
            {
                this.activity = activity;
            }
            CameraCalibrationActivity activity;

            override public void OnManagerConnected(int status)
            {
                switch (status)
                {
                    case ILoaderCallbackInterface.Success:
                        {
                            Log.Info(Tag, "OpenCV loaded successfully");
                            activity.mOpenCvCameraView.SetOnTouchListener(activity);
                            activity.mOpenCvCameraView.EnableView();
                        }
                        break;
                    default:
                        {
                            base.OnManagerConnected(status);
                        }
                        break;
                }
            }
        }
        private BaseLoaderCallback mLoaderCallback;

        public CameraCalibrationActivity()
        {
            mLoaderCallback = new LoaderCallback(this);

            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.camera_calibration_surface_view);

            mOpenCvCameraView = (CameraBridgeViewBase) FindViewById(Resource.Id.camera_calibration_java_surface_view);
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
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(Tag, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, mLoaderCallback);
            } else
            {
                Log.Debug(Tag, "OpenCV library found inside package. Using it!");
                mLoaderCallback.OnManagerConnected(ILoaderCallbackInterface.Success);
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

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);
            MenuInflater.Inflate(Resource.Menu.calibration, menu);
            mMenu = menu;
            return true;
        }

        override public bool OnPrepareOptionsMenu (IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);
            menu.FindItem(Resource.Id.preview_mode).SetEnabled(true);
            if (mCalibrator != null && !mCalibrator.IsCalibrated)
            {
                menu.FindItem(Resource.Id.preview_mode).SetEnabled(false);
            }
            return true;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
            case Resource.Id.calibration:
                mOnCameraFrameRender =
                    new OnCameraFrameRender(new CalibrationFrameRender(mCalibrator));
                item.SetChecked(true);
                return true;
            case Resource.Id.undistortion:
                mOnCameraFrameRender =
                    new OnCameraFrameRender(new UndistortionFrameRender(mCalibrator));
                item.SetChecked(true);
                return true;
            case Resource.Id.comparison:
                mOnCameraFrameRender =
                    new OnCameraFrameRender(new ComparisonFrameRender(mCalibrator, mWidth, mHeight, Resources));
                item.SetChecked(true);
                return true;
            case Resource.Id.calibrate:
                Resources res = Resources;
                if (mCalibrator.CornersBufferSize < 2)
                {
                    (Toast.MakeText(this, res.GetString(Resource.String.more_samples), ToastLength.Short)).Show();
                    return true;
                }

                mOnCameraFrameRender = new OnCameraFrameRender(new PreviewFrameRender());

#pragma warning disable CA1422
                var calibrationProgress = new ProgressDialog(this);
                calibrationProgress.SetTitle(res.GetString(Resource.String.calibrating));
                calibrationProgress.SetMessage(res.GetString(Resource.String.please_wait));
                calibrationProgress.SetCancelable(false);
                calibrationProgress.Indeterminate = true;
                calibrationProgress.Show();
#pragma warning restore CA1422

                    Task.Run(() =>
                    mCalibrator.Calibrate()).
                ContinueWith(_ =>
                {
                    calibrationProgress.Dismiss();
                    mCalibrator.ClearCorners();
                    mOnCameraFrameRender = new OnCameraFrameRender(new CalibrationFrameRender(mCalibrator));
                    string resultMessage = (mCalibrator.IsCalibrated) ?
                        res.GetString(Resource.String.calibration_successful) + " " + mCalibrator.AvgReprojectionError :
                        res.GetString(Resource.String.calibration_unsuccessful);
                    (Toast.MakeText(this, resultMessage, ToastLength.Short)).Show();

                    if (mCalibrator.IsCalibrated)
                    {
                        CalibrationResult.Save(this,
                            mCalibrator.CameraMatrix, mCalibrator.DistortionCoefficients);
                    }
                });
                return true;
            default:
                return base.OnOptionsItemSelected(item);
            }
        }

        public void OnCameraViewStarted(int width, int height)
        {
            if (mWidth != width || mHeight != height)
            {
                mWidth = width;
                mHeight = height;
                mCalibrator = new CameraCalibrator(mWidth, mHeight);
                if (CalibrationResult.TryLoad(this, mCalibrator.CameraMatrix, mCalibrator.DistortionCoefficients))
                {
                    mCalibrator.SetCalibrated();
                }
                else
                {
                    if (mMenu != null && !mCalibrator.IsCalibrated)
                    {
                        mMenu.FindItem(Resource.Id.preview_mode).SetEnabled(false);
                    }
                }

                mOnCameraFrameRender = new OnCameraFrameRender(new CalibrationFrameRender(mCalibrator));
            }
        }

        public void OnCameraViewStopped()
        {
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            return mOnCameraFrameRender.Render(inputFrame);
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            Log.Debug(Tag, "OnTouch invoked");

            mCalibrator.AddCorners();
            return false;
        }
    }
}
