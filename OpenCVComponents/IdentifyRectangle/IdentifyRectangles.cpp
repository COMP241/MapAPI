#include "opencv2/core.hpp"
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"

#include <iostream>
#include <math.h>

using namespace cv;
using namespace std;

int thresh = 50, N = 11;
const char* wndname = "Square Detection Demo";

// Helper function:
// Finds a cosine of angle between vectors
// From pt0->pt1 and from pt0->pt2
static double angle(Point pt1, Point pt2, Point pt0)
{
	double dx1 = pt1.x - pt0.x;
	double dy1 = pt1.y - pt0.y;
	double dx2 = pt2.x - pt0.x;
	double dy2 = pt2.y - pt0.y;
	return (dx1 * dx2 + dy1 * dy2) / sqrt((dx1 * dx1 + dy1 * dy1) * (dx2 * dx2 + dy2 * dy2) + 1e-10);
}

// Returns sequence of squares detected on the image.
// The sequence is stored in the specified memory storage
void findRectangles(Mat& image, vector<vector<Point> >& rectangles)
{
	// Blur will enhance edge detection
	Mat blurred(image);
	medianBlur(image, blurred, 9);

	Mat gray0(blurred.size(), CV_8U), gray;
	vector<vector<Point> > contours;

	// Find squares in every color plane of the image
	for (int c = 0; c < 3; c++)
	{
		int ch[] = {c, 0};
		mixChannels(&blurred, 1, &gray0, 1, ch, 1);

		// Try several threshold levels
		const int threshold_level = 2;
		for (int l = 0; l < threshold_level; l++)
		{
			// Use Canny instead of zero threshold level!
			// Canny helps to catch squares with gradient shading
			if (l == 0)
			{
				Canny(gray0, gray, 10, 20, 3);

				// Dilate helps to remove potential holes between edge segments
				dilate(gray, gray, Mat(), Point(-1, -1));
			}
			else
			{
				gray = gray0 >= (l + 1) * 255 / threshold_level;
			}

			// Find contours and store them in a list
			findContours(gray, contours, CV_RETR_LIST, CV_CHAIN_APPROX_SIMPLE);

			// Test contours
			vector<Point> approx;
			for (size_t i = 0; i < contours.size(); i++)
			{
				// Approximate contour with accuracy proportional
				// to the contour perimeter
				approxPolyDP(Mat(contours[i]), approx, arcLength(Mat(contours[i]), true) * 0.02, true);

				// Note: absolute value of an area is used because
				// area may be positive or negative - in accordance with the
				// contour orientation
				if (approx.size() == 4 &&
					fabs(contourArea(Mat(approx))) > 1000 &&
					isContourConvex(Mat(approx)))
				{
					double maxCosine = 0;

					for (int j = 2; j < 5; j++)
					{
						double cosine = fabs(angle(approx[j % 4], approx[j - 2], approx[j - 1]));
						maxCosine = MAX(maxCosine, cosine);
					}

					if (maxCosine < 0.3)
						rectangles.push_back(approx);
				}
			}
		}
	}
}

// The function draws all the squares in the image
static void drawSquares(Mat& image, const vector<vector<Point> >& rectangles)
{
	for (size_t i = 0; i < rectangles.size(); i++)
	{
		const Point* p = &rectangles[i][0];
		int n = (int)rectangles[i].size();
		polylines(image, &p, &n, 1, true, Scalar(0, 255, 0), 3, LINE_AA);
	}

	imshow(wndname, image);
}


int main(int argc, char** argv)
{
	const char* usage = "usage: IdentifyRectangles [options] <image>...\n\n-w, --windows\tDisplays windows.";
	if (argc < 2) // Checks there is at least 1 arg
	{
		cout << usage << endl;
		return 0;
	}

	bool displayWindows;
	if (strcmp(argv[1], "-w") == 0 || strcmp(argv[1], "--windows") == 0) // Checks if windows should be displayed
	{
		displayWindows = true;
		if (argc < 3) // Checks there is at least one other arg
		{
			cout << usage << endl;
			return 0;
		}
	}
	else
		displayWindows = false;

	static const char* names[10];

	int offset = 1; // Gets number of non image parameters
	if (displayWindows)
		offset++;

	for (int i = 0; i < argc - offset; i++) // Gets array of images (max 10)
	{
		names[i] = argv[i + offset];
	}

	if (displayWindows)
		namedWindow(wndname , 1);

	vector<vector<Point> > rectangles;

	for (int i = 0; names[i] != 0; i++)
	{
		cout << endl; // Creates new line for this squares output

		Mat image = imread(names[i], 1);
		if (image.empty())
		{
			cout << "Couldn't load " << names[i];
			continue;
		}

		findRectangles(image, rectangles);
		if (displayWindows)
			drawSquares(image, rectangles);

		// Does a json output of all the rectangles identified
		cout << "[";
		for (int a = 0; a < rectangles.size(); a++)
		{
			cout << "[";
			for (int b = 0; b < rectangles[a].size(); b++)
			{
				cout << "{\"x\":" << rectangles[a][b].x << ",\"y\":" << rectangles[a][b].y << "}";
				if (b < rectangles[a].size() - 1)
					cout << ",";
			}
			cout << "]";
			if (a < rectangles.size() - 1)
				cout << ",";
		}

		cout << "]";
	}
	cout << endl;
	return 0;
}
