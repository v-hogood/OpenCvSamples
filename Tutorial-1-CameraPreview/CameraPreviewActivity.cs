using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using static OpenCV.Android.CameraBridgeViewBase;

namespace CameraPreview
{
    [Activity(Name = "org.opencv.samples.camerapreview.CameraPreviewActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class CameraPreviewActivity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener2
    {
        private const string Tag = "OCVSample::Activity";

        private CameraBridgeViewBase mOpenCvCameraView;

        public CameraPreviewActivity()
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

            SetContentView(Resource.Layout.camerapreview_surface_view);

            mOpenCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.camerapreview_activity_java_surface_view);

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

        public void OnCameraViewStarted(int width, int height)
        {
        }

        public void OnCameraViewStopped()
        {
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            return inputFrame.Rgba();
        }
    }
}
