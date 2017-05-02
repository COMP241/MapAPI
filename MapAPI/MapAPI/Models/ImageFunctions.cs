using System;
using System.Drawing;
using System.Linq;

namespace MapAPI.Models
{
    public static class ImageFunctions
    {
        /// <summary>
        ///     Tries to identifies the paper from a set of rectangles.
        /// </summary>
        /// <param name="image">Image to identify paper in.</param>
        /// <param name="rectangles">Rectangles in the image.</param>
        /// <returns>The best guess at the rectangle that defines the paper, null if no match was found.</returns>
        public static Point[] IdentifyPaperCorners(Bitmap image, Point[][] rectangles)
        {
            //Orders rectangles from largest to smallest
            rectangles = rectangles.OrderByDescending(ApproximateAreaOfRectangle).ToArray();
            foreach (Point[] rectangle in rectangles)
            {
                //Checks if all 4 points of the rectangle lie on a white pixel
                bool allPointsWhite = rectangle.Aggregate(true,
                    (current, point) => current && IsWhite(image.GetPixel(point.X, point.Y)));
                if (allPointsWhite)
                    return rectangle;
            }

            //Returns null if no rectangles worked
            return null;

            //Returns the approximate area of the rectangle
            int ApproximateAreaOfRectangle(Point[] rectangle)
            {
                //Length of one side with pythagoras
                int length1 = (int) Math.Sqrt(
                    Math.Pow(rectangle[0].X - rectangle[1].X, 2) +
                    Math.Pow(rectangle[0].Y - rectangle[1].Y, 2)
                );

                //Length of another side with pythagoras
                int length2 = (int) Math.Sqrt(
                    Math.Pow(rectangle[0].X - rectangle[3].X, 2) +
                    Math.Pow(rectangle[0].Y - rectangle[3].Y, 2)
                );

                return length1 * length2;
            }

            //Checks if pixel is "white" i.e. Saturation < 30 and Brightness > 50
            bool IsWhite(Color pixel)
            {
                return GetSaturation(pixel) < 0.3 && GetBrightness(pixel) > 0.5;

                double GetBrightness(Color color)
                {
                    return Math.Max(color.R, Math.Max(color.G, color.B)) / 255d;
                }

                double GetSaturation(Color color)
                {
                    int max = Math.Max(color.R, Math.Max(color.G, color.B));
                    int min = Math.Min(color.R, Math.Min(color.G, color.B));

                    return max == 0 ? 0 : 1d - 1d * min / max;
                }
            }
        }

        /// <summary>
        ///     Skews the image to create a new image where the four input points are the new corners.
        ///     Based on the code by mauf, http://stackoverflow.com/a/5730469
        ///     under the CC BY-SA license, https://creativecommons.org/licenses/by-sa/4.0/.
        /// </summary>
        /// <param name="image">Image to transform.</param>
        /// <param name="points">The four points (in any clockwise or anticlockwise order) to become the four corners.</param>
        /// <param name="width">Width of new image.</param>
        /// <param name="height">Height of new image.</param>
        /// <returns>Transformed image.</returns>
        public static Bitmap PerspectiveTransformImage(Bitmap image, Point[] points, int width, int height)
        {
            points.OrderClockwise();

            Point topLeft = points[0];
            Point topRight = points[1];
            Point bottomLeft = points[3];
            Point bottomRight = points[2];

            Bitmap imageOut = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                //Relative position
                double relativeX = (double) x / width;
                double relativeY = (double) y / height;

                //Get top and bottom position
                double topX = topLeft.X + relativeX * (topRight.X - topLeft.X);
                double topY = topLeft.Y + relativeX * (topRight.Y - topLeft.Y);
                double bottomX = bottomLeft.X + relativeX * (bottomRight.X - bottomLeft.X);
                double bottomY = bottomLeft.Y + relativeX * (bottomRight.Y - bottomLeft.Y);

                //Select center between top and bottom point
                double centerX = topX + relativeY * (bottomX - topX);
                double centerY = topY + relativeY * (bottomY - topY);

                //Store result
                Color c = PolyColor(centerX, centerY);
                imageOut.SetPixel(x, y, c);
            }

            return imageOut;

            //Gets an average color based on the point
            Color PolyColor(double x, double y)
            {
                //Get fractions
                double xFraction = x - (int) x;
                double yFraction = y - (int) y;

                //4 colors - we're flipping sides so we can use the distance instead of inverting it later
                Color colorTopLeft = image.GetPixel((int) x + 1, (int) y + 1);
                Color colorTopRight = image.GetPixel((int) x + 0, (int) y + 1);
                Color colorBottomLeft = image.GetPixel((int) x + 1, (int) y + 0);
                Color colorBottomRight = image.GetPixel((int) x + 0, (int) y + 0);

                //4 distances
                double distanceTopLeft = Math.Sqrt(xFraction * xFraction + yFraction * yFraction);
                double distanceTopRight = Math.Sqrt((1 - xFraction) * (1 - xFraction) + yFraction * yFraction);
                double distanceBottomLeft = Math.Sqrt(xFraction * xFraction + (1 - yFraction) * (1 - yFraction));
                double distanceBottomRight =
                    Math.Sqrt((1 - xFraction) * (1 - xFraction) + (1 - yFraction) * (1 - yFraction));

                //4 parts
                double factor = 1.0 / (distanceTopLeft + distanceTopRight + distanceBottomLeft + distanceBottomRight);
                distanceTopLeft *= factor;
                distanceTopRight *= factor;
                distanceBottomLeft *= factor;
                distanceBottomRight *= factor;

                //Accumulate parts
                double r = distanceTopLeft * colorTopLeft.R + distanceTopRight * colorTopRight.R +
                           distanceBottomLeft * colorBottomLeft.R + distanceBottomRight * colorBottomRight.R;
                double g = distanceTopLeft * colorTopLeft.G + distanceTopRight * colorTopRight.G +
                           distanceBottomLeft * colorBottomLeft.G + distanceBottomRight * colorBottomRight.G;
                double b = distanceTopLeft * colorTopLeft.B + distanceTopRight * colorTopRight.B +
                           distanceBottomLeft * colorBottomLeft.B + distanceBottomRight * colorBottomRight.B;

                Color c = Color.FromArgb((int) (r + 0.5), (int) (g + 0.5), (int) (b + 0.5));

                return c;
            }
        }

        /// <summary>
        ///     Orders the points in a clockwise direction from top left.
        /// </summary>
        /// <param name="corners">Array of 4 points to order, must already be in clockwise or anticlockwise order.</param>
        public static void OrderClockwise(this Point[] corners)
        {
            //Checks that there are 4 points
            if (corners.Length != 4)
                throw new ArgumentException("The number of corners is not 4", nameof(corners));

            //Gets left most corner (if multiple corners are the same amount to the left, gets the higher one)
            int leftMostXValue = corners.Min(corner => corner.X);
            Point[] leftMostCorners = corners.Where(corner => corner.X == leftMostXValue).ToArray();
            Point leftMostCorner = leftMostCorners.Length == 1
                ? leftMostCorners[0]
                : leftMostCorners.OrderBy(corner => corner.Y).First();

            //Gets top most corner (if multiple corners are the same height, gets the left most one)
            int topMostYValue = corners.Min(corner => corner.Y);
            Point[] topMostCorners = corners.Where(corner => corner.Y == topMostYValue).ToArray();
            Point topMostCorner = topMostCorners.Length == 1
                ? topMostCorners[0]
                : topMostCorners.OrderBy(corner => corner.X).First();

            Point topLeftCorner, topRightCorner;
            int topLeftCornerIndex, topRightCornerIndex;

            //The top left corner must be 90 degrees with a horizontal and vertical line
            if (leftMostCorner.Equals(topMostCorner))
            {
                topLeftCorner = leftMostCorner;
                topRightCorner = topMostCorners.First(corner => corner.X != topLeftCorner.X);
                topLeftCornerIndex = Array.IndexOf(corners, topLeftCorner);
                topRightCornerIndex = Array.IndexOf(corners, topRightCorner);
            }
            else
            {
                //Finds angle between left most and top most corner
                double angle = Math.Atan((double) (topMostCorner.Y - leftMostCorner.Y) /
                                         (topMostCorner.X - leftMostCorner.X));

                //Based on angle decides which is the top right corner, this is approximate and 
                //requires the image to already be fairly upright and not on too much of an angle
                if (Math.Abs(leftMostCorner.Y - topMostCorner.Y) < Math.Abs(leftMostCorner.X - topMostCorner.X))
                {
                    topLeftCorner = leftMostCorner;
                    topRightCorner = topMostCorner;
                    topLeftCornerIndex = Array.IndexOf(corners, topLeftCorner);
                    topRightCornerIndex = Array.IndexOf(corners, topRightCorner);
                }
                else
                {
                    topLeftCorner = topMostCorner;
                    topLeftCornerIndex = Array.IndexOf(corners, topLeftCorner);

                    int bottomLeftIndex = Array.IndexOf(corners, leftMostCorner);
                    //Top right corner is opposite bottom left
                    topRightCornerIndex = bottomLeftIndex + 2;
                    //Subtracts 4 if the index is above 3
                    topRightCornerIndex -= topRightCornerIndex > 3 ? 4 : 0;
                }
            }

            //Order the points in here before moving it to original array
            Point[] tempOrder = new Point[4];
            int currentPos = topLeftCornerIndex;
            for (int i = 0; i < 4; i++)
            {
                //Subtracts 4 if the index is above 3
                currentPos -= currentPos > 3 ? 4 : 0;
                //Adds 4 if the index is below 0
                currentPos += currentPos < 0 ? 4 : 0;

                tempOrder[i] = corners[currentPos];
                //This either increments or decrements the current position based on the order of the corners originally
                currentPos += topRightCornerIndex - topLeftCornerIndex;
            }

            //Changes original array
            Array.Copy(tempOrder, corners, 4);
        }
    }
}