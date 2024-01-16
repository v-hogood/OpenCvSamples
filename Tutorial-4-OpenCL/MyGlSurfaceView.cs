using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using OpenCV.Android;

namespace OpenCL;

[Register("org.opencv.samples.opencl.MyGLSurfaceView")]
public class MyGLSurfaceView : CameraGLSurfaceView,
    CameraGLSurfaceView.ICameraTextureListener
{
    const string LogTag = "MyGLSurfaceView";
    protected int procMode = NativePart.ProcessingModeOclOcv;
    static string[] procModeName = {"No Processing", "CPU", "OpenCL Direct", "OpenCL via OpenCV"};
    protected int  frameCounter;
    protected long lastNanoTime;
    TextView mFpsText = null;

    public MyGLSurfaceView(Context context, IAttributeSet attrs) :
        base(context, attrs)
    { }

    override public bool OnTouchEvent(MotionEvent e)
    {
        if (e.Action == MotionEventActions.Down)
            ((Activity)Context).OpenOptionsMenu();
        return true;
    }

    override public void SurfaceCreated(ISurfaceHolder holder)
    {
        base.SurfaceCreated(holder);
        // NativePart.InitCL();
    }

    override public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        // NativePart.CloseCL();
        base.SurfaceDestroyed(holder);
    }

    public void SetProcessingMode(int newMode)
    {
        if (newMode >= 0 && newMode < procModeName.Length)
            procMode = newMode;
        else
            Log.Error(LogTag, "Ignoring invalid processing mode: " + newMode);

        ((Activity)Context).RunOnUiThread(() =>
            Toast.MakeText(Context, "Selected mode: " + procModeName[procMode], ToastLength.Short).Show());
    }

    public void OnCameraViewStarted(int width, int height)
    {
        ((Activity)Context).RunOnUiThread(() =>
            Toast.MakeText(Context, "OnCameraViewStarted", ToastLength.Short).Show());
        if (NativePart.BuiltWithOpenCL(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object))))
            NativePart.InitCL(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)));
        frameCounter = 0;
        lastNanoTime = JavaSystem.NanoTime();
    }

    public void OnCameraViewStopped()
    {
        ((Activity)Context).RunOnUiThread(() =>
            Toast.MakeText(Context, "OnCameraViewStopped", ToastLength.Short).Show());
        NativePart.CloseCL(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)));
    }

    public bool OnCameraTexture(int texIn, int texOut, int width, int height)
    {
        // FPS
        frameCounter++;
        if (frameCounter >= 30)
        {
            int fps = (int) (frameCounter * 1e9 / (JavaSystem.NanoTime() - lastNanoTime));
            Log.Info(LogTag, "DrawFrame() FPS: " + fps);
            if (mFpsText != null)
            {
                Action fpsUpdater = new Action(() =>
                    mFpsText.Text = "FPS: " + fps);
                new Handler(Looper.MainLooper).Post(fpsUpdater);
            }
            else
            {
                Log.Debug(LogTag, "mFpsText == null");
                mFpsText = (TextView)((Activity)Context).FindViewById(Resource.Id.fps_text_view);
            }
            frameCounter = 0;
            lastNanoTime = JavaSystem.NanoTime();
        }

        if (procMode == NativePart.ProcessingModeNoProcessing)
            return false;

        NativePart.ProcessFrame(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), texIn, texOut, width, height, procMode);
        return true;
    }
}
