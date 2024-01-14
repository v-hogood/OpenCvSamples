using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;

namespace Puzzle15
{
    [Activity(Name = "org.opencv.samples.puzzle15.Puzzle15Activity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class Puzzle15Activity : CameraActivity,
        CameraBridgeViewBase.ICvCameraViewListener,
        View.IOnTouchListener
    {
        private const string Tag = "Puzzle15::Activity";

        private CameraBridgeViewBase mOpenCvCameraView;
        private Puzzle15Processor    mPuzzle15;
        private IMenuItem            mItemHideNumbers;
        private IMenuItem            mItemStartNewGame;

        private int                  mGameWidth;
        private int                  mGameHeight;

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
            mOpenCvCameraView = (CameraBridgeViewBase) new JavaCameraView(this, -1);
            SetContentView(mOpenCvCameraView);
            mOpenCvCameraView.Visibility = ViewStates.Visible;
            mOpenCvCameraView.SetCvCameraViewListener(this);
            mPuzzle15 = new Puzzle15Processor();
            mPuzzle15.PrepareNewGame();
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

        override public bool OnCreateOptionsMenu(IMenu menu)
        {
            Log.Info(Tag, "called OnCreateOptionsMenu");
            mItemHideNumbers = menu.Add("Show/hide tile numbers");
            mItemStartNewGame = menu.Add("Start new game");
            return true;
        }

        override public bool OnOptionsItemSelected(IMenuItem item)
        {
            Log.Info(Tag, "Menu Item selected " + item);
            if (item == mItemStartNewGame)
            {
                // We need to start new game
                mPuzzle15.PrepareNewGame();
            }
            else if (item == mItemHideNumbers)
            {
                // We need to enable or disable drawing of the tile numbers
                mPuzzle15.ToggleTileNumbers();
            }
            return true;
        }

        public void OnCameraViewStarted(int width, int height)
        {
            mGameWidth = width;
            mGameHeight = height;
            mPuzzle15.PrepareGameSize(width, height);
        }

        public void OnCameraViewStopped() { }

        public bool OnTouch(View view, MotionEvent motionEvent)
        {
            int xpos, ypos;

            xpos = (view.Width - mGameWidth) / 2;
            xpos = (int)motionEvent.GetX() - xpos;

            ypos = (view.Height - mGameHeight) / 2;
            ypos = (int)motionEvent.GetY() - ypos;

            if (xpos >= 0 && xpos <= mGameWidth && ypos >= 0  && ypos <= mGameHeight)
            {
                // click is inside the picture. Deliver this event to processor
                mPuzzle15.DeliverTouchEvent(xpos, ypos);
            }

            return false;
        }

        public Mat OnCameraFrame(Mat inputFrame)
        {
            return mPuzzle15.PuzzleFrame(inputFrame);
        }
    }
}
