using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MapAPI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class PaperDetection
    {
        private readonly string[] _images = {"img1", "img2"};

        [TestMethod]
        public void GetCPlusPlusOutput()
        {
            Process proc = new Process
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
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
                lines.Add(proc.StandardOutput.ReadLine());
            //Checks against command line output
            Assert.AreEqual(
                "[[{\"x\":595,\"y\":164},{\"x\":573,\"y\":208},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":355},{\"x\":367,\"y\":387},{\"x\":424,\"y\":304}],[{\"x\":600,\"y\":171},{\"x\":626,\"y\":184},{\"x\":604,\"y\":218},{\"x\":580,\"y\":205}],[{\"x\":596,\"y\":164},{\"x\":573,\"y\":207},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":855,\"y\":102},{\"x\":871,\"y\":547},{\"x\":228,\"y\":556}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":357},{\"x\":367,\"y\":388},{\"x\":425,\"y\":306}],[{\"x\":599,\"y\":172},{\"x\":626,\"y\":184},{\"x\":605,\"y\":216},{\"x\":581,\"y\":204}],[{\"x\":595,\"y\":164},{\"x\":572,\"y\":206},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":854,\"y\":101},{\"x\":871,\"y\":547},{\"x\":229,\"y\":557}]]",
                lines.Last());
        }

        [TestMethod]
        public void JsonConvertObject()
        {
            foreach (string image in _images)
            {
                Point[][] rectangles = OpenCVWrapper.IdentifyRectangles($"Images//{image}.jpg");
                //Checks valid json was output
                Assert.IsNotNull(rectangles);
                //Checks at least 1 rectangle was identifed
                Assert.AreNotEqual(rectangles.Length, 0);
            }
        }

        [TestMethod]
        public void CornerIdentification()
        {
            foreach (string image in _images)
            {
                Bitmap bitmap = new Bitmap($"Images//{image}.jpg");

                //Get rectangles
                Point[][] rectangles = OpenCVWrapper.IdentifyRectangles($"Images//{image}.jpg");
                //Get paper
                Point[] paper = ImageFunctions.IdentifyPaperCorners(bitmap, rectangles);

                //Draws all points on the image for manual checking
                foreach (Point corner in paper)
                    for (int xoff = -2; xoff < 3; xoff++)
                    for (int yoff = -2; yoff < 3; yoff++)
                        bitmap.SetPixel(corner.X + xoff, corner.Y + yoff, Color.Red);

                //Save modified image
                if (!Directory.Exists("Images//Out"))
                    Directory.CreateDirectory("Images//Out");
                bitmap.Save($"Images//Out//{image} - corners.png", ImageFormat.Png);
            }
        }
    }
}