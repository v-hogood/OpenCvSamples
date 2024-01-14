using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using static OpenCV.Android.CameraBridgeViewBase;

namespace QrDetection;

[Activity(Name = "org.opencv.samples.qrdetection.QrDetectionActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
public class QrDetectionActivity : CameraActivity,
    ICvCameraViewListener
{
    private const string  Tag = "QRdetection::Activity";

    private CameraBridgeViewBase mOpenCvCameraView;
    private QrProcessor    mQRDetector;
    private IMenuItem             mItemQRCodeDetectorAruco;
    private IMenuItem             mItemQRCodeDetector;
    private IMenuItem             mItemTryDecode;
    private IMenuItem             mItemMulti;

    override protected void OnCreate(Bundle savedInstanceState)
    {
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

        Log.Debug(Tag, "Creating and setting view");
        mOpenCvCameraView = new JavaCameraView(this, -1);
        SetContentView(mOpenCvCameraView);
        mOpenCvCameraView.Visibility = ViewStates.Visible;
        mOpenCvCameraView.SetCvCameraViewListener(this);
        mQRDetector = new QrProcessor(true);
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
        if (mOpenCvCameraView != null) {
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

    override public bool OnCreateOptionsMenu(IMenu menu)
    {
        Log.Info(Tag, "called OnCreateOptionsMenu");
        mItemQRCodeDetectorAruco = menu.Add("Aruco-based QR code detector");
        mItemQRCodeDetectorAruco.SetCheckable(true);
        mItemQRCodeDetectorAruco.SetChecked(true);

        mItemQRCodeDetector = menu.Add("Legacy QR code detector");
        mItemQRCodeDetector.SetCheckable(true);
        mItemQRCodeDetector.SetChecked(false);

        mItemTryDecode = menu.Add("Try to decode QR codes");
        mItemTryDecode.SetCheckable(true);
        mItemTryDecode.SetChecked(true);

        mItemMulti = menu.Add("Use multi detect/decode");
        mItemMulti.SetCheckable(true);
        mItemMulti.SetChecked(true);

        return true;
    }

    override public bool OnOptionsItemSelected(IMenuItem item)
    {
        Log.Info(Tag, "Menu Item selected " + item);
        if (item == mItemQRCodeDetector && !mItemQRCodeDetector.IsChecked)
        {
            mQRDetector = new QrProcessor(false);
            mItemQRCodeDetector.SetChecked(true);
            mItemQRCodeDetectorAruco.SetChecked(false);
        } else if (item == mItemQRCodeDetectorAruco && !mItemQRCodeDetectorAruco.IsChecked)
        {
            mQRDetector = new QrProcessor(true);
            mItemQRCodeDetector.SetChecked(false);
            mItemQRCodeDetectorAruco.SetChecked(true);
        }
        else if (item == mItemTryDecode)
        {
            mItemTryDecode.SetChecked(!mItemTryDecode.IsChecked);
        }
        else if (item == mItemMulti)
        {
            mItemMulti.SetChecked(!mItemMulti.IsChecked);
        }
        return true;
    }

    public void OnCameraViewStarted(int width, int height)
    {
    }

    public void OnCameraViewStopped()
    {
    }

    public Mat OnCameraFrame(Mat inputFrame)
    {
        return mQRDetector.handleFrame(inputFrame, mItemTryDecode.IsChecked, mItemMulti.IsChecked);
    }
}
