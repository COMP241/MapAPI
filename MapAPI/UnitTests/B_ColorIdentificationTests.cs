using System;
using System.Drawing;
using System.Drawing.Imaging;
using MapAPI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class B_ColorIdentificationTests
    {
        private readonly string[] _images =
        {
            "img2",
            //"img3",
            //"img4",
            "img5",
            "img6",
            "img7",
            //"img8",
            //"img2L",
            "img3L",
            "img4L",
            "img5L",
            "img6L",
            "img7L"
            //"img8L"
        };

        [TestMethod]
        public void MedianHBSTest()
        {
            //Create bitmap with predefined pixels
            Bitmap bitmap = new[]
            {
                Color.FromArgb(70, 190, 23), Color.FromArgb(167, 70, 81), Color.FromArgb(233, 18, 172),
                Color.FromArgb(193, 113, 141), Color.FromArgb(181, 73, 67), Color.FromArgb(238, 192, 6),
                Color.FromArgb(163, 239, 221), Color.FromArgb(227, 105, 135), Color.FromArgb(132, 45, 89),
                Color.FromArgb(101, 150, 129), Color.FromArgb(141, 215, 82), Color.FromArgb(188, 9, 62),
                Color.FromArgb(185, 124, 99), Color.FromArgb(86, 20, 42), Color.FromArgb(230, 208, 241),
                Color.FromArgb(79, 8, 39), Color.FromArgb(78, 61, 185), Color.FromArgb(80, 72, 13),
                Color.FromArgb(62, 60, 54), Color.FromArgb(241, 163, 82), Color.FromArgb(79, 8, 187),
                Color.FromArgb(1, 254, 229), Color.FromArgb(102, 104, 147), Color.FromArgb(69, 105, 254),
                Color.FromArgb(110, 91, 194), Color.FromArgb(227, 37, 49), Color.FromArgb(252, 155, 177),
                Color.FromArgb(182, 165, 253), Color.FromArgb(101, 86, 19)
            }.ToBitmap(29, 1);

            //Check the values match precalculated ones
            (float saturation, float brightness) medians = bitmap.MedianHSB();
            Assert.AreEqual(Math.Round(medians.saturation, 4), Math.Round(0.659090909, 4));
            Assert.AreEqual(Math.Round(medians.brightness, 4), Math.Round(0.745098039, 4));
        }

        [TestMethod]
        public void ThresholdHSBTest()
        {
            foreach (string image in _images)
            {
                //Gets image
                Bitmap bitmap = new Bitmap($"Images/Out/{image} - transform.png");

                //Checks each pixels threshold
                bool[] threshold = bitmap.CreateThresholdArray();

                //Create bitmap where black is true and white is false
                Bitmap thresholdBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                for (int y = 0; y < thresholdBitmap.Height; y++)
                for (int x = 0; x < thresholdBitmap.Width; x++)
                    thresholdBitmap.SetPixel(x, y,
                        threshold[y * thresholdBitmap.Width + x] ? Color.Black : Color.White);

                //Saves image
                thresholdBitmap.Save($"Images/Out/{image} - threshold.png", ImageFormat.Png);
            }
        }
    }
}