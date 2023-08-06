// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System.CodeDom.Compiler;
using Foundation;
using UIKit;

namespace HelloIosVideo
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIImageView imageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIButton button { get; set; }

        void ReleaseDesignerOutlets()
        {
            imageView?.Dispose();
            imageView = null;

            button?.Dispose();
            button = null;
        }
    }
}
