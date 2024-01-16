LOCAL_PATH := $(call my-dir)

# add OpenCV
include $(CLEAR_VARS)
OPENCV_INSTALL_MODULES:=on
ifdef OPENCV_ANDROID_SDK
  ifneq ("","$(wildcard $(OPENCV_ANDROID_SDK)/OpenCV.mk)")
    include ${OPENCV_ANDROID_SDK}/OpenCV.mk
  else
    include ${OPENCV_ANDROID_SDK}/sdk/native/jni/OpenCV.mk
  endif
else
  include ../../sdk/native/jni/OpenCV.mk
endif

ifndef OPENCL_SDK
  $(warning Specify OPENCL_SDK to Android OpenCL SDK location)
else
  # add OpenCL
  LOCAL_CFLAGS += -DOPENCL_FOUND
  LOCAL_C_INCLUDES += $(OPENCL_SDK)/$(TARGET_ARCH_ABI)/include
  LOCAL_LDLIBS += -L$(OPENCL_SDK)/$(TARGET_ARCH_ABI)/lib -lOpenCL
endif

LOCAL_MODULE    := JNIpart
LOCAL_SRC_FILES := jni.cpp CLprocessor.cpp
LOCAL_LDLIBS    += -llog -lGLESv2 -lEGL
include $(BUILD_SHARED_LIBRARY)