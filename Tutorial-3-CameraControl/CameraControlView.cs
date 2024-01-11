using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Util;
using Java.IO;
using OpenCV.Android;
using Camera = Android.Hardware.Camera;
using Environment = Android.OS.Environment;
using IOException = Java.IO.IOException;
using Size = Android.Hardware.Camera.Size;
using Thread = Java.Lang.Thread;
using Uri = Android.Net.Uri;

namespace CameraControl
{
#pragma warning disable 0618
#pragma warning disable CA1422
    public class CameraControlView : JavaCameraView,
        Camera.IPictureCallback
    {
        new private const string Tag = "Sample::CameraControlView";
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
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                Bitmap bitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length);
                new Thread(() =>
                {
                    ContentResolver resolver = Context.ContentResolver;
                    ContentValues contentValues = new ContentValues();
                    contentValues.Put(MediaStore.MediaColumns.DisplayName, mPictureFileName);
                    contentValues.Put(MediaStore.MediaColumns.MimeType, "image/jpg");
                    contentValues.Put(MediaStore.MediaColumns.RelativePath, Environment.DirectoryPictures);
                    Uri imageUri = resolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);
                    try
                    {
                        Stream fos = resolver.OpenOutputStream(imageUri!);
                        bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, fos);
                        fos?.Close();
                    }
                    catch (Java.IO.IOException e)
                    {
                        Log.Error("PictureDemo", "Exception in photoCallback", e);
                    }
                });
            }
            else
            {
                mPictureFileName = Android.OS.Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).Path
                                   + "/" + mPictureFileName;
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
#pragma warning restore CA1422
#pragma warning restore 0618
}
