using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MapAPI.Models
{
    public static class OpenCVWrapper
    {
        /// <summary>
        ///     Identifies all possible rectangles in an image.
        /// </summary>
        /// <param name="image">Image to detect rectangles in.</param>
        /// <param name="debugWindows">Show image with all identified rectangles drawn on it.</param>
        /// <returns>Returns array of all rectangles or null if there is an error.</returns>
        public static Point[][] IdentifyRectangles(string image, bool debugWindows = false)
        {
            //Starts the identify rectangle process
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "IdentifyRectangles",
                    Arguments = (debugWindows ? "-w " : "") + image,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                }
            };
            process.Start();

            //Read all the lines
            List<string> lines = new List<string>();
            while (!process.StandardOutput.EndOfStream)
                lines.Add(process.StandardOutput.ReadLine());

            try
            {
                //Tries to convert the last line from json
                return JsonConvert.DeserializeObject<Point[][]>(lines.Last());
            }
            catch (JsonReaderException)
            {
                //Returns null if there was an error (i.e. last line wasn't json)
                return null;
            }
        }
    }
}