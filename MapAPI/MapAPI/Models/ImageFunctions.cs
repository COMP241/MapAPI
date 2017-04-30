using System;
using System.Drawing;
using System.Linq;

namespace MapAPI.Models
{
    public static class ImageFunctions
    {
        /// <summary>
        ///     Trys to identifies the paper from a set of rectangles.
        /// </summary>
        /// <param name="image">Image to identify paper in.</param>
        /// <param name="rectangles">Rectangles in the image.</param>
        /// <returns>The best guess at the rectangle that defines the paper, null if no match was found.</returns>
        public static Point[] IdentifyPaperCorners(Bitmap image, Point[][] rectangles)
        {
            //Orders rectangles from largest to smallest
            rectangles = rectangles.OrderBy(ApproximateAreaOfRectangle).Reverse().ToArray();
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

            //Returns the appoximate area of the rectagle
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
                return pixel.GetSaturation() < 0.3 && pixel.GetBrightness() > 0.5;
            }
        }
    }
}