using System;
using System.Runtime.InteropServices;
using Android.Runtime;
using OpenCV.Core;
using String = Java.Lang.String;

namespace FaceDetection
{
    public class DetectionBasedTracker
    {
        public DetectionBasedTracker(string cascadeName, int minFaceSize)
        {
            String s = new String(cascadeName);
            mNativeObj = NativeCreateObject(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), s.Handle, minFaceSize);
        }

        public void Start()
        {
            NativeStart(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), mNativeObj);
        }

        public void Stop()
        {
            NativeStop(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), mNativeObj);
        }

        public void SetMinFaceSize(int size)
        {
            NativeSetFaceSize(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), mNativeObj, size);
        }

        public void detect(Mat imageGray, MatOfRect faces)
        {
            NativeDetect(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), mNativeObj, imageGray.NativeObjAddr, faces.NativeObjAddr);
        }

        public void Release()
        {
            NativeDestroyObject(JNIEnv.Handle, JNIEnv.FindClass(typeof(Java.Lang.Object)), mNativeObj);
            mNativeObj = IntPtr.Zero;
        }

        private IntPtr mNativeObj = IntPtr.Zero;

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeCreateObject")]
        private extern static IntPtr NativeCreateObject(IntPtr env, IntPtr jniClass, IntPtr cascadeName, int minFaceSize);

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeDestroyObject")]
        private extern static void NativeDestroyObject(IntPtr env, IntPtr jniClass, IntPtr thiz);

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeStart")]
        private extern static void NativeStart(IntPtr env, IntPtr jniClass, IntPtr thiz);

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeStop")]
        private extern static void NativeStop(IntPtr env, IntPtr jniClass, IntPtr thiz);

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeSetFaceSize")]
        private extern static void NativeSetFaceSize(IntPtr env, IntPtr jniClass, IntPtr thiz, int size);

        [DllImport("libdetection_based_tracker", EntryPoint = "Java_org_opencv_samples_facedetect_DetectionBasedTracker_nativeDetect")]
        private extern static void NativeDetect(IntPtr env, IntPtr jniClass, IntPtr thiz, long inputImage, long faces);
    }
}
