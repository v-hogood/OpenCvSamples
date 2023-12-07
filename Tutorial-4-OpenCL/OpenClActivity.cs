using Android.Content.PM;
using Android.Util;
using Android.Views;
using Java.Lang;
using OpenCV.Android;

namespace OpenCL;

[Activity(Name = "org.opencv.samples.opencl.OpenClActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
public class OpenClActivity : CameraActivity
{
    private const string Tag = "OCVSample::Activity";

    private MyGLSurfaceView mView;
    private TextView mProcMode;

    public class LoaderCallback : BaseLoaderCallback
    {
        public LoaderCallback(OpenClActivity activity) : base(activity)
        {
            this.activity = activity;
        }
        OpenClActivity activity;

        override public void OnManagerConnected(int status)
        {
            switch (status)
            {
                case ILoaderCallbackInterface.Success:
                    {
                        Log.Info(Tag, "OpenCV loaded successfully");

                        // Load native library after(!) OpenCV initialization
                        JavaSystem.LoadLibrary("JNIpart");

                        activity.mView.EnableView();
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

    override protected void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestWindowFeature(WindowFeatures.NoTitle);
        Window.AddFlags(WindowManagerFlags.Fullscreen);
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        RequestedOrientation = ScreenOrientation.Landscape;

        mLoaderCallback = new LoaderCallback(this);

        // mView = new MyGLSurfaceView(this, null);
        // SetContentView(mView);
        SetContentView(Resource.Layout.activity);
        mView = (MyGLSurfaceView)FindViewById(Resource.Id.my_gl_surface_view);
        mView.CameraTextureListener = mView;
        TextView tv = (TextView)FindViewById(Resource.Id.fps_text_view);
        mProcMode = (TextView)FindViewById(Resource.Id.proc_mode_text_view);
        RunOnUiThread(() =>
        {
            mProcMode.Text = "Processing mode: OpenCL via OpenCV";
        });

        mView.SetProcessingMode(NativePart.ProcessingModeOclOcv);
    }

    override protected void OnPause()
    {
        mView.OnPause();
        base.OnPause();
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
        mView.OnResume();
    }

    override public bool OnCreateOptionsMenu(IMenu menu)
    {
        MenuInflater inflater = MenuInflater;
        inflater.Inflate(Resource.Menu.menu, menu);
        return base.OnCreateOptionsMenu(menu);
    }

    override public bool OnOptionsItemSelected(IMenuItem item)
    {
        switch (item.ItemId)
        {
            case Resource.Id.no_proc:
                RunOnUiThread(() =>
                    mProcMode.Text = "Processing mode: No Processing");
                mView.SetProcessingMode(NativePart.ProcessingModeNoProcessing);
                return true;
            case Resource.Id.cpu:
                RunOnUiThread(() =>
                    mProcMode.Text = "Processing mode: CPU");
                mView.SetProcessingMode(NativePart.ProcessingModeCpu);
                return true;
            case Resource.Id.ocl_direct:
                RunOnUiThread(() =>
                    mProcMode.Text = "Processing mode: OpenCL direct");
                mView.SetProcessingMode(NativePart.ProcessingModeOclDirect);
                return true;
            case Resource.Id.ocl_ocv:
                RunOnUiThread(() =>
                    mProcMode.Text = "Processing mode: OpenCL via OpenCV");
                mView.SetProcessingMode(NativePart.ProcessingModeOclOcv);
                return true;
            default:
                return false;
        }
    }
}
