using System;
using System.Drawing;

namespace MapAPI.Helpers
{
    public static class General
    {
        /// <summary>
        ///     Gets the value at the specified position in the one-dimensional Array, wrapping around if the index is out of
        ///     bounds. The index is specified as a 32-bit integer.
        /// </summary>
        /// <param name="index">A 32-bit integer that represents the position of the Array element to get.</param>
        /// <exception cref="IndexOutOfRangeException">The current Array has a length of 0.</exception>
        /// <returns>The value at the specified position in the one-dimensional Array.</returns>
        public static T GetValueOverflow<T>(this T[] array, int index)
        {
            if (array.Length == 0)
                throw new IndexOutOfRangeException("Index was outside the bounds of the array.");

            index = index % array.Length;
            if (index < 0)
                index = array.Length + index;

            return array[index];
        }

        /// <summary>
        ///     Gets the correct hue-saturation-brightness (HSB) brightness value for this Color structure.
        /// </summary>
        /// <returns>
        ///     The brightness of this Color. The brightness ranges from 0.0 through 1.0, where 0.0 represents black and 1.0
        ///     represents white.
        /// </returns>
        public static float Brightness(this Color color)
        {
            return (float) (Math.Max(color.R, Math.Max(color.G, color.B)) / 255d);
        }

        /// <summary>
        ///     Gets the correct hue-saturation-brightness (HSB) saturation value for this Color structure.
        /// </summary>
        /// <returns>
        ///     The saturation of this Color. The saturation ranges from 0.0 through 1.0, where 0.0 is grayscale and 1.0 is
        ///     the most saturated.
        /// </returns>
        public static float Saturation(this Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            return (float) (max == 0 ? 0 : 1d - 1d * min / max);
        }

        /// <summary>
        ///     Creates an one-dimensional Array of Colors containing all the pixels in this bitmap going long the rows then down.
        /// </summary>
        /// <returns>A one-dimensional Array od Colors containing all the pixels in this bitmap.</returns>
        public static Color[] ToColorArray(this Bitmap bitmap)
        {
            Color[] pixels = new Color[bitmap.Height * bitmap.Width];
            for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                pixels[y * bitmap.Width + x] = bitmap.GetPixel(x, y);

            return pixels;
        }

        /// <summary>
        ///     Creates a Bitmap from this one-dimensional Array of Colors with the specified dimensions.
        /// </summary>
        /// <param name="width">The width of the new Bitmap.</param>
        /// <param name="height">The height of the new Bitmap.</param>
        /// <exception cref="ArgumentException">
        ///     The number of elements in this Array of Colors does not match the number of
        ///     elements needed for this height and width.
        /// </exception>
        /// <returns>A Bitmap with colors matching this Array od Colors.</returns>
        public static Bitmap ToBitmap(this Color[] pixels, int width, int height)
        {
            if (width * height != pixels.Length)
                throw new ArgumentException(
                    "Number of elements in this Array of Colors does not match the number of elements needed for this height and width.");

            Bitmap bitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, pixels[y * width + x]);

            return bitmap;
        }

        /// <summary>
        ///     Creates a Color from alpha, hue, saturation and brightness.
        ///     Based on the code by Chris Hulbert, http://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
        /// </summary>
        /// <param name="hue">The hue value.</param>
        /// <param name="saturation">The saturation value.</param>
        /// <param name="brightness">The brightness value.</param>
        /// <returns>A Color with the given values.</returns>
        public static Color FromHsb(float hue, float saturation, float brightness
        )
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            while (hue < 0)
                hue += 360;
            while (hue >= 360)
                hue -= 360;
            double red, green, blue;
            if (brightness <= 0)
            {
                red = green = blue = 0;
            }
            else if (saturation <= 0)
            {
                red = green = blue = brightness;
            }
            else
            {
                double hf = hue / 60.0;
                int i = (int) Math.Floor(hf);
                double f = hf - i;
                double pv = brightness * (1 - saturation);
                double qv = brightness * (1 - saturation * f);
                double tv = brightness * (1 - saturation * (1 - f));
                switch (i)
                {
                    // Red is the dominant color

                    case 0:
                        red = brightness;
                        green = tv;
                        blue = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        red = qv;
                        green = brightness;
                        blue = pv;
                        break;
                    case 2:
                        red = pv;
                        green = brightness;
                        blue = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        red = pv;
                        green = qv;
                        blue = brightness;
                        break;
                    case 4:
                        red = tv;
                        green = pv;
                        blue = brightness;
                        break;

                    // Red is the dominant color

                    case 5:
                        red = brightness;
                        green = pv;
                        blue = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        red = brightness;
                        green = tv;
                        blue = pv;
                        break;
                    case -1:
                        red = brightness;
                        green = pv;
                        blue = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        red = green = blue = brightness; // Just pretend its black/white
                        break;
                }
            }

            return Color.FromArgb(255, Clamp((int) (red * 255.0)), Clamp((int) (green * 255.0)),
                Clamp((int) (blue * 255.0)));

            int Clamp(int i)
            {
                if (i < 0) return 0;
                if (i > 255) return 255;
                return i;
            }
        }
    }
}