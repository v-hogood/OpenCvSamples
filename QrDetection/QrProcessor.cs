using System.Runtime.CompilerServices;
using OpenCV.Core;
using OpenCV.ImgProc;
using OpenCV.ObjDetect;

namespace QrDetection;

public class QrProcessor
{
    private GraphicalCodeDetector detector;
    private const string Tag = "QRProcessor";
    private Scalar LineColor = new Scalar(255, 0, 0);
    private Scalar FontColor = new Scalar(0, 0, 255);

    public QrProcessor(bool useArucoDetector)
    {
        if (useArucoDetector)
            detector = new QRCodeDetectorAruco();
        else
            detector = new QRCodeDetector();
    }

    private bool FindQRs(Mat inputFrame, List<String> decodedInfo, MatOfPoint points,
                         bool tryDecode, bool multiDetect)
    {
        bool result = false;
        if (multiDetect)
        {
            if (tryDecode)
                result = detector.DetectAndDecodeMulti(inputFrame, decodedInfo, points);
            else
                result = detector.DetectMulti(inputFrame, points);
        }
        else
        {
            if (tryDecode)
            {
                String s = detector.DetectAndDecode(inputFrame, points);
                result = !points.Empty();
                if (result)
                    decodedInfo.Add(s);
            }
            else
            {
                result = detector.Detect(inputFrame, points);
            }
        }
        return result;
    }

    private void renderQRs(Mat inputFrame, List<String> decodedInfo, MatOfPoint points)
    {
        for (int i = 0; i < points.Rows(); i++)
        {
            for (int j = 0; j < points.Cols(); j++)
            {
                Point pt1 = new Point(points.Get(i, j));
                Point pt2 = new Point(points.Get(i, (j + 1) % 4));
                Imgproc.Line(inputFrame, pt1, pt2, LineColor, 3);
            }
            if (decodedInfo.Count > 0)
            {
                string decode = decodedInfo[i];
                if (decode.Length > 15)
                {
                    decode = decode.Substring(0, 12) + "...";
                }
                int[] baseline = { 0 };
                Size textSize = Imgproc.GetTextSize(decode, Imgproc.FontHersheyComplex, .95, 3, baseline);
                Scalar sum = Core.SumElems(points.Row(i));
                Point start = new Point(sum.Val[0] / 4 - textSize.Width / 2, sum.Val[1] / 4 - textSize.Height / 2);
                Imgproc.PutText(inputFrame, decode, start, Imgproc.FontHersheyComplex, .95, FontColor, 3);
            }
        }
    }

    /* this method to be called from the outside. It processes the frame to find QR codes. */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Mat handleFrame(Mat inputFrame, bool tryDecode, bool multiDetect)
    {
        List<string> decodedInfo = new();
        MatOfPoint points = new MatOfPoint();
        bool result = FindQRs(inputFrame, decodedInfo, points, tryDecode, multiDetect);
        if (result)
        {
            renderQRs(inputFrame, decodedInfo, points);
        }
        points.Release();
        return inputFrame;
    }
}
