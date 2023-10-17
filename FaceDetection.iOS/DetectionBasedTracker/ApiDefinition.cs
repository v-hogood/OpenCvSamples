using Foundation;
using ObjCRuntime;
using OpenCvSdk;

namespace FaceDetection;

// @interface DetectionBasedTracker : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
interface DetectionBasedTracker
{
	// -(instancetype)initWithCascadeName:(NSString *)cascadeName minFaceSize:(int)minFaceSize;
	[Export ("initWithCascadeName:minFaceSize:")]
	NativeHandle Constructor (string cascadeName, int minFaceSize);

	// -(void)start;
	[Export ("start")]
	void Start ();

	// -(void)stop;
	[Export ("stop")]
	void Stop ();

	// -(void)setFaceSize:(int)size;
	[Export ("setFaceSize:")]
	void SetFaceSize (int size);

	// -(void)detect:(Mat *)imageGray faces:(NSMutableArray<Rect2i *> *)faces;
	[Export ("detect:faces:")]
	void Detect (Mat imageGray, NSMutableArray<Rect2i> faces);
}
