using AppMonitoring.SharedTypes;
using Microsoft.AspNetCore.Mvc;
using Monitor.Agent.Services;
using Monitor.Infra.LogSink;
using System.Collections.Concurrent;

namespace Monitor.Agent.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MonitorAgentController : ControllerBase
    {
        private IMonitorAgentService _monitorAgentService;
		private ILogger<MonitorAgentController> _logger;
        private ConcurrentQueue<LogInfo> _logsQueue;

        public MonitorAgentController(
            IMonitorAgentService monitorAgentService, 
            ILogger<MonitorAgentController> logger,
            ConcurrentQueue<LogInfo> logsQueue)
        {
            _logsQueue = logsQueue;
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

        [HttpGet(Name = "GetLogs")]
        public ActionResult<List<LogInfo>> GetLogs()
        {
            var logs = new List<LogInfo>();
            var logsCount = _logsQueue.Count;
            for (int i = 0; i < logsCount; i++)
                if (_logsQueue.TryDequeue(out var log))
                    logs.Add(log);

            return new ActionResult<List<LogInfo>>(logs);
        }
    }
}
