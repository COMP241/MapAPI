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
    }
}