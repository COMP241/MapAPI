using Microsoft.AspNetCore.Mvc;
using System;

namespace MapAPI.Controllers
{
    [Route("api/[controller]")]
    public class MapController : Controller
    {
        [HttpGet("{id}", Name = "GetMap")]
        public IActionResult GetById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public IActionResult Create()
        {
            throw new NotImplementedException();
        }
    }
}