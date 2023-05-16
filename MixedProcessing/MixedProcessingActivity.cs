using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;

namespace MixedProcessing
{
    [Activity(Name = "org.opencv.samples.mixedprocessing.MixedProcessingActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MixedProcessingActivity : CameraActivity,
            CameraBridgeViewBase.ICvCameraViewListener2
    {
        private const string Tag = "OCVSample::Activity";

        private const int ViewModeRgba     = 0;
        private const int ViewModeGray     = 1;
        private const int ViewModeCanny    = 2;
        private const int ViewModeFeatures = 5;

        private int       mViewMode;
        private Mat       mRgba;
        private Mat       mIntermediateMat;
        private Mat       mGray;

        private IMenuItem mItemPreviewRGBA;
        private IMenuItem mItemPreviewGray;
        private IMenuItem mItemPreviewCanny;
        private IMenuItem mItemPreviewFeatures;

        private CameraBridgeViewBase mOpenCvCameraView;

        public class LoaderCallback : BaseLoaderCallback
        {
            public LoaderCallback(MixedProcessingActivity activity) : base(activity)
            {
                this.activity = activity;
            }
            MixedProcessingActivity activity;

            override public void OnManagerConnected(int status)
            {
                switch (status)
                {
                    case LoaderCallbackInterface.Success:
                        {
                            Log.Info(Tag, "OpenCV loaded successfully");

                            // Load native library after(!) OpenCV initialization
                            JavaSystem.LoadLibrary("mixed_sample");

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

        public MixedProcessingActivity()
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

            SetContentView(Resource.Layout.mixedprocessing_surface_view);

            mOpenCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.mixedprocessing_activity_surface_view);
            mOpenCvCameraView.Visibility = ViewStates.Visible;
            mOpenCvCameraView.SetCvCameraViewListener2(this);
        }

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            Log.Info(Tag, "called OnCreateOptionsMenu");
            mItemPreviewRGBA = menu.Add("Preview RGBA");
            mItemPreviewGray = menu.Add("Preview GRAY");
            mItemPreviewCanny = menu.Add("Canny");
            mItemPreviewFeatures = menu.Add("Find features");
            return true;
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
                mLoaderCallback.OnManagerConnected(LoaderCallbackInterface.Success);
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
            mRgba = new Mat(height, width, CvType.Cv8uc4);
            mIntermediateMat = new Mat(height, width, CvType.Cv8uc4);
            mGray = new Mat(height, width, CvType.Cv8uc1);
        }

        public void OnCameraViewStopped()
        {
            mRgba.Release();
            mGray.Release();
            mIntermediateMat.Release();
        }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            int viewMode = mViewMode;
            switch (viewMode)
            {
                case ViewModeGray:
                    // input frame has gray scale format
                    Imgproc.CvtColor(inputFrame.Gray(), mRgba, Imgproc.ColorGray2rgba, 4);
                    break;
                case ViewModeRgba:
                    // input frame has RBGA format
                    mRgba = inputFrame.Rgba();
                    break;
                case ViewModeCanny:
                    // input frame has gray scale format
                    mRgba = inputFrame.Rgba();
                    Imgproc.Canny(inputFrame.Gray(), mIntermediateMat, 80, 100);
                    Imgproc.CvtColor(mIntermediateMat, mRgba, Imgproc.ColorGray2rgba, 4);
                    break;
                case ViewModeFeatures:
                    // input frame has RGBA format
                    mRgba = inputFrame.Rgba();
                    mGray = inputFrame.Gray();
                    FindFeatures(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)),
                        mGray.NativeObjAddr, mRgba.NativeObjAddr);
                    break;
            }

            return mRgba;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            Log.Info(Tag, "called OnOptionsItemSelected; selected item: " + item);

            if (item == mItemPreviewRGBA)
            {
                mViewMode = ViewModeRgba;
            }
            else if (item == mItemPreviewGray)
            {
                mViewMode = ViewModeGray;
            }
            else if (item == mItemPreviewCanny)
            {
                mViewMode = ViewModeCanny;
            }
            else if (item == mItemPreviewFeatures)
            {
                mViewMode = ViewModeFeatures;
            }

            return true;
        }

        [DllImport("libmixed_sample", EntryPoint = "Java_org_opencv_samples_mixedprocessing_MixedProcessingActivity_FindFeatures")]
        private extern static void FindFeatures(IntPtr env, IntPtr jniClass, long matAddrGr, long matAddrRgba);
    }
}
