using AppMonitoring.SharedTypes;
using Microsoft.AspNetCore.Mvc;
using Monitor.Blazor.Interfaces;

namespace Monitor.Blazor.Controllers
{
    [ApiController]
    [Route("/api/Rest")]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        public MonitorController(IMonitorService monitorService)
        {
               _monitorService = monitorService;
        }

        [HttpGet("KillAll")]
        public ActionResult KillAll()
        {
            _monitorService.KillAll();
            return Ok();
        }

        [HttpGet("StartAll")]
        public ActionResult StartAll()
        {
            _monitorService.StartAll();
            return Ok();
        }

        [HttpGet("GetAllCsProjs")]
        public ActionResult GetAllCsProjs()
        {
            _monitorService.GetAllCsprojs();
            return Ok();
        }

        [HttpGet("GetAllCsProjForBuild")]
        public ActionResult GetAllCsProjForBuild()
        {
            _monitorService.GetAllCsprojForBuild();
            return Ok();
        }

        [HttpGet("GetAllCsProjForPublish")]
        public ActionResult GetAllCsProjForPublish()
        {
            _monitorService.GetAllCsProjForPublish();
            return Ok();
        }

        [HttpGet("GetAllDeployedFoldersToCopy")]
        public ActionResult GetAllDeployedFoldersToCopy()
        {
            _monitorService.GetAllDeployedFoldersToCopy();
            return Ok();
        }
    }
}
