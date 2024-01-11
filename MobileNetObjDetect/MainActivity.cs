using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Views;
using Java.IO;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.Dnn;
using OpenCV.ImgProc;
using static OpenCV.Android.CameraBridgeViewBase;
using File = Java.IO.File;
using IOException = Java.IO.IOException;
using Size = OpenCV.Core.Size;

namespace MobileNetObjDetect;

[Activity(Name = "org.opencv.samples.mobilenet.MainActivity", Label = "@string/app_name", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", MainLauncher = true)]
public class MainActivity : CameraActivity,
    ICvCameraViewListener2
{
    public MainActivity()
    {
        Log.Info(Tag, "Instantiated new " + this.Class);
    }

    override protected IList<CameraBridgeViewBase> CameraViewList =>
        new List<CameraBridgeViewBase>(1) { mOpenCvCameraView };

    protected override void OnResume()
    {
        base.OnResume();
        if (mOpenCvCameraView != null)
            mOpenCvCameraView.EnableView();
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
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

        SetContentView(Resource.Layout.activity_main);

        // Set up camera listener.
        mOpenCvCameraView = FindViewById<CameraBridgeViewBase>(Resource.Id.CameraView);
        mOpenCvCameraView.Visibility = ViewStates.Visible;
        mOpenCvCameraView.SetCvCameraViewListener2(this);
    }

    // Load a network.
    public void OnCameraViewStarted(int width, int height)
    {
        string proto = GetPath("MobileNetSSD_deploy.prototxt", this);
        string weights = GetPath("MobileNetSSD_deploy.caffemodel", this);
        net = Dnn.ReadNetFromCaffe(proto, weights);
        Log.Info(Tag, "Network loaded successfully");
    }

    public Mat OnCameraFrame(ICvCameraViewFrame inputFrame)
    {
        const int IN_WIDTH = 300;
        const int IN_HEIGHT = 300;
        const double IN_SCALE_FACTOR = 0.007843;
        const double MEAN_VAL = 127.5;
        const double THRESHOLD = 0.2;

        // Get a new frame
        Mat frame = inputFrame.Rgba();
        Imgproc.CvtColor(frame, frame, Imgproc.ColorRgba2rgb);

        // Forward image through network.
        Mat blob = Dnn.BlobFromImage(frame, IN_SCALE_FACTOR,
            new Size(IN_WIDTH, IN_HEIGHT),
            new Scalar(MEAN_VAL, MEAN_VAL, MEAN_VAL), /*swapRB*/false, /*crop*/false);
        net.SetInput(blob);
        Mat detections = net.Forward();

        int cols = frame.Cols();
        int rows = frame.Rows();

        detections = detections.Reshape(1, (int)detections.Total() / 7);

        for (int i = 0; i < detections.Rows(); ++i)
        {
            double confidence = detections.Get(i, 2)[0];
            if (confidence > THRESHOLD)
            {
                int classId = (int)detections.Get(i, 1)[0];

                int left   = (int)(detections.Get(i, 3)[0] * cols);
                int top    = (int)(detections.Get(i, 4)[0] * rows);
                int right  = (int)(detections.Get(i, 5)[0] * cols);
                int bottom = (int)(detections.Get(i, 6)[0] * rows);

                // Draw rectangle around detected object.
                Imgproc.Rectangle(frame, new Point(left, top), new Point(right, bottom),
                    new Scalar(0, 255, 0), 2);
                string label = classNames[classId] + ": " + String.Format("{0:0.000}", confidence);
                int[] baseLine = new int[1];
                Size labelSize = Imgproc.GetTextSize(label, Imgproc.FontHersheySimplex, 1, 2, baseLine);

                // Draw background for label.
                Imgproc.Rectangle(frame, new Point(left, top - labelSize.Height),
                    new Point(left + labelSize.Width, top + baseLine[0]),
                    new Scalar(255, 255, 255), Imgproc.Filled);
                // Write class name and confidence.
                Imgproc.PutText(frame, label, new Point(left, top),
                    Imgproc.FontHersheySimplex, 1, new Scalar(0, 0, 0), 2);
            }
        }
        return frame;
    }

    public void OnCameraViewStopped() { }

    // Upload file to storage and return a path.
    private static string GetPath(string file, Context context)
    {
        AssetManager assetManager = context.Assets;

        BufferedInputStream inputStream = null;
        try
        {
            // Read data from assets.
            inputStream = new BufferedInputStream(assetManager.Open(file));
            byte[] data = new byte[inputStream.Available()];
            inputStream.Read(data);
            inputStream.Close();

            // Create copy file in storage.
            File outFile = new File(context.FilesDir, file);
            FileOutputStream os = new FileOutputStream(outFile);
            os.Write(data);
            os.Close();
            // Return a path to file which may be read in common way.
            return outFile.AbsolutePath;
        }
        catch (IOException ex)
        {
            Log.Info(Tag, "Failed to upload a file: " + ex.Message);
        }
        return "";
    }

    private static string Tag = "OpenCV/Sample/MobileNet";
    private static string[] classNames = {"background",
        "aeroplane", "bicycle", "bird", "boat",
        "bottle", "bus", "car", "cat", "chair",
        "cow", "diningtable", "dog", "horse",
        "motorbike", "person", "pottedplant",
        "sheep", "sofa", "train", "tvmonitor"};

    private Net net;
    private CameraBridgeViewBase mOpenCvCameraView;
}
