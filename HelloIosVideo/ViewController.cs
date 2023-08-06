using AVFoundation;
using ObjCRuntime;
using OpenCvSdk;

namespace HelloIosVideo
{
    public partial class ViewController : UIViewController,
        ICvVideoCameraDelegate2
    {
        public ViewController(NativeHandle handle) : base(handle) { }

        CvVideoCamera2 videoCamera;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            this.videoCamera = new CvVideoCamera2(imageView);
            this.videoCamera.Delegate = this;
            this.videoCamera.DefaultAVCaptureDevicePosition = AVCaptureDevicePosition.Front;
            this.videoCamera.DefaultAVCaptureSessionPreset = AVCaptureSession.Preset352x288;
            this.videoCamera.DefaultAVCaptureVideoOrientation = AVCaptureVideoOrientation.Portrait;
            this.videoCamera.DefaultFPS = 30;
            this.videoCamera.GrayscaleMode = false;
        }

        public override void ViewDidUnload()
        {
#pragma warning disable CA1422
            base.ViewDidUnload();
#pragma warning restore CA1422
            // Release any retained subviews of the main view.
        }

        public bool ShouldAutorotateInterfaceOrientation(UIInterfaceOrientation interfaceOrientation)
        {
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                return (interfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
            }
            else
            {
                return true;
            }
        }

        [Export("actionStart:")]
        public void ActionStart(UIButton sender)
        {
            this.videoCamera.Start();
        }

        public void ProcessImage(Mat image)
        {
            // Do some OpenCV stuff with the image
            Mat image_copy = new Mat(image.Size(), image.Type);
            Imgproc.CvtColor(image, image_copy, ColorConversionCodes.Bgra2bgr);

            // invert image
            Core.Bitwise_not(image_copy, image_copy);
            Imgproc.CvtColor(image_copy, image, ColorConversionCodes.Bgr2bgra);
        }
    }
}
