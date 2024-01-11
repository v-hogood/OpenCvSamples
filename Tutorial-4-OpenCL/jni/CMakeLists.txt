cmake_minimum_required(VERSION 3.6)

set(target JNIpart)
project(${target} C CXX)

set(ANDROID_OPENCV_COMPONENTS "opencv_java" CACHE STRING "")
message(STATUS "ANDROID_ABI=${ANDROID_ABI}")
find_package(OpenCV REQUIRED COMPONENTS ${ANDROID_OPENCV_COMPONENTS})

file(GLOB srcs *.cpp *.c)
file(GLOB hdrs *.hpp *.h)

include_directories("${CMAKE_CURRENT_LIST_DIR}" $ENV{OPENCL_SDK}/include)
link_directories($ENV{OPENCL_SDK}/lib/${ANDROID_ABI})
add_library(${target} SHARED ${srcs} ${hdrs})
target_link_libraries(${target} ${ANDROID_OPENCV_COMPONENTS} -lGLESv2 -lEGL -lOpenCL)