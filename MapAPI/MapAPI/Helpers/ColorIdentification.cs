using System;
using System.Drawing;
using System.Linq;

namespace MapAPI.Helpers
{
    public static class ColorIdentification
    {
        /// <summary>
        ///     Checks to see if each Color's saturation and brightness values in this Array of Colors is outside of the threshold
        ///     of another set of the median saturation and brightness in each section. Also preforms a white balance based on the
        ///     medians.
        /// </summary>
        /// <returns>
        ///     A two-dimensional Array of Booleans where true means the corresponding pixel in this Bitmap was outside the
        ///     threshold.
        /// </returns>
        public static bool[][] CreateThresholdArrayAndBalance(this Bitmap bitmap)
        {
            const int size = 200;

            bool[][] output = new bool[bitmap.Height][];
            for (int i = 0; i < output.Length; i++)
                output[i] = new bool[bitmap.Width];

            //Gets number of chunks equal to number size, min 1
            int yChunkCount = Math.Max(bitmap.Height / size, 1);
            int xChunkCount = Math.Max(bitmap.Width / size, 1);

            for (int yChunk = 0; yChunk < yChunkCount; yChunk++)
            for (int xChunk = 0; xChunk < xChunkCount; xChunk++)
            {
                int width;
                //Remainder of image is lass segment
                if (xChunk == xChunkCount - 1)
                    width = bitmap.Width - size * xChunk;
                //Do default size
                else
                    width = size;

                //Same again with height
                int height;
                if (yChunk == yChunkCount - 1)
                    height = bitmap.Height - size * yChunk;
                else
                    height = size;

                Bitmap segment = bitmap.Clone(new Rectangle(size * xChunk, size * yChunk, width, height),
                    bitmap.PixelFormat);


                (float hue, float saturation, float brightness) medians = segment.MedianHsb();
                bool[] segmentArray = segment.HbsThresholdCheck(medians.saturation, medians.brightness);

                //Gets the factor required to balance the image
                Color averageColor = General.FromHsb(medians.hue, medians.saturation, medians.brightness);
                (float red, float green, float blue) whiteBalanceModifier =
                    (255F / averageColor.R, 255F / averageColor.G, 255F / averageColor.B);


                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    output[yChunk * 200 + y][xChunk * 200 + x] = segmentArray[y * width + x];

                    // Balanced color
                    Color balancedColor = bitmap.GetPixel(xChunk * 200 + x, yChunk * 200 + y);
                    balancedColor = Color.FromArgb(255,
                        Math.Min((int) (balancedColor.R * whiteBalanceModifier.red), 255),
                        Math.Min((int) (balancedColor.G * whiteBalanceModifier.green), 255),
                        Math.Min((int) (balancedColor.B * whiteBalanceModifier.blue), 255));
                    //Changes color
                    bitmap.SetPixel(xChunk * 200 + x, yChunk * 200 + y, balancedColor);
                }
            }

            return output;
        }

        /// <summary>
        ///     Gets the median hue, saturation and brightness of all the pixels in this Bitmap.
        /// </summary>
        /// <returns>A Tuple consisting of the median hue, saturation and brightness for all the pixels in this bitmap.</returns>
        public static (float hue, float saturation, float brightness) MedianHsb(this Bitmap bitmap)
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
        ///     Checks to see if each Color's saturation and brightness values in this Array of Colors is outside of the threshold
        ///     of another set of saturation and brightness.
        /// </summary>
        /// <param name="saturation">The saturation to center the threshold around.</param>
        /// <param name="brightness">The brightness to center the threshold around.</param>
        /// <exception cref="ArgumentException">One or more of saturation and brightness are outside of the possible range.</exception>
        /// <returns>
        ///     An Array of Booleans where each element is true if the corresponding element in this Array of Colors is
        ///     outside of the threshold saturation and brightness values.
        /// </returns>
        public static bool[] HbsThresholdCheck(this Bitmap bitmap, float saturation, float brightness)
        {
            if (saturation < 0 || saturation > 1)
                throw new ArgumentException("saturation must be between 0 and 1.", nameof(saturation));
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("brightness must be between 0 and 1.", nameof(brightness));

            Color[] pixels = bitmap.ToColorArray();
            return pixels.Select(pixel => pixel.HbsThresholdCheck(saturation, brightness)).ToArray();
        }

        /// <summary>
        ///     Checks to see if this Color's saturation and brightness values is outside of the threshold of another set of
        ///     saturation and brightness values.
        /// </summary>
        /// <param name="saturation">The saturation to center the threshold around.</param>
        /// <param name="brightness">The brightness to center the threshold around.</param>
        /// <exception cref="ArgumentException">One or more of saturation and brightness are outside of the possible range.</exception>
        /// <returns>True if at least one value is outside of the threshold saturation and brightness values.</returns>
        private static bool HbsThresholdCheck(this Color pixel, float saturation, float brightness)
        {
            if (saturation < 0 || saturation > 1)
                throw new ArgumentException("saturation must be between 0 and 1.", nameof(saturation));
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("brightness must be between 0 and 1.", nameof(brightness));

            //Threshold differences
            const float saturationThreshold = 0.1F;
            const float brightnessThreshold = 0.1F;

            //Current pixels values
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

            return false;
        }
    }
}