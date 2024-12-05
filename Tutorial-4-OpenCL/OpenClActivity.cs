using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using OpenCV.Android;

namespace OpenCL;

[Activity(Name = "org.opencv.samples.opencl.OpenClActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
public class OpenClActivity : CameraActivity
{
    private const string Tag = "OCVSample::Activity";

    private MyGLSurfaceView mView;
    private TextView mProcMode;

    private bool builtWithOpenCL = false;

    private IMenuItem mItemNoProc;
    private IMenuItem mItemCpu;
    private IMenuItem mItemOclDirect;
    private IMenuItem mItemOclOpenCV;

    override protected void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestWindowFeature(WindowFeatures.NoTitle);
        Window.AddFlags(WindowManagerFlags.Fullscreen);
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        RequestedOrientation = ScreenOrientation.Landscape;

        // mView = new MyGLSurfaceView(this, null);
        // SetContentView(mView);
        SetContentView(Resource.Layout.activity);
        mView = (MyGLSurfaceView)FindViewById(Resource.Id.my_gl_surface_view);
        mView.CameraTextureListener = mView;
        TextView tv = (TextView)FindViewById(Resource.Id.fps_text_view);
        mProcMode = (TextView)FindViewById(Resource.Id.proc_mode_text_view);
        builtWithOpenCL = NativePart.BuiltWithOpenCL(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)));
        if (builtWithOpenCL)
        {
            mProcMode.Text = "Processing mode: OpenCL direct";
            mView.SetProcessingMode(NativePart.ProcessingModeOclDirect);
        }
        else
        {
            mProcMode.Text = "Processing mode: CPU";
            mView.SetProcessingMode(NativePart.ProcessingModeCpu);
        }
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
        mItemNoProc = menu.Add("No processing");
        mItemCpu = menu.Add("Use CPU code");
        if (builtWithOpenCL)
        {
            mItemOclOpenCV = menu.Add("Use OpenCL via OpenCV");
            mItemOclDirect = menu.Add("Use OpenCL direct");
        }
        return base.OnCreateOptionsMenu(menu);
    }

    override public bool OnOptionsItemSelected(IMenuItem item)
    {
        string procName = "Not selected";
        int procMode = NativePart.ProcessingModeNoProcessing;

        if (item == mItemNoProc)
        {
            procMode = NativePart.ProcessingModeNoProcessing;
            procName = "Processing mode: No Processing";
        }
        else if (item == mItemCpu)
        {
            procMode = NativePart.ProcessingModeCpu;
            procName = "Processing mode: CPU";
        }
        else if (item == mItemOclOpenCV && builtWithOpenCL)
        {
            procMode = NativePart.ProcessingModeOclOcv;
            procName = "Processing mode: OpenCL via OpenCV (TAPI)";
        }
        else if (item == mItemOclDirect && builtWithOpenCL)
        {
            procMode = NativePart.ProcessingModeOclDirect;
            procName = "Processing mode: OpenCL direct";
        }

        mView.SetProcessingMode(procMode);
        mProcMode.Text = procName;

        return true;
    }

    override public void OnOptionsMenuClosed(IMenu menu)
    {
        base.OnOptionsMenuClosed(menu);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            // Workaround for https://issuetracker.google.com/issues/315761686
            InvalidateOptionsMenu();
        }
    }
}
