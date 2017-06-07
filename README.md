# MapAPI
## Compiling
### MapAPI
To compile the MapAPI project in the MapAPI solution download [ASP.NET Core](https://www.microsoft.com/net/core). For the application to run properly, the IdentifyRectangles project in the OpenCVComponents solution must be compiled and in your PATH.
### IdentifyRectangles
To compile the IdentifyRectangles project in the OpenCVComponents with Visual Studio download the [OpenCV Win Pack](https://sourceforge.net/projects/opencvlibrary/files/opencv-win/3.2.0/opencv-3.2.0-vc14.exe/download) and put the build folder in OpenCVComponents/OpenCV. You then have to add OpenCVComponents\OpenCV\build\x64\vc14\bin to your PATH. To compile on Linux [install OpenCV](https://www.linuxhint.com/how-to-install-opencv-on-ubuntu/) and [compile with CMake](http://docs.opencv.org/2.4/doc/tutorials/introduction/linux_gcc_cmake/linux_gcc_cmake.html).
