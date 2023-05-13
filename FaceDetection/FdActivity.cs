using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.IO;
using Java.Lang;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using OpenCV.ObjDetect;
using static OpenCV.Android.CameraBridgeViewBase;
using File = Java.IO.File;
using IOException = Java.IO.IOException;
using Size = OpenCV.Core.Size;

namespace FaceDetection
{
    [Activity(Name = "org.opencv.samples.facedetction.FdActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class FdActivity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener2
    {

        private const string         Tag                = "OCVSample::Activity";
        private readonly Scalar      FaceRectColor      = new Scalar(0, 255, 0, 255);
        public const int             JavaDetector       = 0;
        public const int             NativeDetector     = 1;

        private IMenuItem             mItemFace50;
        private IMenuItem             mItemFace40;
        private IMenuItem             mItemFace30;
        private IMenuItem             mItemFace20;
        private IMenuItem             mItemType;

        private Mat                   mRgba;
        private Mat                   mGray;
        private File                  mCascadeFile;
        private CascadeClassifier     mJavaDetector;
        private DetectionBasedTracker mNativeDetector;

        private int                   mDetectorType     = JavaDetector;
        private string[]              mDetectorName;

        private float                 mRelativeFaceSize = 0.2f;
        private int                   mAbsoluteFaceSize = 0;

        private CameraBridgeViewBase  mOpenCvCameraView;

        public class LoaderCallback : BaseLoaderCallback
        {
            public LoaderCallback(FdActivity activity) : base(activity)
            {
                this.activity = activity;
            }
            FdActivity activity;

            override public void OnManagerConnected(int status)
            {
                switch (status)
                {
                    case LoaderCallbackInterface.Success:
                    {
                        Log.Info(Tag, "OpenCV loaded successfully");

                        // Load native library after(!) OpenCV initialization
                        JavaSystem.LoadLibrary("detection_based_tracker");

                        try
                        {
                            // load cascade file from application resources
                            Stream s = activity.Resources.OpenRawResource(Resource.Raw.lbpcascade_frontalface);
                            File cascadeDir = activity.GetDir("cascade", FileCreationMode.Private);
                            activity.mCascadeFile = new File(cascadeDir, "lbpcascade_frontalface.xml");
                            FileOutputStream os = new FileOutputStream(activity.mCascadeFile);

                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = s.Read(buffer)) != 0)
                            {
                                os.Write(buffer, 0, bytesRead);
                            }
                            s.Close();
                            os.Close();

                            activity.mJavaDetector = new CascadeClassifier(activity.mCascadeFile.AbsolutePath);
                            if (activity.mJavaDetector.Empty())
                            {
                                Log.Error(Tag, "Failed to load cascade classifier");
                                activity.mJavaDetector = null;
                            }
                            else
                                Log.Info(Tag, "Loaded cascade classifier from " + activity.mCascadeFile.AbsolutePath);

                            activity.mNativeDetector = new DetectionBasedTracker(activity.mCascadeFile.AbsolutePath, 0);

                            cascadeDir.Delete();

                        }
                        catch (IOException e)
                        {
                            e.PrintStackTrace();
                            Log.Error(Tag, "Failed to load cascade. Exception thrown: " + e);
                        }

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

        public FdActivity()
        {
            mDetectorName = new string[2];
            mDetectorName[JavaDetector] = "Java";
            mDetectorName[NativeDetector] = "Native (tracking)";

            mLoaderCallback = new LoaderCallback(this);

            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
            base.OnCreate(savedInstanceState);
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
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug(Tag, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, mLoaderCallback);
            } else {
                Log.Debug(Tag, "OpenCV library found inside package. Using it!");
                mLoaderCallback.OnManagerConnected(LoaderCallbackInterface.Success);
            }
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
            mGray = new Mat();
            mRgba = new Mat();
        }

        public void OnCameraViewStopped()
        {
            mGray.Release();
            mRgba.Release();
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            mRgba = inputFrame.Rgba();
            mGray = inputFrame.Gray();

            if (mAbsoluteFaceSize == 0)
            {
                int height = mGray.Rows();
                if (Math.Round(height * mRelativeFaceSize) > 0)
                {
                    mAbsoluteFaceSize = (int)Math.Round(height * mRelativeFaceSize);
                }
                mNativeDetector.SetMinFaceSize(mAbsoluteFaceSize);
            }

            MatOfRect faces = new MatOfRect();

            if (mDetectorType == JavaDetector)
            {
                if (mJavaDetector != null)
                    mJavaDetector.DetectMultiScale(mGray, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                            new Size(mAbsoluteFaceSize, mAbsoluteFaceSize), new Size());
            }
            else if (mDetectorType == NativeDetector)
            {
                if (mNativeDetector != null)
                    mNativeDetector.detect(mGray, faces);
            }
            else
            {
                Log.Error(Tag, "Detection method is not selected!");
            }

            Rect[] facesArray = faces.ToArray();
            for (int i = 0; i < facesArray.Length; i++)
                Imgproc.Rectangle(mRgba, facesArray[i].Tl(), facesArray[i].Br(), FaceRectColor, 3);

            return mRgba;
        }

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            Log.Info(Tag, "called onCreateOptionsMenu");
            mItemFace50 = menu.Add("Face size 50%");
            mItemFace40 = menu.Add("Face size 40%");
            mItemFace30 = menu.Add("Face size 30%");
            mItemFace20 = menu.Add("Face size 20%");
            mItemType   = menu.Add(mDetectorName[mDetectorType]);
            return true;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            Log.Info(Tag, "called onOptionsItemSelected; selected item: " + item);
            if (item == mItemFace50)
                SetMinFaceSize(0.5f);
            else if (item == mItemFace40)
                SetMinFaceSize(0.4f);
            else if (item == mItemFace30)
                SetMinFaceSize(0.3f);
            else if (item == mItemFace20)
                SetMinFaceSize(0.2f);
            else if (item == mItemType)
            {
                int tmpDetectorType = (mDetectorType + 1) % mDetectorName.Length;
                item.SetTitle(mDetectorName[tmpDetectorType]);
                SetDetectorType(tmpDetectorType);
            }
            return true;
        }

        private void SetMinFaceSize(float faceSize)
        {
            mRelativeFaceSize = faceSize;
            mAbsoluteFaceSize = 0;
        }

        private void SetDetectorType(int type)
        {
            if (mDetectorType != type)
            {
                mDetectorType = type;

                if (type == NativeDetector)
                {
                    Log.Info(Tag, "Detection Based Tracker enabled");
                    mNativeDetector.Start();
                }
                else
                {
                    Log.Info(Tag, "Cascade detector enabled");
                    mNativeDetector.Stop();
                }
            }
        }
    }
}
