using OpenCvSdk;

namespace ColorBlobDetection;

public class ColorBlobDetector
{
    // Lower and Upper bounds for range checking in HSV color space
    Scalar lowerBound = new(0.0);
    Scalar upperBound = new(0.0);
    // Minimum contour area in percent for contours filtering
    const float minContourArea = 0.1f;
    // Color radius for range checking in HSV color space
    Scalar colorRadius = new(25.0, 50.0, 50.0, 0.0);

    public Mat Spectrum = new();
    public NSArray<Point2i>[] Contours = Array.Empty<NSArray<Point2i>>();

    // Cache
    Mat pyrDownMat = new();
    Mat hsvMat = new();
    Mat mask = new();
    Mat dilatedMask = new();
    Mat hierarchy = new();

    public void SetHsvColor(Scalar hsvColor)
    {
        var minH = (hsvColor.Val[0].DoubleValue >= colorRadius.Val[0].DoubleValue) ? hsvColor.Val[0].DoubleValue - colorRadius.Val[0].DoubleValue : 0;
        var maxH = (hsvColor.Val[0].DoubleValue + colorRadius.Val[0].DoubleValue <= 255) ? hsvColor.Val[0].DoubleValue + colorRadius.Val[0].DoubleValue : 255;

        lowerBound = new Scalar(minH, hsvColor.Val[1].DoubleValue - colorRadius.Val[1].DoubleValue, hsvColor.Val[2].DoubleValue - colorRadius.Val[2].DoubleValue, 0);
        upperBound = new Scalar(maxH, hsvColor.Val[1].DoubleValue + colorRadius.Val[1].DoubleValue, hsvColor.Val[2].DoubleValue + colorRadius.Val[2].DoubleValue, 255);

        var spectrumHsv = new Mat(rows: 1, cols: (Int32)(maxH-minH), type:CvType.Cv8uc3);

        for (int j = 0; j < (int)(maxH - minH); j++)
        {
            var tmp = new NSNumber[] { (double)((int)minH + j), 255, 255 };
            spectrumHsv.Put(row: 0, col: j, data: tmp);
        }

        Imgproc.CvtColor(src: spectrumHsv, dst: Spectrum, code: ColorConversionCodes.Hsv2rgbFull, dstCn: 4);
    }

    public void Process(Mat rgbaImage)
    {
        Imgproc.PyrDown(src: rgbaImage, dst: pyrDownMat);
        Imgproc.PyrDown(src: pyrDownMat, dst: pyrDownMat);

        Imgproc.CvtColor(src: pyrDownMat, dst: hsvMat, code: ColorConversionCodes.Rgb2hsvFull);

        Core.InRange(src: hsvMat, lowerb: lowerBound, upperb: upperBound, dst: mask);
        Imgproc.Dilate(src: mask, dst: dilatedMask, kernel: new Mat());

        var contoursTmp = new NSMutableArray<NSMutableArray<Point2i>>();

        Imgproc.FindContours(image: dilatedMask, contours: contoursTmp, hierarchy: hierarchy, mode: RetrievalModes.External, method: ContourApproximationModes.Simple);

        // Find max contour area
        var maxArea = 0.0;
        foreach (var contour in contoursTmp)
        {
            var contourMat = new MatOfPoint2i(array: contour.ToArray<Point2i>());
            var area = Imgproc.ContourArea(contour: contourMat);
            maxArea = Math.Max(area, maxArea);
        }

        // Filter contours by area and resize to fit the original image size
        Contours = Array.Empty<NSArray<Point2i>>();
        foreach (var contour in contoursTmp)
        {
            var contourMat = new MatOfPoint2i(array: contour.ToArray<Point2i>());
            if (Imgproc.ContourArea(contour: contourMat) > ColorBlobDetector.minContourArea * maxArea)
            {
                Core.Multiply(src1: contourMat, srcScalar: new Scalar(4.0, 4.0), dst: contourMat);
                Contours = Contours.Append<NSArray<Point2i>>(NSArray<Point2i>.FromNSObjects(contourMat.ToArray())!).ToArray();
            }
        }
    }
}
