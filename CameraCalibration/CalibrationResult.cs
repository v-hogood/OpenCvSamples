using Android.App;
using Android.Content;
using Android.Util;
using OpenCV.Core;

namespace CameraCalibration
{
    public abstract class CalibrationResult
    {
        private const string Tag = "OCV::CalibrationResult";

        private const int CameraMatrixRows = 3;
        private const int CameraMatrixCols = 3;
        private const int DistortionCoefficientsSize = 5;

        public static void Save(Activity activity, Mat cameraMatrix, Mat distortionCoefficients)
        {
            ISharedPreferences sharedPref = activity.GetPreferences(FileCreationMode.Private);
            ISharedPreferencesEditor editor = sharedPref.Edit();

            double[] cameraMatrixArray = new double[CameraMatrixRows * CameraMatrixCols];
            cameraMatrix.Get(0,  0, cameraMatrixArray);
            for (int i = 0; i < CameraMatrixRows; i++)
            {
                for (int j = 0; j < CameraMatrixCols; j++)
                {
                    int id = i * CameraMatrixRows + j;
                    editor.PutFloat(id.ToString(), (float)cameraMatrixArray[id]);
                }
            }

            double[] distortionCoefficientsArray = new double[DistortionCoefficientsSize];
            distortionCoefficients.Get(0, 0, distortionCoefficientsArray);
            int shift = CameraMatrixRows * CameraMatrixCols;
            for (int i = shift; i < DistortionCoefficientsSize + shift; i++)
            {
                editor.PutFloat(i.ToString(), (float)distortionCoefficientsArray[i-shift]);
            }

            editor.Apply();
            Log.Info(Tag, "Saved camera matrix: " + cameraMatrix.Dump());
            Log.Info(Tag, "Saved distortion coefficients: " + distortionCoefficients.Dump());
        }

        public static bool TryLoad(Activity activity, Mat cameraMatrix, Mat distortionCoefficients)
        {
            ISharedPreferences sharedPref = activity.GetPreferences(FileCreationMode.Private);
            if (sharedPref.GetFloat("0", -1) == -1)
            {
                Log.Info(Tag, "No previous calibration results found");
                return false;
            }

            double[] cameraMatrixArray = new double[CameraMatrixRows * CameraMatrixCols];
            for (int i = 0; i < CameraMatrixRows; i++)
            {
                for (int j = 0; j < CameraMatrixCols; j++)
                {
                    int id = i * CameraMatrixRows + j;
                    cameraMatrixArray[id] = sharedPref.GetFloat(id.ToString(), -1);
                }
            }
            cameraMatrix.Put(0, 0, cameraMatrixArray);
            Log.Info(Tag, "Loaded camera matrix: " + cameraMatrix.Dump());

            double[] distortionCoefficientsArray = new double[DistortionCoefficientsSize];
            int shift = CameraMatrixRows * CameraMatrixCols;
            for (int i = shift; i < DistortionCoefficientsSize + shift; i++)
            {
                distortionCoefficientsArray[i - shift] = sharedPref.GetFloat(i.ToString(), -1);
            }
            distortionCoefficients.Put(0, 0, distortionCoefficientsArray);
            Log.Info(Tag, "Loaded distortion coefficients: " + distortionCoefficients.Dump());

            return true;
        }
    }
}
