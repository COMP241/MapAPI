using System;
using System.Drawing;

namespace MapAPI.Helpers
{
    public static class Debug
    {
        public static Bitmap DrawPoints(Bitmap bitmap, Point[] rectangle)
        {
            Random rnd = new Random();

            Bitmap output = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
            foreach (Point point in rectangle)
                for (int xOffset = -2; xOffset <= 2; xOffset++)
                for (int yOffset = -2; yOffset <= 2; yOffset++)
                {
                    int x = point.X + xOffset;
                    int y = point.Y + yOffset;

                    if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height) continue;

                    output.SetPixel(point.X + xOffset, point.Y + yOffset, randomColor);
                }

            return output;
        }

        public static Bitmap DrawPoints(Bitmap bitmap, Point[][] rectangles)
        {
            Random rnd = new Random();

            Bitmap output = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            foreach (Point[] rectangle in rectangles)
            {
                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                foreach (Point point in rectangle)
                    for (int xOffset = -2; xOffset <= 2; xOffset++)
                    for (int yOffset = -2; yOffset <= 2; yOffset++)
                    {
                        int x = point.X + xOffset;
                        int y = point.Y + yOffset;

                        if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height) continue;

                        output.SetPixel(point.X + xOffset, point.Y + yOffset, randomColor);
                    }
            }

            return output;
        }

        public static Bitmap BitmapFromBool(bool[][] threshold)
        {
            Bitmap output = new Bitmap(threshold[0].Length, threshold.Length);

            for (int y = 0; y < output.Height; y++)
            for (int x = 0; x < output.Width; x++)
                output.SetPixel(x, y,
                    threshold[y][x] ? Color.Black : Color.White);

            return output;
        }
    }
}