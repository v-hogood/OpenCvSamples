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

        public class LoaderCallback : BaseLoaderCallback
        {
            public LoaderCallback(CameraPreviewActivity activity) : base(activity)
            {
                this.activity = activity;
            }
            CameraPreviewActivity activity;

            override public void OnManagerConnected(int status)
            {
                switch (status)
                {
                    case ILoaderCallbackInterface.Success:
                        {
                            Log.Info(Tag, "OpenCV loaded successfully");
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

        public CameraPreviewActivity()
        {
            Log.Info(Tag, "Instantiated new " + this.Class);
            mLoaderCallback = new LoaderCallback(this);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

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
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(Tag, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, mLoaderCallback);
            }
            else
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
