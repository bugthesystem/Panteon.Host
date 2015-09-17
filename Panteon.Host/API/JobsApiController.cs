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
    public class JobsApiController : ApiController
    {
        private readonly IPanteonEngine _panteonEngine;

        public JobsApiController(IPanteonEngine panteonEngine)
        {
            _panteonEngine = panteonEngine;
        }

        [HttpGet]
        [Route("")]
        public Task<List<PanteonTaskInfo>> Get()
        {
            List<PanteonTaskInfo> result = _panteonEngine.GetTasks().Select(task => task.Inspect()).ToList();

            return Task.FromResult(result);
        }

        [HttpGet]
        [Route("stop/{name}")]
        public IHttpActionResult Stop(string name)
        {
            bool ok = _panteonEngine.StopTask(name);
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
            bool ok = _panteonEngine.StartTask(name);
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
