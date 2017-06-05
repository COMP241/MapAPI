using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using MapAPI.Helpers;
using MapAPI.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
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

        [HttpGet(Name = "GetMapIds")]
        public IActionResult GetByIdRange()
        {
            string[] mapFiles = Directory.GetFiles(Path.Combine(_workingDirectory, "Maps"));

            int[] ids = mapFiles.Select(Path.GetFileNameWithoutExtension).Select(fileId => Convert.ToInt32(fileId))
                .ToArray();
            Array.Sort(ids);

            return new ObjectResult(ids);
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

        [HttpGet("{index1}-{index2}", Name = "GetMapRange")]
        public IActionResult GetByIdRange(int index1, int index2)
        {
            List<string> mapFiles = Directory.GetFiles(Path.Combine(_workingDirectory, "Maps")).Select(Path.GetFileName)
                .ToList();
            mapFiles.Sort((file1, file2) => Convert.ToInt32(Path.GetFileNameWithoutExtension(file1))
                .CompareTo(Convert.ToInt32(Path.GetFileNameWithoutExtension(file2))));

            if (index2 < index1 || index1 < 0 || index2 > mapFiles.Count - 1)
                return new BadRequestResult();

            string[] selectMapFiles = mapFiles.Skip(index1).Take(index2 - index1 + 1).ToArray();

            string finalJson = selectMapFiles.Aggregate("",
                (current, selectMapFile) => current + System.IO.File
                                                .ReadAllText(Path.Combine(_workingDirectory, "Maps", selectMapFile))
                                                .Trim() + '\n');

            return new ObjectResult(finalJson);
        }

        [HttpPost]
        public IActionResult Create(IFormCollection form)
        {
            //Start of working directory
            string workingDirectory = Path.Combine(_workingDirectory, "Working ");
            // ReSharper disable once RedundantAssignment, needs to be there for non debug compile
            Bitmap initialImage = null;

#if !DEBUG
            try
            {
#endif

            #region Setup

            //Checks there is a file
            if (form.Files.Count == 0)
                return new BadRequestResult();

            //Gets the id for this upload, locked so only one thread can enter at a time
            int id = SetupId(ref workingDirectory);
            //Cleans up Working folders that are leftover for some reason on every 20th pass
            if (id % 20 == 0)
            {
                Thread cleanWorkingDirectories = new Thread(CleanWorkingDirectories);
                cleanWorkingDirectories.Start();
            }
            //Saves the file sent and get if transformation is needed
            bool transform = ProcessFormData(form, workingDirectory);
            //Tries to load file sent as image and will return an UnsupportedMediaTypeResult if file can't be loaded to a bitmap 
            try
            {
                //Tries to load the image
                initialImage = LoadImage(form, workingDirectory);
            }
            catch (Exception)
            {
                Directory.Delete(workingDirectory, true);
                return new UnsupportedMediaTypeResult();
            }

            #endregion

            #region Image Manipulation

            //Scales image to be less than a certain number of pixels
            Bitmap scaledImage = ScaleImage(workingDirectory, initialImage, transform);
            Bitmap perspectiveImage;
            //Will only run this if transform flag has been checked
            if (transform)
            {
                //Finds possible rectangles with OpenCV
                Point[][] rectangles =
                    OpenCVWrapper.IdentifyRectangles($"\"{Path.Combine(workingDirectory, "scaled.png")}\"");
                if (rectangles == null || rectangles.Length == 0)
                {
                    initialImage.Dispose();
                    Directory.Delete(workingDirectory, true);
                    return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
                }

#if DEBUG
                Bitmap temp = Debug.DrawPoints(scaledImage, rectangles);
                temp.Save(Path.Combine(_workingDirectory, "Debug", "3 Rectangles.png"), ImageFormat.Png);
#endif

                //Finds the correct rectangle
                Point[] paper = scaledImage.IdentifyPaperCorners(rectangles);
                if (paper == null || paper.Length != 4)
                {
                    initialImage.Dispose();
                    Directory.Delete(workingDirectory, true);
                    return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
                }

#if DEBUG
                temp = Debug.DrawPoints(scaledImage, paper);
                temp.Save(Path.Combine(_workingDirectory, "Debug", "4 Paper.png"), ImageFormat.Png);
#endif

                perspectiveImage = TransformImage(scaledImage, paper);
            }
            else
            {
                perspectiveImage = scaledImage;
            }

            #endregion

            #region Color Identification

            //Gets threshold array for image
            bool[][] threshold = CreateThresholds(perspectiveImage);
            //Thins lines
            ThinLines(threshold);

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

            #endregion

            #region Map Creation

            //Creates a map
            Map map = new Map
            {
                Id = id,
                Lines = lines,
                Ratio = (double) perspectiveImage.Width / perspectiveImage.Height
            };

            //Converts map to json
            string json = JsonConvert.SerializeObject(map).Replace("\"IsEmpty\":false,", "");

            SaveMap(id, json);

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
#if !DEBUG
        }
            catch
            {
                initialImage?.Dispose();
                if (Directory.Exists(workingDirectory))
                    Directory.Delete(workingDirectory ,true);
                return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
            }
#endif
        }

        private static int SetupId(ref string workingDirectory)
        {
            int id;
            lock (Lock)
            {
                id = _id;
                workingDirectory += _id;
                Directory.CreateDirectory(workingDirectory);
                _id++;
                System.IO.File.WriteAllText("NextId", _id.ToString());
            }

            return id;
        }

        private static bool ProcessFormData(IFormCollection form, string workingDirectory)
        {
            //Checks if image should be transformed
            bool transform = true;
            if (form.TryGetValue("transform", out StringValues value))
                if (value[0] == "false")
                    transform = false;

            //Saves the image and loads it as a bitmap
            using (FileStream fileStream = new FileStream(Path.Combine(workingDirectory, form.Files[0].FileName),
                FileMode.Create))
            {
                form.Files[0].CopyTo(fileStream);
            }

            return transform;
        }

        private Bitmap LoadImage(IFormCollection form, string workingDirectory)
        {
            Bitmap initialImage = new Bitmap(Path.Combine(workingDirectory, form.Files[0].FileName));

#if DEBUG
            //Saves the starting image
            if (!Directory.Exists(Path.Combine(_workingDirectory, "Debug")))
                Directory.CreateDirectory(Path.Combine(_workingDirectory, "Debug"));
            initialImage.Save(Path.Combine(_workingDirectory, "Debug", "1 Initial.png"), ImageFormat.Png);
#endif
            return initialImage;
        }

        private Bitmap ScaleImage(string workingDirectory, Bitmap initialImage, bool transform)
        {
            Bitmap scaledImage;
            int pixelLimit = transform ? Config.PixelCounts.InitialImage : Config.PixelCounts.TransformedImage;
            if (initialImage.Width * initialImage.Height > pixelLimit)
            {
                //Gets dimensions of scaled image
                double ratio = (double) initialImage.Width / initialImage.Height;
                int height = (int) Math.Sqrt(pixelLimit / ratio);
                int width = pixelLimit / height;

                //Scales image in a poor way cause the proper way didn't work on Linux
                scaledImage =
                    initialImage.PerspectiveTransformImage(
                        new[]
                        {
                            new Point(0, 0), new Point(initialImage.Width - 1, 0),
                            new Point(initialImage.Width - 1, initialImage.Height - 1),
                            new Point(0, initialImage.Height - 1)
                        }, width, height);
            }
            else
            {
                scaledImage = initialImage.Copy();
            }

            //Saves image to be used by OpenCV code
            scaledImage.Save(Path.Combine(workingDirectory, "scaled.png"), ImageFormat.Png);

#if DEBUG
            initialImage.Save(Path.Combine(_workingDirectory, "Debug", "2 Scaled.png"), ImageFormat.Png);
#endif
            return scaledImage;
        }

        private Bitmap TransformImage(Bitmap scaledImage, Point[] paper)
        {
            Bitmap perspectiveImage;
            //Finds width and height of transformation
            double ratio = 1.414;
            int height = (int) Math.Sqrt(Config.PixelCounts.TransformedImage / ratio);
            int width = Config.PixelCounts.TransformedImage / height;

            //Transforms image
            perspectiveImage = scaledImage.PerspectiveTransformImage(paper, width, height);

#if DEBUG
            perspectiveImage.Save(Path.Combine(_workingDirectory, "Debug", "5 Perspective.png"), ImageFormat.Png);
#endif
            return perspectiveImage;
        }

        private bool[][] CreateThresholds(Bitmap perspectiveImage)
        {
            //Checks each pixels threshold
            bool[][] threshold = perspectiveImage.CreateThresholdArrayAndBalance();

#if DEBUG
            Bitmap temp = Debug.BitmapFromBool(threshold);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "6 Threshold.png"), ImageFormat.Png);
            perspectiveImage.Save(Path.Combine(_workingDirectory, "Debug", "7 Correction.png"), ImageFormat.Png);
#endif
            return threshold;
        }

        private void ThinLines(bool[][] threshold)
        {
            //Thins points
            threshold.ZhangSuenThinning();

#if DEBUG
            Bitmap temp = Debug.BitmapFromBool(threshold);
            temp.Save(Path.Combine(_workingDirectory, "Debug", "8 Thinned.png"), ImageFormat.Png);
#endif
        }

        private void SaveMap(int id, string json)
        {
            //Saves it
            if (!Directory.Exists(Path.Combine(_workingDirectory, "Maps")))
                Directory.CreateDirectory(Path.Combine(_workingDirectory, "Maps"));
            System.IO.File.WriteAllText(Path.Combine(_workingDirectory, "Maps", $"{id}.json"), json);

#if DEBUG
            System.IO.File.WriteAllText(Path.Combine(_workingDirectory, "Debug", "9 Json.json"), json);
#endif
        }

        private void CleanWorkingDirectories()
        {
            DirectoryInfo workingDirectoryInfo = new DirectoryInfo(_workingDirectory);
            List<DirectoryInfo> workingFolders = workingDirectoryInfo.GetDirectories()
                .Where(x => x.Name.Contains("Working")).ToList();
            foreach (DirectoryInfo workingFolder in workingFolders)
                if (workingFolder.CreationTime < DateTime.Now.AddSeconds(-120))
                    workingFolder.Delete(true);
        }
    }
}