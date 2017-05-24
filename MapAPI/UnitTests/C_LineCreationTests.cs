using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using MapAPI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class C_LineCreationTests
    {
        [TestMethod]
        public void LineCreationTest()
        {
            Bitmap bitmap = new Bitmap("Images/thin.png");
            Color[] pixels = bitmap.ToColorArray();
            bool[][] boolArray = new bool[bitmap.Height][];
            for (int y = 0; y < bitmap.Height; y++)
            {
                boolArray[y] = new bool[bitmap.Width];
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (pixels[y * bitmap.Width + x].GetBrightness() < 0.1)
                        boolArray[y][x] = true;
                }
            }

            var output = boolArray.CreateLines();

            Random rnd = new Random(123456);
            for (int index = 0; index < output.Count; index++)
            {
                if (index == 17)
                {
                    index++;
                    index--;
                }
                List<PointF> pointFs = output[index];
                Bitmap bitout = new Bitmap(bitmap.Width, bitmap.Height);

                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                bitout.SetPixel(0, 0, Color.Black);
                bitout.SetPixel(1413, 999, Color.Black);
                foreach (PointF pointF in pointFs)
                {
                    bitout.SetPixel((int) pointF.X, (int) pointF.Y, randomColor);
                }
                bitout.Save($"Images/Out/thin - {index}.png", ImageFormat.Png);
            }
        }
    }
}
