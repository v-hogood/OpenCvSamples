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
            MCamera.GetParameters().SupportedColorEffects;

        public bool IsEffectSupported =>
            (MCamera.GetParameters().ColorEffect != null);

        public string Effect
        {
            get => MCamera.GetParameters().ColorEffect;
            set
            {
                Camera.Parameters parms = MCamera.GetParameters();
                parms.ColorEffect = value;
                MCamera.SetParameters(parms);
            }
        }     

        public IList<Size> ResolutionList =>
            MCamera.GetParameters().SupportedPreviewSizes;

        public Size Resolution
        {
            get => MCamera.GetParameters().PreviewSize;
            set
            {
                DisconnectCamera();
                MMaxHeight = value.Height;
                MMaxWidth = value.Width;
                ConnectCamera(Width, Height);
            }
        }

        public void TakePicture(string fileName)
        {
            Log.Info(Tag, "Taking picture");
            this.mPictureFileName = fileName;
            // Postview and jpeg are sent in the same buffers if the queue is not empty when performing a capture.
            // Clear up buffers to avoid mCamera.takePicture to be stuck because of a memory issue
            MCamera.SetPreviewCallback(null);

            // PictureCallback is implemented by the current class
            MCamera.TakePicture(null, null, this);
        }

        public void OnPictureTaken(byte[] data, Camera camera)
        {
            Log.Info(Tag, "Saving a bitmap to file");
            // The camera preview was automatically stopped. Start it again.
            MCamera.StartPreview();
            MCamera.SetPreviewCallback(this);

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
