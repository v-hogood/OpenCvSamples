using ObjCRuntime;
using OpenCvSdk;

namespace FaceDetection;

public static class Extensions
{
    public static void RotateClockwise(this Rect2i rect, int parentHeight)
    {
        var tmpX = rect.X;
        rect.X = parentHeight - (rect.Y + rect.Height);
        rect.Y = tmpX;
        SwapDims(rect);
    }

    public static void RotateCounterclockwise(this Rect2i rect, int parentWidth)
    {
        var tmpY = rect.Y;
        rect.Y = parentWidth - (rect.X + rect.Width);
        rect.X = tmpY;
        SwapDims(rect);
    }

    public static void SwapDims(this Rect2i rect)
    {
        var tmpWidth = rect.Width;
        rect.Width = rect.Height;
        rect.Height = tmpWidth;
    }
}

public partial class ViewController : UIViewController,
    ICvVideoCameraDelegate2
{
    CascadeClassifier swiftDetector = new CascadeClassifier(NSBundle.MainBundle.PathForResource(name: "lbpcascade_frontalface", ofType: "xml")!);
    DetectionBasedTracker nativeDetector = new DetectionBasedTracker(cascadeName: NSBundle.MainBundle.PathForResource(name: "lbpcascade_frontalface", ofType: "xml")!, minFaceSize: 0);
    Mat? rgba;
    Mat gray = new();
    float relativeFaceSize = 0.2f;
    int absoluteFaceSize = 0;
    Scalar FaceRectColor = new Scalar(0.0, 255.0, 0.0, 255.0);
    int FaceRectThickness = 4;

    public ViewController(NativeHandle handle) : base(handle) { }

    public void ProcessImage(Mat image)
    {
        var orientation = UIDevice.CurrentDevice.Orientation;
        switch(orientation)
        {
            case UIDeviceOrientation.LandscapeLeft:
                rgba = new();
                Core.Rotate(src: image, dst: rgba!, rotateCode: RotateFlags.Rotate90Counterclockwise);
                break;
            case UIDeviceOrientation.LandscapeRight:
                rgba = new();
                Core.Rotate(src: image, dst: rgba!, rotateCode: RotateFlags.Rotate90Clockwise);
                break;
            default:
                rgba = image;
                break;
        }

        Imgproc.CvtColor(src: rgba!, dst: gray, code: ColorConversionCodes.Rgb2gray);
     
        if (absoluteFaceSize == 0)
        {
            var height = gray.Rows;
            if (Math.Round((float)height * relativeFaceSize) > 0)
            {
                absoluteFaceSize = (int)(Math.Round((float)height) * relativeFaceSize);
            }
        }

        var faces = new NSMutableArray<Rect2i>();

        swiftDetector.DetectMultiScale(image: gray, objects: faces, scaleFactor: 1.1, minNeighbors: 2, flags: 2, minSize: new Size2i(width: absoluteFaceSize, height: absoluteFaceSize), maxSize: new Size2i());
        //let facesArray = NSMutableArray()
        //nativeDetector!.detect(gray, faces: facesArray)
        //faces.append(contentsOf: facesArray)

        foreach (var face in faces)
        {
            if (orientation == UIDeviceOrientation.LandscapeLeft)
            {
                face.RotateClockwise(parentHeight: gray.Rows);
            }
            else if (orientation == UIDeviceOrientation.LandscapeRight)
            {
                face.RotateCounterclockwise(parentWidth: gray.Cols);
            }
            Imgproc.Rectangle(img: image, pt1: face.Tl, pt2: face.Br, color: FaceRectColor, thickness: FaceRectThickness);
        }
    }

    CvVideoCamera2? camera;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        camera = new CvVideoCamera2(parent: cameraHolder);
        camera.RotateVideo = true;
        camera.Delegate = this;
        camera?.Start();
    }
}
