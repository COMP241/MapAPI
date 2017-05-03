using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MapAPI.Helpers;
using MapAPI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace UnitTests
{
    [TestClass]
    public class PaperDetection
    {
        private readonly string[] _images =
        {
            "img1",
            //"img2",
            "img3",
            "img4",
            "img5",
            "img6",
            "img7",
            "img1L",
            //"img2L",
            //"img3L",
            //"img4L",
            "img5L",
            "img6L",
            "img7L"
        };

        [TestMethod]
        public void GetCPlusPlusOutputTest()
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "IdentifyRectangles",
                    Arguments = "Images\\img1.jpg",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                }
            };

            List<string> lines = new List<string>();
            process.Start();
            while (!process.StandardOutput.EndOfStream)
                lines.Add(process.StandardOutput.ReadLine());
            //Checks against command line output
            Assert.AreEqual(
                "[[{\"x\":732,\"y\":191},{\"x\":1171,\"y\":480},{\"x\":884,\"y\":779},{\"x\":442,\"y\":400}],[{\"x\":734,\"y\":190},{\"x\":1171,\"y\":480},{\"x\":884,\"y\":779},{\"x\":442,\"y\":400}],[{\"x\":734,\"y\":190},{\"x\":1171,\"y\":480},{\"x\":884,\"y\":779},{\"x\":442,\"y\":400}]]",
                lines.Last());
        }

        [TestMethod]
        public void GetPaperFromImageTest()
        {
            foreach (string image in _images)
            {
                //Gets rectangles
                Point[][] rectangles = OpenCVWrapper.IdentifyRectangles($"Images//{image}.jpg");
                //Checks valid json was output
                Assert.IsNotNull(rectangles);
                //Checks at least 1 rectangle was identified
                Assert.AreNotEqual(rectangles.Length, 0);

                Bitmap bitmap = new Bitmap($"Images//{image}.jpg");
                //Get rectangle that corresponds to the paper (I hope) 
                Point[] paper = ImageFunctions.IdentifyPaperCorners(bitmap, rectangles);
                Assert.IsNotNull(paper);

                //Draws all points on the image for manual checking
                Bitmap tempBitmap = new Bitmap(bitmap);
                foreach (Point corner in paper)
                    for (int xOff = -2; xOff < 3; xOff++)
                    for (int yOff = -2; yOff < 3; yOff++)
                        if (corner.X + xOff < tempBitmap.Width && corner.Y + yOff < tempBitmap.Height &&
                            corner.X + xOff >= 0 &&
                            corner.Y + yOff >= 0)
                            tempBitmap.SetPixel(corner.X + xOff, corner.Y + yOff, Color.Red);

                //Save modified image
                if (!Directory.Exists("Images//Out"))
                    Directory.CreateDirectory("Images//Out");
                tempBitmap.Save($"Images//Out//{image} - corners.png", ImageFormat.Png);

                //Transforms and saves image for manual checking
                bitmap = ImageFunctions.PerspectiveTransformImage(bitmap, paper, 1414, 1000);
                bitmap.Save($"Images//Out//{image} - transform.png", ImageFormat.Png);
            }
        }

        [TestMethod]
        public void OrderClockwiseTest()
        {
            //Prefect rectangle
            TryAllOrders(new[] {new Point(100, 100), new Point(200, 100), new Point(200, 200), new Point(100, 200)});

            //Rotated Rectangle
            TryAllOrders(new[] {new Point(100, 100), new Point(300, 50), new Point(350, 150), new Point(150, 200)});

            //Warped Rectangle
            TryAllOrders(new[] {new Point(125, 100), new Point(300, 50), new Point(275, 150), new Point(150, 200)});

            //Difficult case
            TryAllOrders(new[] {new Point(100, 100), new Point(200, 90), new Point(200, 200), new Point(90, 200) });

            void TryAllOrders(Point[] testPoints)
            {
                //Shifts points along
                for (int i = 0; i < 4; i++)
                {
                    Point[] testOrder =
                    {
                        testPoints[0 + i],
                        testPoints[1 + i - (i > 2 ? 4 : 0)],
                        testPoints[2 + i - (i > 1 ? 4 : 0)],
                        testPoints[3 + i - (i > 0 ? 4 : 0)]
                    };
                    testOrder.OrderClockwise();
                    CollectionAssert.AreEqual(testPoints, testOrder);
                }

                //Reverse order and shifts points along
                for (int i = 0; i < 4; i++)
                {
                    Point[] testOrder =
                    {
                        testPoints[3 + i - (i > 0 ? 4 : 0)],
                        testPoints[2 + i - (i > 1 ? 4 : 0)],
                        testPoints[1 + i - (i > 2 ? 4 : 0)],
                        testPoints[0 + i]
                    };
                    testOrder.OrderClockwise();
                    CollectionAssert.AreEqual(testPoints, testOrder);
                }
            }
        }

        [TestMethod]
        public void JsonConversionTest()
        {
            Map map = new Map
            {
                Id = 0,
                Ratio = 1.414,
                Lines = new List<Line>
                {
                    new Line
                    {
                        Color = Line.Colors.Red,
                        Loop = false,
                        Points = new List<PointF>
                        {
                            new PointF(0.1F, 0.1F),
                            new PointF(0.2F, 0.05F),
                            new PointF(0.4F, 0.25F)
                        }
                    },
                    new Line
                    {
                        Color = Line.Colors.Green,
                        Loop = true,
                        Points = new List<PointF>
                        {
                            new PointF(0.5F, 0.5F),
                            new PointF(0.7F, 0.35F),
                            new PointF(0.7F, 0.8F)
                        }
                    }
                }
            };
            
            string json = JsonConvert.SerializeObject(map);
            json = json.Replace("\"IsEmpty\":false,", "");

            if (!Directory.Exists("Maps"))
                Directory.CreateDirectory("Maps");
            File.WriteAllText("Maps/0.json", json);

            Map test = JsonConvert.DeserializeObject<Map>(File.ReadAllText("Maps/0.json"));
            Assert.IsNotNull(test);
        }
    }
}