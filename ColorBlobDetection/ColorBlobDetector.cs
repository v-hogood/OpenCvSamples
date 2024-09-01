#if __ANDROID__
using Android.Runtime;
using OpenCV.Core;
using OpenCV.ImgProc;
#elif __IOS__
using Foundation;
using OpenCvSdk;
#endif

namespace ColorBlobDetection;

public class ColorBlobDetector
{
    // Lower and Upper bounds for range checking in HSV color space
    Scalar lowerBound = new(0);
    Scalar upperBound = new(0);
    // Minimum contour area in percent for contours filtering
    const float minContourArea = 0.1f;
    // Color radius for range checking in HSV color space
    Scalar colorRadius = new(25, 50, 50, 0);

    private Mat spectrum = new();
    public Mat Spectrum => spectrum;

#if __ANDROID__
    private MatOfPoint[] contours = Array.Empty<MatOfPoint>();
    public MatOfPoint[] Contours => contours;
#elif __IOS__
    private NSArray<Point2i>[] contours = Array.Empty<NSArray<Point2i>>();
    public NSArray<Point2i>[] Contours => contours;
#endif

    // Cache
    Mat pyrDownMat = new();
    Mat hsvMat = new();
    Mat mask = new();
    Mat dilatedMask = new();
    Mat hierarchy = new();

    public void SetHsvColor(Scalar hsvColor)
    {
        var minH = Math.Max((double) hsvColor.Val[0] - (double) colorRadius.Val[0], 0);
        var maxH = Math.Min((double) hsvColor.Val[0] + (double) colorRadius.Val[0], 255);

        lowerBound = new Scalar(minH, (double) hsvColor.Val[1] - (double) colorRadius.Val[1], (double) hsvColor.Val[2] - (double) colorRadius.Val[2], 0);
        upperBound = new Scalar(maxH, (double) hsvColor.Val[1] + (double) colorRadius.Val[1], (double) hsvColor.Val[2] + (double) colorRadius.Val[2], 255);

        var spectrumHsv = new Mat(rows: 1, cols: (int)(maxH-minH), type:CvType.Cv8uc3);

        for (int j = 0; j < (int)(maxH - minH); j++)
        {
#if __ANDROID__
            var tmp = new byte[] { (byte)(minH + j), 255, 255 };
#elif __IOS__
            var tmp = new NSNumber[] { minH + j, 255, 255 };
#endif
            spectrumHsv.Put(row: 0, col: j, data: tmp);
        }

#if __ANDROID__
        Imgproc.CvtColor(src: spectrumHsv, dst: spectrum, code: Imgproc.ColorHsv2rgbFull, dstCn: 4);
#elif __IOS__
        Imgproc.CvtColor(src: spectrumHsv, dst: Spectrum, code: ColorConversionCodes.Hsv2rgbFull, dstCn: 4);
#endif
    }

    public void Process(Mat rgbaImage)
    {
        Imgproc.PyrDown(src: rgbaImage, dst: pyrDownMat);
        Imgproc.PyrDown(src: pyrDownMat, dst: pyrDownMat);

#if __ANDROID__
        Imgproc.CvtColor(src: pyrDownMat, dst: hsvMat, code: Imgproc.ColorRgb2hsvFull);
#elif __IOS__
        Imgproc.CvtColor(src: pyrDownMat, dst: hsvMat, code: ColorConversionCodes.Rgb2hsvFull);
#endif

        Core.InRange(src: hsvMat, lowerb: lowerBound, upperb: upperBound, dst: mask);
        Imgproc.Dilate(src: mask, dst: dilatedMask, kernel: new());

#if __ANDROID__
        var contoursTmp = new JavaList<MatOfPoint>();
        Imgproc.FindContours(image: dilatedMask, contours: contoursTmp, hierarchy: hierarchy, mode: Imgproc.RetrExternal, method: Imgproc.ChainApproxSimple);
#elif __IOS__
        var contoursTmp = new NSMutableArray<NSMutableArray<Point2i>>();
        Imgproc.FindContours(image: dilatedMask, contours: contoursTmp, hierarchy: hierarchy, mode: RetrievalModes.External, method: ContourApproximationModes.Simple);
#endif

        // Find max contour area
        var maxArea = 0.0;
        foreach (var contour in contoursTmp)
        {
#if __ANDROID__
            var area = Imgproc.ContourArea(contour: contour);
#elif __IOS__
            var contourMat = new MatOfPoint2i(array: contour.ToArray<Point2i>());
            var area = Imgproc.ContourArea(contour: contourMat);
#endif
            maxArea = Math.Max(area, maxArea);
        }

        // Filter contours by area and resize to fit the original image size
#if __ANDROID__
        contours = Array.Empty<MatOfPoint>();
#elif __IOS__
        contours = Array.Empty<NSArray<Point2i>>();
#endif
        foreach (var contour in contoursTmp)
        {
#if __ANDROID__
            if (Imgproc.ContourArea(contour: contour) > minContourArea * maxArea)
            {
                Core.Multiply(src1: contour, src2: new Scalar(4.0, 4.0), dst: contour);
                contours = contours.Append(contour).ToArray();
            }
#elif __IOS__
            var contourMat = new MatOfPoint2i(array: contour.ToArray<Point2i>());
            if (Imgproc.ContourArea(contour: contourMat) > ColorBlobDetector.minContourArea * maxArea)
            {
                Core.Multiply(src1: contourMat, srcScalar: new Scalar(4.0, 4.0), dst: contourMat);
                contours = contours.Append(NSArray<Point2i>.FromNSObjects(contourMat.ToArray())).ToArray();
            }
#endif
        }
    }
}
