using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Panteon.Host.Interface;
using Panteon.Sdk.Models;

namespace Panteon.Host.API
{
    [RoutePrefix("tasks")]
    public class TaskController : ApiController
    {
        private readonly IPanteonEngine _panteon;

        public TaskController(IPanteonEngine panteon)
        {
            _panteon = panteon;
        }

        [HttpGet]
        [Route("")]
        public Task<List<PanteonTaskInfo>> Get()
        {
            List<PanteonTaskInfo> result = _panteon.GetTasks().Select(task => task.Inspect()).ToList();

            return Task.FromResult(result);
        }

        [HttpGet]
        [Route("stop/{name}")]
        public IHttpActionResult Stop(string name)
        {
            bool ok = _panteon.StopTask(name);
            if (ok)
            {
                return Ok();
            }

            return BadRequest();
        }

        [HttpGet]
        [Route("start/{name}")]
        public IHttpActionResult Start(string name)
        {
            bool ok = _panteon.StartTask(name);
            if (ok)
            {
                return Ok();
            }

            return BadRequest();
        }


        [HttpGet]
        [Route("echo")]
        public DateTime Time()
        {
            return DateTime.Now;
        }
    }
}
