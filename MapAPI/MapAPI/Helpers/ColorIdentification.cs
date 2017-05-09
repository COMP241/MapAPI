using System;
using System.Drawing;
using System.Linq;

namespace MapAPI.Helpers
{
    public static class ColorIdentification
    {
        /// <summary>
        ///     Gets the median hue, saturation and brightness of all the pixels in this Bitmap.
        /// </summary>
        /// <returns>A Tuple consisting of the median hue, saturation and brightness for all the pixels in this bitmap.</returns>
        public static (float hue, float saturation, float brightness) MedianHSB(this Bitmap bitmap)
        {
            //Put all the pixels in an array
            Color[] pixels = new Color[bitmap.Height * bitmap.Width];
            for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                pixels[y * bitmap.Width + x] = bitmap.GetPixel(x, y);

            //Get and sort hue
            float[] hueValues = pixels.Select(pixel => pixel.GetHue()).ToArray();
            Array.Sort(hueValues);

            //Get and sort saturation
            float[] saturationValues = pixels.Select(pixel => pixel.Saturation()).ToArray();
            Array.Sort(saturationValues);

            //Get and sort brightness
            float[] brightnessValues = pixels.Select(pixel => pixel.Brightness()).ToArray();
            Array.Sort(brightnessValues);

            int middle = pixels.Length / 2;

            //Return medians
            return (hueValues[middle], saturationValues[middle], brightnessValues[middle]);
        }
    }
}