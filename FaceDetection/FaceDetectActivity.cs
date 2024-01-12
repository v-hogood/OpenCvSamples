using Android.Util;
using Android.Views;
using Java.IO;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using OpenCV.ObjDetect;
using static OpenCV.Android.CameraBridgeViewBase;
using IOException = Java.IO.IOException;
using Size = OpenCV.Core.Size;

namespace FaceDetection;

[Activity(Name = "org.opencv.samples.facedetection.FaceDetectActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
public class FaceDetectActivity : CameraActivity,
    ICvCameraViewListener2
{
    private const string    Tag  = "OCVSample::Activity";

    private static Scalar    BOX_COLOR         = new Scalar(0, 255, 0);
    private static Scalar    RIGHT_EYE_COLOR   = new Scalar(255, 0, 0);
    private static Scalar    LEFT_EYE_COLOR    = new Scalar(0, 0, 255);
    private static Scalar    NOSE_TIP_COLOR    = new Scalar(0, 255, 0);
    private static Scalar    MOUTH_RIGHT_COLOR = new Scalar(255, 0, 255);
    private static Scalar    MOUTH_LEFT_COLOR  = new Scalar(0, 255, 255);

    private Mat                    mRgba;
    private Mat                    mBgr;
    private Mat                    mBgrScaled;
    private Size                   mInputSize = null;
    private float                  mScale = 2.0f;
    private MatOfByte              mModelBuffer;
    private MatOfByte              mConfigBuffer;
    private FaceDetectorYN         mFaceDetector;
    private Mat                    mFaces;

    private CameraBridgeViewBase   mOpenCvCameraView;

    public FaceDetectActivity()
    {
        Log.Info(Tag, "Instantiated new " + this.Class);
    }

    /** Called when the activity is first created. */
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

        byte[] buffer;
        try
        {
            // Read data from assets.
            BufferedInputStream bis = new BufferedInputStream(Assets.Open("face_detection_yunet_2023mar.onnx"));

            int size = bis.Available();
            buffer = new byte[size];
            int bytesRead = bis.Read(buffer);
            bis.Close();
        }
        catch (IOException e)
        {
            e.PrintStackTrace();
            Log.Error(Tag, "Failed to ONNX model from resources! Exception thrown: " + e);
            Toast.MakeText(this, "Failed to ONNX model from resources!", ToastLength.Long).Show();
            return;
        }

        mModelBuffer = new MatOfByte(buffer);
        mConfigBuffer = new MatOfByte();

        mFaceDetector = FaceDetectorYN.Create("onnx", mModelBuffer, mConfigBuffer, new Size(320, 320));
        if (mFaceDetector == null)
        {
            Log.Error(Tag, "Failed to create FaceDetectorYN!");
            Toast.MakeText(this, "Failed to create FaceDetectorYN!", ToastLength.Long).Show();
            return;
        }
        else
            Log.Info(Tag, "FaceDetectorYN initialized successfully!");

        Window.AddFlags(WindowManagerFlags.KeepScreenOn);

        SetContentView(Resource.Layout.face_detect_surface_view);

        mOpenCvCameraView = (CameraBridgeViewBase) FindViewById(Resource.Id.fd_activity_surface_view);
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
        mOpenCvCameraView.DisableView();
    }

    public void OnCameraViewStarted(int width, int height)
    {
        mRgba = new Mat();
        mBgr = new Mat();
        mBgrScaled = new Mat();
        mFaces = new Mat();
    }

    public void OnCameraViewStopped()
    {
        mRgba.Release();
        mBgr.Release();
        mBgrScaled.Release();
        mFaces.Release();
    }

    public void Visualize(Mat rgba, Mat faces)
    {
        int thickness = 2;
        float[] faceData = new float[faces.Cols() * faces.Channels()];

        for (int i = 0; i < faces.Rows(); i++)
        {
            faces.Get(i, 0, faceData);

            Log.Debug(Tag, "Detected face (" + faceData[0] + ", " + faceData[1] + ", " +
                                               faceData[2] + ", " + faceData[3] + ")");

            // Draw bounding box
            Imgproc.Rectangle(rgba, new Rect((int)Math.Round(mScale * faceData[0]), (int)Math.Round(mScale * faceData[1]),
                                             (int)Math.Round(mScale * faceData[2]), (int)Math.Round(mScale * faceData[3])),
                              BOX_COLOR, thickness);
            // Draw landmarks
            Imgproc.Circle(rgba, new Point(Math.Round(mScale * faceData[4]), Math.Round(mScale * faceData[5])),
                           2, RIGHT_EYE_COLOR, thickness);
            Imgproc.Circle(rgba, new Point(Math.Round(mScale * faceData[6]), Math.Round(mScale * faceData[7])),
                           2, LEFT_EYE_COLOR, thickness);
            Imgproc.Circle(rgba, new Point(Math.Round(mScale * faceData[8]), Math.Round(mScale * faceData[9])),
                           2, NOSE_TIP_COLOR, thickness);
            Imgproc.Circle(rgba, new Point(Math.Round(mScale * faceData[10]), Math.Round(mScale * faceData[11])),
                           2, MOUTH_RIGHT_COLOR, thickness);
            Imgproc.Circle(rgba, new Point(Math.Round(mScale * faceData[12]), Math.Round(mScale * faceData[13])),
                           2, MOUTH_LEFT_COLOR, thickness);
        }
    }

    public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
    {
        mRgba = inputFrame.Rgba();

        if (mInputSize == null)
        {
            mInputSize = new Size(Math.Round(mRgba.Cols() / mScale), Math.Round(mRgba.Rows() / mScale));
            mFaceDetector.InputSize = mInputSize;
        }

        Imgproc.CvtColor(mRgba, mBgr, Imgproc.ColorRgba2bgr);
        Imgproc.Resize(mBgr, mBgrScaled, mInputSize);

        if (mFaceDetector != null)
        {
            int status = mFaceDetector.Detect(mBgrScaled, mFaces);
            Log.Debug(Tag, "Detector returned status " + status);
            Visualize(mRgba, mFaces);
        }

        return mRgba;
    }
}
