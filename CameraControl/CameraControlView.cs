using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Java.IO;
using OpenCV.Android;
using Camera = Android.Hardware.Camera;
using Size = Android.Hardware.Camera.Size;

namespace CameraControl
{
    public class CameraControlView : JavaCameraView,
        Camera.IPictureCallback
    {
        private const string Tag = "Sample::CameraControlView";
        private string mPictureFileName;

        public CameraControlView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        { }

        public IList<string> EffectList =>
            Camera.GetParameters().SupportedColorEffects;

        public bool IsEffectSupported =>
            (Camera.GetParameters().ColorEffect != null);

        public string Effect
        {
            get => Camera.GetParameters().ColorEffect;
            set
            {
                Camera.Parameters parms = Camera.GetParameters();
                parms.ColorEffect = value;
                Camera.SetParameters(parms);
            }
        }     

        public IList<Size> ResolutionList =>
            Camera.GetParameters().SupportedPreviewSizes;

        public Size Resolution
        {
            get => Camera.GetParameters().PreviewSize;
            set
            {
                DisconnectCamera();
                MaxHeight = value.Height;
                MaxWidth = value.Width;
                ConnectCamera(Width, Height);
            }
        }

        public void TakePicture(string fileName)
        {
            Log.Info(Tag, "Taking picture");
            this.mPictureFileName = fileName;
            // Postview and jpeg are sent in the same buffers if the queue is not empty when performing a capture.
            // Clear up buffers to avoid mCamera.takePicture to be stuck because of a memory issue
            Camera.SetPreviewCallback(null);

            // PictureCallback is implemented by the current class
            Camera.TakePicture(null, null, this);
        }

        public void OnPictureTaken(byte[] data, Camera camera)
        {
            Log.Info(Tag, "Saving a bitmap to file");
            // The camera preview was automatically stopped. Start it again.
            Camera.StartPreview();
            Camera.SetPreviewCallback(this);

            // Write the image in a file (in jpeg format)
            try
            {
                FileOutputStream fos = new FileOutputStream(mPictureFileName);

                fos.Write(data);
                fos.Close();

            }
            catch (IOException e)
            {
                Log.Error("PictureDemo", "Exception in photoCallback", e);
            }
        }
    }
}
