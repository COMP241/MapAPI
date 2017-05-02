using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MapAPI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class PaperDetection
    {
        private readonly string[] _images =
        {
            "img1",
            "img2",
            "img3",
            //"img4",
            //"img5",
            "img6",
            "img7"
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
                "[[{\"x\":595,\"y\":164},{\"x\":573,\"y\":208},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":355},{\"x\":367,\"y\":387},{\"x\":424,\"y\":304}],[{\"x\":600,\"y\":171},{\"x\":626,\"y\":184},{\"x\":604,\"y\":218},{\"x\":580,\"y\":205}],[{\"x\":596,\"y\":164},{\"x\":573,\"y\":207},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":855,\"y\":102},{\"x\":871,\"y\":547},{\"x\":228,\"y\":556}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":357},{\"x\":367,\"y\":388},{\"x\":425,\"y\":306}],[{\"x\":599,\"y\":172},{\"x\":626,\"y\":184},{\"x\":605,\"y\":216},{\"x\":581,\"y\":204}],[{\"x\":595,\"y\":164},{\"x\":572,\"y\":206},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":854,\"y\":101},{\"x\":871,\"y\":547},{\"x\":229,\"y\":557}]]",
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
                        if (corner.X + xOff < tempBitmap.Width && corner.Y + yOff < tempBitmap.Height && corner.X + xOff >= 0 &&
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

            //Extra Test
            TryAllOrders(new[] {new Point(232, 101), new Point(855, 102), new Point(871, 547), new Point(228, 556)});

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
    }
}