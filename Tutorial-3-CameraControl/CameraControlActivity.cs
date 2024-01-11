using Android;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.Text;
using Java.Util;
using OpenCV.Android;
using OpenCV.Core;
using static OpenCV.Android.CameraBridgeViewBase;
using Size = Android.Hardware.Camera.Size;

namespace CameraControl
{
    [Activity(Name = "org.opencv.samples.cameracontrol.CameraControlActivity", Label = "@string/app_name", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", MainLauncher = true)]
    public class CameraControlActivity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener2,
        View.IOnTouchListener
    {
        private const string Tag = "OCVSample::Activity";

        private CameraControlView mOpenCvCameraView;
#pragma warning disable 0618
        private IList<Size> mResolutionList;
#pragma warning restore 0618
        private IMenu mMenu;
        private bool mCameraStarted = false;
        private bool mMenuItemsCreated = false;
        private IMenuItem[] mEffectMenuItems;
        private ISubMenu mColorEffectsMenu;
        private IMenuItem[] mResolutionMenuItems;
        private ISubMenu mResolutionMenu;

        public CameraControlActivity()
        {
            Log.Info(Tag, "Instantiated new " + this.Class);
        }

        // Called when the activity is first created.
        override protected void OnCreate(Bundle savedInstanceState)
        {
            Log.Info(Tag, "called OnCreate");
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

            SetContentView(Resource.Layout.cameracontrol_surface_view);

            mOpenCvCameraView = FindViewById<CameraControlView>(Resource.Id.cameracontrol_activity_java_surface_view);

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
            {
                mOpenCvCameraView.SetOnTouchListener(this);
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

        public void OnCameraViewStarted(int width, int height)
        {
            mCameraStarted = true;
            SetupMenuItems();
        }

        public void OnCameraViewStopped() { }

        public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
        {
            return inputFrame.Rgba();
        }

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            mMenu = menu;
            SetupMenuItems();
            return true;
        }

        private void SetupMenuItems()
        {
            if (mMenu == null || !mCameraStarted || mMenuItemsCreated)
            {
                return;
            }
            IList<string> effects = mOpenCvCameraView.EffectList;

            if (effects == null)
            {
                Log.Error(Tag, "Color effects are not supported by device!");
                return;
            }

            mColorEffectsMenu = mMenu.AddSubMenu("Color Effect");
            mEffectMenuItems = new IMenuItem[effects.Count];

            int idx = 0;
            foreach(string effect in effects)
            {
                mEffectMenuItems[idx] = mColorEffectsMenu.Add(1, idx, IMenu.None, effect);
                idx++;
            }

            mResolutionMenu = mMenu.AddSubMenu("Resolution");
            mResolutionList = mOpenCvCameraView.ResolutionList;
            mResolutionMenuItems = new IMenuItem[mResolutionList.Count];

            idx = 0;
#pragma warning disable 0618
            foreach (Size resolution in mResolutionList)
#pragma warning restore 0618
            {
                mResolutionMenuItems[idx] = mResolutionMenu.Add(2, idx, IMenu.None,
#pragma warning disable CA1422
                        resolution.Width + "x" + resolution.Height);
#pragma warning restore CA1422
                idx++;
            }
            mMenuItemsCreated = true;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            Log.Info(Tag, "called onOptionsItemSelected; selected item: " + item);
            if (item.GroupId == 1)
            {
                mOpenCvCameraView.Effect = item.TitleFormatted.ToString();
                Toast.MakeText(this, mOpenCvCameraView.Effect, ToastLength.Short).Show();
            }
            else if (item.GroupId == 2)
            {
                int id = item.ItemId;
#pragma warning disable 0618
                Size resolution = mResolutionList[id];
#pragma warning restore 0618
                mOpenCvCameraView.Resolution = resolution;
                resolution = mOpenCvCameraView.Resolution;
#pragma warning disable CA1422
                string caption = resolution.Width + "x" + resolution.Height;
#pragma warning restore CA1422
                Toast.MakeText(this, caption, ToastLength.Short).Show();
            }

            return true;
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            Log.Info(Tag,"OnTouch event");
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
#pragma warning disable CA1416
                if (CheckSelfPermission(Manifest.Permission.WriteExternalStorage)
                    != Permission.Granted)
                {
                    String[] permissions = { Manifest.Permission.WriteExternalStorage };
                    RequestPermissions(permissions, 1);
                    return false;
                }
#pragma warning restore CA1416
            }
            SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd_HH-mm-ss");
            string currentDateandTime = sdf.Format(new Date());
            string fileName = "sample_picture_" + currentDateandTime + ".jpg";
            mOpenCvCameraView.TakePicture(fileName);
            Toast.MakeText(this, fileName + " saved", ToastLength.Short).Show();
            return false;
        }
    }
}
