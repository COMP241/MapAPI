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
            Color[] pixels = bitmap.ToColorArray();

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

        /// <summary>
        ///     Checks to see if each Color's HSB values in this Array of Colors is outside of the threshold of another set of HSB
        ///     values.
        /// </summary>
        /// <param name="hue">The hue to center the threshold around.</param>
        /// <param name="saturation">The saturation to center the threshold around.</param>
        /// <param name="brightness">The brightness to center the threshold around.</param>
        /// <exception cref="ArgumentException">One or more of hue, saturation and brightness are outside of the possible range.</exception>
        /// <returns>
        ///     An Array of Booleans where each element is true if the corresponding element in this Array of Colors is
        ///     outside of the threshold HSB values.
        /// </returns>
        public static bool[] HBSThresholdCheck(this Bitmap bitmap, float hue, float saturation, float brightness)
        {
            if (hue < 0 || hue > 360)
                throw new ArgumentException("hue must be between 0 and 360.", nameof(hue));
            if (saturation < 0 || saturation > 1)
                throw new ArgumentException("saturation must be between 0 and 1.", nameof(saturation));
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("brightness must be between 0 and 1.", nameof(brightness));

            Color[] pixels = bitmap.ToColorArray();
            return pixels.Select(pixel => pixel.HBSThresholdCheck(hue, saturation, brightness)).ToArray();
        }

        /// <summary>
        ///     Checks to see if this Color's HSB values is outside of the threshold of another set of HSB values.
        /// </summary>
        /// <param name="hue">The hue to center the threshold around.</param>
        /// <param name="saturation">The saturation to center the threshold around.</param>
        /// <param name="brightness">The brightness to center the threshold around.</param>
        /// <exception cref="ArgumentException">One or more of hue, saturation and brightness are outside of the possible range.</exception>
        /// <returns>True if at least one value is outside of the threshold HSB values.</returns>
        private static bool HBSThresholdCheck(this Color pixel, float hue, float saturation, float brightness)
        {
            if (hue < 0 || hue > 360)
                throw new ArgumentException("hue must be between 0 and 360.", nameof(hue));
            if (saturation < 0 || saturation > 1)
                throw new ArgumentException("saturation must be between 0 and 1.", nameof(saturation));
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("brightness must be between 0 and 1.", nameof(brightness));

            //Threshold differences
            float hueThreshold = 10F;
            float saturationThreshold = 0.1F;
            float brightnessThreshold = 0.1F;

            //Current pixels values
            float pixelHue = pixel.GetHue();
            float pixelSaturation = pixel.Saturation();
            float pixelBrightness = pixel.Brightness();

            //Checks if brightness is different
            if (pixelBrightness < brightness - brightnessThreshold ||
                pixelBrightness > brightness + brightnessThreshold)
                return true;

            //Checks if saturation is different
            if (pixelSaturation < saturation - saturationThreshold ||
                pixelSaturation > saturation + saturationThreshold)
                return true;

            /*
            //If saturation is low, hue can't be checked safely
            if (pixelSaturation < 0.08)
                return false;

            //Gets hue min and max (because hue is a circle)
            float hueMin = hue - hueThreshold;
            hueMin += hueMin < 0 ? 360 : 0;
            float hueMax = hue + hueThreshold;
            hueMax -= hueMax > 360 ? 360 : 0;

            //If max hue is greater than min hue does a normal check
            if (hueMax > hueMin)
            {
                if (pixelHue < hueMin || pixelHue > hueMax)
                    return true;
            }
            //If max hue is less than min hue does different check
            else
            {
                if (pixelHue < hueMin && pixelHue > hueMax)
                    return true;
            }
            */

            return false;
        }
    }
}