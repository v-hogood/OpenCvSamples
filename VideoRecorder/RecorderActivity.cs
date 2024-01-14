using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using OpenCV.VideoIO;
using static OpenCV.Android.CameraBridgeViewBase;
using File = Java.IO.File;
using Size = OpenCV.Core.Size;

namespace VideoRecorder;

[Activity(Name = "org.opencv.samples.videorecorder.RecorderActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
public class RecorderActivity : CameraActivity,
    ICvCameraViewListener2, View.IOnClickListener
{
    private const string Tag = "OCVSample::Activity";
    private const string FILENAME_MP4 = "sample_video1.mp4";
    private const string FILENAME_AVI = "sample_video1.avi";

    private const int STATUS_FINISHED_PLAYBACK = 0;
    private const int STATUS_PREVIEW = 1;
    private const int STATUS_RECORDING = 2;
    private const int STATUS_PLAYING = 3;
    private const int STATUS_ERROR = 4;

    private string mVideoFilename;
    private bool mUseBuiltInMJPG = false;

    private int mStatus = STATUS_FINISHED_PLAYBACK;
    private int mFPS = 30;
    private int mWidth = 0, mHeight = 0;

    private CameraBridgeViewBase mOpenCvCameraView;
    private ImageView mImageView;
    private Button mTriggerButton;
    private TextView mStatusTextView;
    Action mPlayerThread;

    private VideoWriter mVideoWriter = null;
    private VideoCapture mVideoCapture = null;
    private Mat mVideoFrame;
    private Mat mRenderFrame;

    public RecorderActivity()
    {
        Log.Info(Tag, "Instantiated new " + this.Class);
    }

    /** Called when the activity is first created. */
    override protected void OnCreate(Bundle savedInstanceState)
    {
        Log.Info(Tag, "called OnCreate");
        base.OnCreate(savedInstanceState);
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);

        SetContentView(Resource.Layout.recorder_surface_view);

        mStatusTextView = (TextView) FindViewById(Resource.Id.textview1);
        mStatusTextView.BringToFront();

        if (OpenCVLoader.InitLocal())
        {
            Log.Info(Tag, "OpenCV loaded successfully");
        }
        else
        {
            Log.Error(Tag, "OpenCV initialization failed!");
            mStatus = STATUS_ERROR;
            mStatusTextView.Text = "Error: Can't initialize OpenCV";
            return;
        }

        mOpenCvCameraView = (CameraBridgeViewBase) FindViewById(Resource.Id.recorder_activity_java_surface_view);
        mOpenCvCameraView.Visibility = ViewStates.Gone;
        mOpenCvCameraView.SetCvCameraViewListener2(this);
        mOpenCvCameraView.DisableView();

        mImageView = (ImageView) FindViewById(Resource.Id.image_view);

        mTriggerButton = (Button) FindViewById(Resource.Id.btn1);
        mTriggerButton.SetOnClickListener(this);
        mTriggerButton.BringToFront();

        if (mUseBuiltInMJPG)
            mVideoFilename = FilesDir + "/" + FILENAME_AVI;
        else
            mVideoFilename = FilesDir + "/" + FILENAME_MP4;
    }

    override protected void OnPause()
    {
        Log.Debug(Tag, "OnPause");
        base.OnPause();
        if (mOpenCvCameraView != null)
            mOpenCvCameraView.DisableView();
        mImageView.Visibility = ViewStates.Gone;
        if (mVideoWriter != null)
        {
            mVideoWriter.Release();
            mVideoWriter = null;
        }
        if (mVideoCapture != null)
        {
            mVideoCapture.Release();
            mVideoCapture = null;
        }
        mStatus = STATUS_FINISHED_PLAYBACK;
        mStatusTextView.Text = "Status: Finished playback";
        mTriggerButton.Text = "Start Camera";

        mVideoFrame.Release();
        mRenderFrame.Release();
    }

    override protected void OnResume()
    {
        Log.Debug(Tag, "OnResume");
        base.OnResume();

        mVideoFrame = new Mat();
        mRenderFrame = new Mat();

        ChangeStatus();
    }

    override protected IList<CameraBridgeViewBase> CameraViewList =>
        new List<CameraBridgeViewBase>(1) { mOpenCvCameraView };

    override protected void OnDestroy()
    {
        Log.Debug(Tag, "called OnDestroy");
        base.OnDestroy();
        if (mOpenCvCameraView != null)
            mOpenCvCameraView.DisableView();
        if (mVideoWriter != null)
            mVideoWriter.Release();
        if (mVideoCapture != null)
            mVideoCapture.Release();
    }

    public void OnCameraViewStarted(int width, int height)
    {
        Log.Debug(Tag, "Camera view started " + width + "x" + height);
        mWidth = width;
        mHeight = height;
    }

    public void OnCameraViewStopped()
    {
        Log.Debug(Tag, "Camera view stopped");
    }

    public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
    {
        Log.Debug(Tag, "Camera frame arrived");

        Mat rgbMat = inputFrame.Rgba();

        Log.Debug(Tag, "Size: " + rgbMat.Width() + "x" + rgbMat.Height());

        if (mVideoWriter != null && mVideoWriter.IsOpened)
        {
            Imgproc.CvtColor(rgbMat, mVideoFrame, Imgproc.ColorRgba2bgr);
            mVideoWriter.Write(mVideoFrame);
        }

        return rgbMat;
    }

    public void OnClick(View view)
    {
        Log.Info(Tag, "OnClick event");
        ChangeStatus();
    }

    public void ChangeStatus()
    {
        switch(mStatus)
        {
            case STATUS_ERROR:
                Toast.MakeText(this, "Error", ToastLength.Long).Show();
                break;
            case STATUS_FINISHED_PLAYBACK:
                if (!StartPreview())
                {
                    SetErrorStatus();
                    break;
                }
                mStatus = STATUS_PREVIEW;
                mStatusTextView.Text = "Status: Camera preview";
                mTriggerButton.Text = "Start recording";
                break;
            case STATUS_PREVIEW:
                if (!StartRecording())
                {
                    SetErrorStatus();
                    break;
                }
                mStatus = STATUS_RECORDING;
                mStatusTextView.Text = "Status: recording video";
                mTriggerButton.Text = " Stop and play video";
                break;
            case STATUS_RECORDING:
                if (!StopRecording())
                {
                    SetErrorStatus();
                    break;
                }
                if (!StartPlayback())
                {
                    SetErrorStatus();
                    break;
                }
                mStatus = STATUS_PLAYING;
                mStatusTextView.Text = "Status: Playing video";
                mTriggerButton.Text = "Stop playback";
                break;
            case STATUS_PLAYING:
                if (!StopPlayback())
                {
                    SetErrorStatus();
                    break;
                }
                mStatus = STATUS_FINISHED_PLAYBACK;
                mStatusTextView.Text = "Status: Finished playback";
                mTriggerButton.Text = "Start Camera";
                break;
        }
    }

    public void SetErrorStatus()
    {
        mStatus = STATUS_ERROR;
        mStatusTextView.Text = "Status: Error";
    }

    public bool StartPreview()
    {
        mOpenCvCameraView.EnableView();
        mOpenCvCameraView.Visibility = ViewStates.Visible;
        return true;
    }

    public bool StartRecording()
    {
        Log.Info(Tag, "Starting recording");

        File file = new File(mVideoFilename);
        file.Delete();

        mVideoWriter = new VideoWriter();
        if (!mUseBuiltInMJPG)
        {
            mVideoWriter.Open(mVideoFilename, Videoio.CapAndroid, VideoWriter.Fourcc('H', '2', '6', '4'), mFPS, new Size(mWidth, mHeight));
            if (!mVideoWriter.IsOpened)
            {
                Log.Info(Tag, "Can't record H264. Switching to MJPG");
                mUseBuiltInMJPG = true;
                mVideoFilename = FilesDir + "/" + FILENAME_AVI;
            }
        }

        if (mUseBuiltInMJPG)
        {
            mVideoWriter.Open(mVideoFilename, VideoWriter.Fourcc('M', 'J', 'P', 'G'), mFPS, new Size(mWidth, mHeight));
        }

        Log.Debug(Tag, "Size: " + mWidth + "x" + mHeight);
        Log.Debug(Tag, "File: " + mVideoFilename);

        if (mVideoWriter.IsOpened)
        {
            Toast.MakeText(this, "Record started to file " + mVideoFilename, ToastLength.Long).Show();
            return true;
        }
        else
        {
            Toast.MakeText(this, "Failed to start a record", ToastLength.Long).Show();
            return false;
        }
    }

    public bool StopRecording()
    {
        Log.Info(Tag, "Finishing recording");
        mOpenCvCameraView.DisableView();
        mOpenCvCameraView.Visibility = ViewStates.Gone;
        mVideoWriter.Release();
        mVideoWriter = null;
        return true;
    }

    public bool StartPlayback()
    {
        mImageView.Visibility = ViewStates.Visible;

        if (!mUseBuiltInMJPG)
        {
            mVideoCapture = new VideoCapture(mVideoFilename, Videoio.CapAndroid);
        }
        else
        {
            mVideoCapture = new VideoCapture(mVideoFilename, Videoio.CapOpencvMjpeg);
        }

        if (!mVideoCapture.IsOpened)
        {
            Log.Error(Tag, "Can't open video");
            Toast.MakeText(this, "Can't open file " + mVideoFilename, ToastLength.Short).Show();
            return false;
        }

        Toast.MakeText(this, "Starting playback from file " + mVideoFilename, ToastLength.Short).Show();

        mPlayerThread = new(() =>
        {
            if (mVideoCapture == null || !mVideoCapture.IsOpened)
            {
                return;
            }
            mVideoCapture.Read(mVideoFrame);
            if (mVideoFrame.Empty())
            {
                if (mStatus == STATUS_PLAYING)
                {
                    ChangeStatus();
                }
                return;
            }
            // VideoCapture with CAP_ANDROID generates RGB frames instead of BGR
            // https://github.com/opencv/opencv/issues/24687
            Imgproc.CvtColor(mVideoFrame, mRenderFrame, mUseBuiltInMJPG ? Imgproc.ColorBgr2rgba: Imgproc.ColorRgb2rgba);
            Bitmap bmp = Bitmap.CreateBitmap(mRenderFrame.Cols(), mRenderFrame.Rows(), Bitmap.Config.Argb8888);
            Utils.MatToBitmap(mRenderFrame, bmp);
            mImageView.SetImageBitmap(bmp);
            Handler h = new(Looper.MainLooper);
            h.PostDelayed(mPlayerThread, 33);
        });

        mPlayerThread.Invoke();
        return true;
    }

    public bool StopPlayback()
    {
        mVideoCapture.Release();
        mVideoCapture = null;
        mImageView.Visibility = ViewStates.Gone;
        return true;
    }
}
