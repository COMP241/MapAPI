using System.IO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MapAPI.Controllers
{
    [EnableCors("AllowAllOrigins")]
    [Route("api/[controller]")]
    public class MapController : Controller
    {
        private readonly IHostingEnvironment _environment;

        public MapController(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("{id}", Name = "GetMap")]
        public IActionResult GetById(int id)
        {
            string uploads = Path.Combine(_environment.ContentRootPath, "Maps");

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
            System.Threading.Thread.Sleep(5000);
            return new ObjectResult(System.IO.File.ReadAllText("Maps/1.json"));
        }
    }
}