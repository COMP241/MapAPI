using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MapAPI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace UnitTests
{
    [TestClass]
    public class PaperDetection
    {
        private readonly string[] _images = {"Images\\img1.jpg", "Images\\img2.jpg"};
                                    
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
            {
                lines.Add(proc.StandardOutput.ReadLine());
            }
            Assert.AreEqual("[[{\"x\":595,\"y\":164},{\"x\":573,\"y\":208},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":355},{\"x\":367,\"y\":387},{\"x\":424,\"y\":304}],[{\"x\":600,\"y\":171},{\"x\":626,\"y\":184},{\"x\":604,\"y\":218},{\"x\":580,\"y\":205}],[{\"x\":596,\"y\":164},{\"x\":573,\"y\":207},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":855,\"y\":102},{\"x\":871,\"y\":547},{\"x\":228,\"y\":556}],[{\"x\":336,\"y\":276},{\"x\":289,\"y\":357},{\"x\":367,\"y\":388},{\"x\":425,\"y\":306}],[{\"x\":599,\"y\":172},{\"x\":626,\"y\":184},{\"x\":605,\"y\":216},{\"x\":581,\"y\":204}],[{\"x\":595,\"y\":164},{\"x\":572,\"y\":206},{\"x\":605,\"y\":226},{\"x\":634,\"y\":180}],[{\"x\":232,\"y\":101},{\"x\":854,\"y\":101},{\"x\":871,\"y\":547},{\"x\":229,\"y\":557}]]", lines.Last());
        }
        
        [TestMethod]
        public void JsonConvertObject()
        {
            var x = JsonConvert.DeserializeObject<Point[][]>("[]");
            foreach (string image in _images)
            {
                Point[][] rectangles = OpenCVWrapper.IdentifyRectangles(image);
                Assert.IsNotNull(rectangles);
                Assert.AreNotEqual(rectangles.Length, 0);
            }
        }
    }
}
