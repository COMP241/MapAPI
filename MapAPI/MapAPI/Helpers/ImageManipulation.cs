using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MapAPI.Models;
using Newtonsoft.Json;

namespace MapAPI.Helpers
{
    public static class ImageManipulation
    {
        private static readonly ConfigFile.Config Config;

        static ImageManipulation()
        {
            Config = JsonConvert.DeserializeObject<ConfigFile.Config>(File.ReadAllText("config.json"));
        }

        /// <summary>
        ///     Tries to identifies the paper from a set of rectangles.
        /// </summary>
        /// <param name="rectangles">An Array of Arrays with 4 Point elements making up the possible locations of the paper.</param>
        /// <exception cref="ArgumentException">rectangles is either empty or an Array of Points is invalid.</exception>
        /// <returns>The Array of 4 Points that is most likely to be the location of the paper.</returns>
        public static Point[] IdentifyPaperCorners(this Bitmap image, Point[][] rectangles)
        {
            if (rectangles.Length == 0)
                throw new ArgumentException("The Array rectangles is empty.", nameof(rectangles));
            if (rectangles.All(rectangle => rectangle.Length != 4))
                throw new ArgumentException(
                    "At least one Array of Points element in rectangles does not exactly contain 4 points.",
                    nameof(rectangles));
            if (rectangles.All(rectangle => rectangle.All(
                point => point.X >= 0 && point.X < image.Height && point.Y >= 0 && point.Y < image.Width)))
                throw new ArgumentException(
                    "At least one Point element in rectangles is not within the bounds of the current image.",
                    nameof(rectangles));

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

            //Returns the largest rectangle if none worked
            return rectangles[0];

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
                return pixel.Saturation() < Config.WhiteDefinition.Saturation &&
                       pixel.Brightness() > Config.WhiteDefinition.Brightness;
            }
        }

        /// <summary>
        ///     Skews the Bitmap to create a new Bitmap where the 4 Points in a Array of Points are the new corners.
        ///     Based on the code by mauf, http://stackoverflow.com/a/5730469
        ///     under the CC BY-SA license, https://creativecommons.org/licenses/by-sa/4.0/.
        /// </summary>
        /// <param name="points">
        ///     A Array of Points with 4 values defining the 4 new corners. All 4 Points must be in a clockwise or
        ///     anticlockwise direction.
        /// </param>
        /// <param name="width">A 32-bit integer that represents the width of the new Bitmap.</param>
        /// <param name="height">A 32-bit integer that represents the height of the new Bitmap.</param>
        /// <exception cref="ArgumentException">The Array of Points is invalid.</exception>
        /// <returns>The skewed image based on this Bitmap</returns>
        public static Bitmap PerspectiveTransformImage(this Bitmap image, Point[] points, int width, int height)
        {
            if (points.Length != 4)
                throw new ArgumentException("The Array of Points points does not contain exactly 4 Points.",
                    nameof(points));
            if (points.All(point => point.X >= 0 && point.X < image.Height && point.Y >= 0 && point.Y < image.Width))
                throw new ArgumentException(
                    "At least one Point element in the Array of Points points is not within the bounds of the current image.",
                    nameof(points));

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
        ///     Orders this Array of Points in a clockwise order from the top left.
        /// </summary>
        /// <exception cref="ArgumentException">points does not contain exactly 4 points.</exception>
        public static void OrderClockwise(this Point[] points)
        {
            //Checks that there are 4 points
            if (points.Length != 4)
                throw new ArgumentException("The number of Points in corners is not exactly 4", nameof(points));

            //Finds the top left corner based on the corner where the sum of its X and Y is the lowest
            Point topLeftCorner =
                points.First(corner => corner.X + corner.Y == points.Min(corner2 => corner2.X + corner2.Y));
            int topLeftCornerIndex = Array.IndexOf(points, topLeftCorner);

            //The top right corner must be the corner that has the greater angle relative to the top left corner
            Point[] adjacentPoints =
                {points.GetValueOverflow(topLeftCornerIndex - 1), points.GetValueOverflow(topLeftCornerIndex + 1)};
            double[] relativeAngles = adjacentPoints.Select(corner => RelativeAngle(topLeftCorner, corner)).ToArray();
            int greaterRelativeAngleIndex = Array.IndexOf(relativeAngles, relativeAngles.Max());
            Point topRightCorner = adjacentPoints[greaterRelativeAngleIndex];
            int topRightCornerIndex = Array.IndexOf(points, topRightCorner);

            //Order the points in here before moving it to original array
            Point[] tempOrder = new Point[4];
            int currentPos = topLeftCornerIndex;
            for (int i = 0; i < 4; i++)
            {
                //Copies value from corners into the correct position
                tempOrder[i] = points.GetValueOverflow(currentPos);
                //This either increments or decrements the current position based on the order of the corners originally
                currentPos += topRightCornerIndex - topLeftCornerIndex;
            }

            //Changes original array
            Array.Copy(tempOrder, points, 4);

            //Returns a double ranging from 2 to -2 where 2 is an angle of pi and -2 is an angle of -pi
            double RelativeAngle(Point center, Point point)
            {
                int x = point.X - center.X;
                int y = -(point.Y - center.Y);
                double hypotenuse = Math.Sqrt(x * x + y * y);
                //Gets sin(theta) of angle
                double relativeAngle = y / hypotenuse;

                //If x >=0 it is already right
                if (x >= 0) return relativeAngle;

                //Otherwise adjusts value
                if (y >= 0)
                    relativeAngle = 2 - relativeAngle;
                else
                    relativeAngle = -2 - relativeAngle;

                return relativeAngle;
            }
        }
    }
}