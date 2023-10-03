using ObjCRuntime;
using OpenCvSdk;

namespace ColorBlobDetection;

public partial class ViewController : UIViewController,
    ICvVideoCameraDelegate2
{
    bool isColorSelected = false;
    Mat? rgba;
    ColorBlobDetector detector = new();
    Mat spectrum = new();
    Scalar blobColorRgba = new(255.0);
    Scalar blobColorHsv = new(255.0);
    Size2i SpectrumSize = new(width: 200, height: 64);
    Scalar ContourColor = new(255.0, 0.0, 0.0, 255.0);
    float cameraHolderWidth = 0;
    float cameraHolderHeight = 0;

    public ViewController(NativeHandle handle) : base(handle) { }

    public void ProcessImage(Mat image)
    {
        rgba = image;
        if (isColorSelected)
        {
            detector.Process(rgbaImage: image);
            var contours = detector.Contours;
            Console.WriteLine("Contours count: " + contours.Length);
            Imgproc.DrawContours(image: image, contours: contours, contourIdx: -1, color: ContourColor);

            var colorLabel = image.Submat(rowStart: 4, rowEnd: 68, colStart: 4, colEnd: 68);
            colorLabel.SetToScalar(blobColorRgba);

            var spectrumLabel = image.Submat(rowStart: 4, rowEnd: 4 + spectrum.Rows, colStart: 70, colEnd: 70 + spectrum.Cols);
            spectrum.CopyTo(spectrumLabel);
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

    public override void ViewDidLayoutSubviews()
    {
        if (UIDevice.CurrentDevice.Orientation.IsLandscape())
        {
            cameraHolderWidth = (float)cameraHolder.Bounds.Height;
            cameraHolderHeight = (float)cameraHolder.Bounds.Width;
        }
        else
        {
            cameraHolderWidth = (float)cameraHolder.Bounds.Width;
            cameraHolderHeight = (float)cameraHolder.Bounds.Height;
        }
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        var aRgba = rgba!;
        if (touches.Count == 1)
        {
            var touch = touches.ToArray<UITouch>()[0];
            var cols = (float)aRgba.Cols;
            var rows = (float)aRgba.Rows;

            var orientation = UIDevice.CurrentDevice.Orientation;
            var x = touch.LocationInView(cameraHolder).X;
            var y = touch.LocationInView(cameraHolder).Y;
            if (orientation == UIDeviceOrientation.LandscapeLeft)
            {
                var tempX = x;
                x = cameraHolder.Bounds.Height - y;
                y = tempX;
            }
            else if (orientation == UIDeviceOrientation.LandscapeRight)
            {
                var tempY = y;
                y = cameraHolder.Bounds.Width - x;
                x = tempY;
            }

            x = x * (cols / cameraHolderWidth);
            y = y * (rows / cameraHolderHeight);

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
            {
                return;
            }

            var touchedRect = new Rect2i();

            touchedRect.X = (x>4) ? (int)x-4 : 0;
            touchedRect.Y = (y>4) ? (int)y-4 : 0;

            touchedRect.Width = (x+4 < cols) ? (int)x + 4 - touchedRect.X : (int)cols - touchedRect.X;
            touchedRect.Height = (y+4 < rows) ? (int)y + 4 - touchedRect.Y : (int)rows - touchedRect.Y;

            var touchedRegionRgba = aRgba.SubmatRoi(touchedRect);

            var touchedRegionHsv = new Mat();
            Imgproc.CvtColor(src: touchedRegionRgba, dst: touchedRegionHsv, code: ColorConversionCodes.Rgb2hsvFull);

            // Calculate average color of touched region
            blobColorHsv = Core.SumElems(src: touchedRegionHsv);
            var pointCount = touchedRect.Width * touchedRect.Height;
            blobColorHsv = blobColorHsv.Mul(Scalar.All(1.0 / (double)pointCount));

            blobColorRgba = ConvertScalarHsv2Rgba(hsvColor: blobColorHsv);

            Console.WriteLine("Touched rgba color: " + blobColorRgba.Val[0] + " " + blobColorRgba.Val[1] + " " + blobColorRgba.Val[2] + " " + blobColorRgba.Val[3]);

            detector.SetHsvColor(hsvColor: blobColorHsv);

            Imgproc.Resize(src: detector.Spectrum, dst: spectrum, dsize: SpectrumSize, fx: 0, fy: 0, interpolation: (int)InterpolationFlags.InterLinearExact);

            isColorSelected = true;
        }
    }

    Scalar ConvertScalarHsv2Rgba(Scalar hsvColor)
    {
        var pointMatRgba = new Mat();
        var pointMatHsv = new Mat(rows: 1, cols: 1, type: CvType.Cv8uc3, scalar: hsvColor);
        Imgproc.CvtColor(src: pointMatHsv, dst: pointMatRgba, code: ColorConversionCodes.Hsv2rgbFull, dstCn: 4);
        var elementData = pointMatRgba.Get(row: 0, col: 0);
        return new Scalar(vals: elementData);
    }
}
