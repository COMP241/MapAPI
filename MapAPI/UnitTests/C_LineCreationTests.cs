using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MapAPI.Helpers;
using MapAPI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace UnitTests
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class C_LineCreationTests
    {
        [TestMethod]
        public void LineCreationSingleTest()
        {
            Bitmap bitmap = new Bitmap("Images/Thin.png");
            Color[] pixels = bitmap.ToColorArray();

            //Create bool array based on black and white
            bool[][] boolArray = new bool[bitmap.Height][];
            for (int y = 0; y < bitmap.Height; y++)
            {
                boolArray[y] = new bool[bitmap.Width];
                for (int x = 0; x < bitmap.Width; x++)
                    if (pixels[y * bitmap.Width + x].GetBrightness() < 0.1)
                        boolArray[y][x] = true;
            }

            Random rnd = new Random(123456);

            //Creates part of lines
            List<List<PointF>> lineParts = boolArray.CreateLineParts();
            for (int index = 0; index < lineParts.Count; index++)
            {
                List<PointF> pointFs = lineParts[index];
                Bitmap bitout = new Bitmap(bitmap.Width, bitmap.Height);

                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                bitout.SetPixel(0, 0, Color.Black);
                bitout.SetPixel(1413, 999, Color.Black);
                foreach (PointF pointF in pointFs)
                    bitout.SetPixel((int) pointF.X, (int) pointF.Y, randomColor);
                if (!Directory.Exists("Images/Out/Line"))
                    Directory.CreateDirectory("Images/Out/Line");
                bitout.Save($"Images/Out/Line/{index}.png", ImageFormat.Png);
            }

            //Cuts out points in the lines
            lineParts.ReduceLines();
            for (int index = 0; index < lineParts.Count; index++)
            {
                List<PointF> pointFs = lineParts[index];
                Bitmap bitout = new Bitmap(bitmap.Width, bitmap.Height);

                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                bitout.SetPixel(0, 0, Color.Black);
                bitout.SetPixel(1413, 999, Color.Black);
                foreach (PointF pointF in pointFs)
                    bitout.SetPixel((int) pointF.X, (int) pointF.Y, randomColor);
                bitout.Save($"Images/Out/Line/{index} - reduced.png", ImageFormat.Png);
            }

            //Finds loops
            List<List<PointF>> loops = lineParts.CreateLoops();
            for (int index = 0; index < loops.Count; index++)
            {
                List<PointF> pointFs = loops[index];
                Bitmap bitout = new Bitmap(bitmap.Width, bitmap.Height);

                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                bitout.SetPixel(0, 0, Color.Black);
                bitout.SetPixel(1413, 999, Color.Black);
                foreach (PointF pointF in pointFs)
                    bitout.SetPixel((int) pointF.X, (int) pointF.Y, randomColor);
                bitout.Save($"Images/Out/Line/loop - {index}.png", ImageFormat.Png);
            }

            //Connects remaining lines
            lineParts.ConnectLines();
            for (int index = 0; index < lineParts.Count; index++)
            {
                List<PointF> pointFs = lineParts[index];
                Bitmap bitout = new Bitmap(bitmap.Width, bitmap.Height);

                Color randomColor = Color.FromArgb(rnd.Next(50, 200), rnd.Next(50, 200), rnd.Next(50, 200));
                bitout.SetPixel(0, 0, Color.Black);
                bitout.SetPixel(1413, 999, Color.Black);
                foreach (PointF pointF in pointFs)
                    bitout.SetPixel((int) pointF.X, (int) pointF.Y, randomColor);
                bitout.Save($"Images/Out/Line/line - {index}.png", ImageFormat.Png);
            }

            //Creates line objects
            List<Line> lines = LineCreation.CreateLineObjects(lineParts, loops, bitmap);

            //Creates a map
            Map map = new Map
            {
                Id = 3,
                Lines = lines,
                Ratio = 1.414
            };

            //Saves it
            File.WriteAllText("Images/Out/Line/3.json",
                JsonConvert.SerializeObject(map).Replace("\"IsEmpty\":false,", ""));
        }

        [TestMethod]
        public void LineCreationTest()
        {
            foreach (string image in A_ImageManipulationTests.Images)
            {
                Bitmap bitmap = new Bitmap($"Images/Out/{image}/5 thinned.png");
                Color[] pixels = bitmap.ToColorArray();

                //Create bool array based on black and white
                bool[][] boolArray = new bool[bitmap.Height][];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    boolArray[y] = new bool[bitmap.Width];
                    for (int x = 0; x < bitmap.Width; x++)
                        if (pixels[y * bitmap.Width + x].GetBrightness() < 0.1)
                            boolArray[y][x] = true;
                }

                //Creates part of lines
                List<List<PointF>> lineParts = boolArray.CreateLineParts();

                //Cuts out points in the lines
                lineParts.ReduceLines();

                //Finds loops
                List<List<PointF>> loops = lineParts.CreateLoops();

                //Connects remaining lines
                lineParts.ConnectLines();

                //Creates line objects
                List<Line> lines =
                    LineCreation.CreateLineObjects(lineParts, loops, new Bitmap($"Images/Out/{image}/3 balanced.png"));

                //Creates a map
                Map map = new Map
                {
                    Id = 3,
                    Lines = lines,
                    Ratio = 1.414
                };

                //Saves it
                File.WriteAllText($"Images/Out/{image}/3.json",
                    JsonConvert.SerializeObject(map).Replace("\"IsEmpty\":false,", ""));
            }
        }
    }
}