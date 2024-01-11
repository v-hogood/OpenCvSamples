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

    override protected void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestWindowFeature(WindowFeatures.NoTitle);
        Window.AddFlags(WindowManagerFlags.Fullscreen);
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        RequestedOrientation = ScreenOrientation.Landscape;

        if (OpenCVLoader.InitLocal())
        {
            Log.Info(Tag, "OpenCV loaded successfully");

            // Load native library after(!) OpenCV initialization
            JavaSystem.LoadLibrary("JNIpart");
        }
        else
        {
            Log.Error(Tag, "OpenCV initialization failed!");
            Toast.MakeText(this, "OpenCV initialization failed!", ToastLength.Long).Show();
            return;
        }

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
