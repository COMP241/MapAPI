using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using MapAPI.Helpers;
using MapAPI.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace MapAPI.Controllers
{
    [EnableCors("AllowAllOrigins")]
    [Route("api/[controller]")]
    public class MapController : Controller
    {
        private static readonly object Lock = new object();
        private static int _id;
        private static readonly ConfigFile.Config Config;
        private readonly string _workingDirectory;

        static MapController()
        {
            if (!System.IO.File.Exists("NextId"))
                System.IO.File.WriteAllText("NextId", "0");

            _id = Convert.ToInt32(System.IO.File.ReadAllText("NextId"));

            Config = JsonConvert.DeserializeObject<ConfigFile.Config>(System.IO.File.ReadAllText("config.json"));
        }

        public MapController(IHostingEnvironment environment)
        {
            //Finds working directory   
#if DEBUG
            _workingDirectory = Path.Combine(environment.ContentRootPath, "bin", "Debug", "netcoreapp1.1");
#else
            _workingDirectory = environment.ContentRootPath;
#endif
        }

        [HttpGet("{id}", Name = "GetMap")]
        public IActionResult GetById(int id)
        {
            string uploads = Path.Combine(_workingDirectory, "Maps");

            try
            {
                //Gets file based on id
                string jsonFile = System.IO.File.ReadAllText(Path.Combine(uploads, id + ".json"));
                return new ObjectResult(jsonFile)
                {
                    //Sets the media type to be json instead of string
                    ContentTypes = new MediaTypeCollection
                    {
                        "application/json",
                        "charset=utf-8"
                    }
                };
            }
            catch
            {
                //404 if file isn't found
                NotFoundResult o = new NotFoundResult();
                return o;
            }
        }

        [HttpPost]
        public IActionResult Create(IFormCollection form)
        {
            #region Setup

            //Start of working directory
            string workingDirectory = Path.Combine(_workingDirectory, "Working ");
            int id = 0;

            //Gets the id for this upload, locked so only one thread can enter at a time
            lock (Lock)
            {
                id = _id;
                workingDirectory += _id;
                Directory.CreateDirectory(workingDirectory);
                _id++;
                System.IO.File.WriteAllText("NextId", _id.ToString());
            }

            //Saves the image and loads it as a bitmap
            using (FileStream fileStream = new FileStream(Path.Combine(workingDirectory, form.Files[0].FileName),
                FileMode.Create))
            {
                form.Files[0].CopyTo(fileStream);
            }
            Bitmap initialImage;
            //Will return an UnsupportedMediaTypeResult if file can't be loaded to a bitmap 
            try
            {
                initialImage = new Bitmap(Path.Combine(workingDirectory, form.Files[0].FileName));
            }
            catch (Exception)
            {
                Directory.Delete(workingDirectory, true);
                return new UnsupportedMediaTypeResult();
            }

#if DEBUG
            //Saves the starting image
            if (!Directory.Exists(Path.Combine(_workingDirectory, "Debug")))
                Directory.CreateDirectory(Path.Combine(_workingDirectory, "Debug"));
            initialImage.Save(Path.Combine(_workingDirectory, "Debug", "1 Initial.png"), ImageFormat.Png);
#endif

            #endregion

            #region Image Manipulation

            //Gets dimensions of scaled image
            double ratio = (double) initialImage.Width / initialImage.Height;
            int height = (int) Math.Sqrt(Config.PixelCounts.InitialImage / ratio);
            int width = Config.PixelCounts.InitialImage / height;

            //Scales image
            Rectangle scaledRect = new Rectangle(0, 0, width, height);
            Bitmap scaledImage = new Bitmap(width, height);
            scaledImage.SetResolution(initialImage.HorizontalResolution, initialImage.VerticalResolution);
            using (Graphics graphics = Graphics.FromImage(scaledImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(initialImage, scaledRect, 0, 0, initialImage.Width, initialImage.Height,
                        GraphicsUnit.Pixel, wrapMode);
                }
            }

            //Saves image to be used by OpenCV code
            scaledImage.Save(Path.Combine(workingDirectory, "scaled.png"), ImageFormat.Png);

#if DEBUG
            initialImage.Save(Path.Combine(_workingDirectory, "Debug", "2 Scaled.png"), ImageFormat.Png);
#endif

            //Finds possible rectangles with OpenCV
            Point[][] rectangles =
                OpenCVWrapper.IdentifyRectangles($"\"{Path.Combine(workingDirectory, "scaled.png")}\"");
            if (rectangles == null)
            {
                initialImage.Dispose();
                Directory.Delete(workingDirectory, true);
                throw new Exception("No rectangles were found.");
            }

#if DEBUG
            Bitmap temp = Debug.DrawPoints(scaledImage, rectangles);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "3 Rectangles.png"), ImageFormat.Png);
#endif

            //Finds the correct rectangle
            Point[] paper = scaledImage.IdentifyPaperCorners(rectangles);
            if (paper == null)
            {
                initialImage.Dispose();
                Directory.Delete(workingDirectory, true);
                throw new Exception("No paper was found.");
            }

#if DEBUG
            temp = Debug.DrawPoints(scaledImage, paper);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "4 Paper.png"), ImageFormat.Png);
#endif

            //Finds width and height of transformation
            ratio = 1.414;
            height = (int) Math.Sqrt(Config.PixelCounts.TrandformedImage / ratio);
            width = Config.PixelCounts.TrandformedImage / height;

            //Transforms image
            Bitmap perspectiveImage = scaledImage.PerspectiveTransformImage(paper, width, height);

#if DEBUG
            perspectiveImage.Save(Path.Combine(_workingDirectory, "Debug", "5 Perspective.png"), ImageFormat.Png);
#endif

            #endregion

            #region Color Identification

            //Checks each pixels threshold
            bool[][] threshold = perspectiveImage.CreateThresholdArrayAndBalance();

#if DEBUG
            temp = Debug.BitmapFromBool(threshold);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "6 Threshold.png"), ImageFormat.Png);
            perspectiveImage.Save(Path.Combine(_workingDirectory, "Debug", "7 Correction.png"), ImageFormat.Png);
#endif

            //Thins points
            threshold.ZhangSuenThinning();

#if DEBUG
            temp = Debug.BitmapFromBool(threshold);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "8 Thinned.png"), ImageFormat.Png);
#endif

            #endregion

            #region Line Identification

            //Finds lines
            List<List<PointF>> lineParts = threshold.CreateLineParts();

            //Reduces number of points in lines
            lineParts.ReduceLines();

            //Finds loops
            List<List<PointF>> loops = lineParts.CreateLoops();

            //Joins remaining lines
            lineParts.ConnectLines();

            //Create line objects
            List<Line> lines = LineCreation.CreateLineObjects(lineParts, loops, perspectiveImage);

            //Creates a map
            Map map = new Map
            {
                Id = id,
                Lines = lines,
                Ratio = 1.414
            };

            //Converts map to json
            string json = JsonConvert.SerializeObject(map).Replace("\"IsEmpty\":false,", "");

            //Saves it
            System.IO.File.WriteAllText(Path.Combine(_workingDirectory, "Maps", $"{id}.json"), json);

#if DEBUG
            System.IO.File.WriteAllText(Path.Combine(_workingDirectory, "Debug", "9 Json.json"), json);
#endif

            #endregion
            initialImage.Dispose();
            Directory.Delete(workingDirectory, true);

            //Returns map
            return new ObjectResult(json)
            {
                //Sets the media type to be json instead of string
                ContentTypes = new MediaTypeCollection
                {
                    "application/json",
                    "charset=utf-8"
                }
            };
        }
    }
}