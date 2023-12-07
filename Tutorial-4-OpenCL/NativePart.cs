using System.Runtime.InteropServices;

namespace OpenCL;

public static class NativePart
{
    public const int ProcessingModeNoProcessing = 0;
    public const int ProcessingModeCpu = 1;
    public const int ProcessingModeOclDirect = 2;
    public const int ProcessingModeOclOcv = 3;
                        
    [DllImport("libJNIpart", EntryPoint = "Java_org_opencv_samples_opencl_NativePart_initCL")]
    public  extern static int InitCL(IntPtr env, IntPtr jniClass);
    [DllImport("libJNIpart", EntryPoint = "Java_org_opencv_samples_opencl_NativePart_closeCL")]
    public extern static void CloseCL(IntPtr env, IntPtr jniClass);
    [DllImport("libJNIpart", EntryPoint = "Java_org_opencv_samples_opencl_NativePart_processFrame")]
    public extern static void ProcessFrame(IntPtr env, IntPtr jniClass,
        int tex1, int tex2, int w, int h, int mode);
}
