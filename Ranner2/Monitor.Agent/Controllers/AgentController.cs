using AppMonitoring.SharedTypes;
using Microsoft.AspNetCore.Mvc;
using Monitor.Agent.Services;

namespace Monitor.Agent.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MonitorAgentController : ControllerBase
    {
        private IMonitorAgentService _monitorAgentService;
		private ILogger<MonitorAgentController> _logger;
        public MonitorAgentController(IMonitorAgentService monitorAgentService, ILogger<MonitorAgentController> logger)
        {
            _logger = logger;
            _monitorAgentService = monitorAgentService;
        }

		[HttpPut(Name = "GenericCommand")]
		public ActionResult GenericCommand([FromBody] GenericCommand genericCommand)
		{

            _logger.LogInformation($"Generic Command Invoked: {genericCommand.Command}");

			_monitorAgentService.InvokeGenericCommand(genericCommand);

			return Ok();
		}

		[HttpPut(Name = "SetCompilerRequest")]
		public ActionResult SetCompilerRequest([FromBody] CompileInfo compileInfo)
		{
			_monitorAgentService.InvokeCompile(compileInfo.InstanceId, compileInfo.Configuration);

			return Ok();
		}

		[HttpPut(Name = "SetMonitorAgentSettings")]
        public ActionResult SetMonitorAgentSettings([FromBody] MonitorAgentSettings monitorAgentSettings)
        {
            _logger.LogInformation("MonitorAgentSettings Message Received");
            _monitorAgentService.InvokeNewSettings(monitorAgentSettings);

            return Ok();
        }

        [HttpGet(Name = "GetMonitorAgentSettings")]
        public ActionResult<MonitorAgentSettings> GetMonitorAgentSettings()
        {
            return new ActionResult<MonitorAgentSettings>(_monitorAgentService.GetMonitorAgentSettings());
        }


        [HttpGet(Name = "GetVmInfoAndListProcessInstaceInfo")]
        public ActionResult<Tuple<VmInfo, List<ProcessInstanceInfo>>> GetVmInfoAndListProcessInstaceInfo()
        {
            return new ActionResult<Tuple<VmInfo, List<ProcessInstanceInfo>>>(_monitorAgentService.GetVmInfoAndListProcessInstaceInfo());
        }

		[HttpGet(Name = "KillAll")]
		public ActionResult<Tuple<VmInfo, List<ProcessInstanceInfo>>> KillAll()
		{
			_monitorAgentService.InvokeKillAll();

			return Ok();
		}

		[HttpGet(Name = "StartAll")]
		public ActionResult StartAll()
		{
            _monitorAgentService.InvokeStartAll();

            return Ok();
		}

		[HttpGet(Name = "Compile")]
		public ActionResult Compile(int id, string configuration)
		{
			_monitorAgentService.InvokeCompile(id, configuration);

			return Ok();
		}		
	}
}
