using AppMonitoring.SharedTypes;
using Microsoft.Extensions.Logging;
using Monitor.Blazor.Interfaces;
using Monitor.Infra.LogSink;
using Montior.Blazor.Data;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Monitor.Services
{
	public class MonitorAgentCommunicationLayer : IMonitorAgentCommunicationLayer
	{
		private IMonitorService _monitorService;
		private HttpClient httpClient = new HttpClient();
		private ILogger _logger;

		public MonitorAgentCommunicationLayer(
			IMonitorService monitorService,
			ILogger<MonitorAgentCommunicationLayer> logger)
		{
			_logger = logger;
			_monitorService = monitorService;

            _logger.LogInformation("Init MonitorAgentCommunicationLayer...");

            monitorService.SetMonitorAgentSettingsHandler = SetMonitorAgentSettings;
			monitorService.GetVmInfoAndListProcessInstaceInfoHandler = GetVmInfoAndListProcessInstaceInfo;
			monitorService.GetVmLogsHandler = GetVmLogs;
			monitorService.SetCompilerRequestHandler = SetCompilerRequest;
			monitorService.GenericCommandHandler = GenericCommand;
		}

		public void GenericCommand(IpAddress ipAddress, GenericCommand genericCommand)
		{
			var ipAdd = ipAddress.Ip;
			var ipPort = ipAddress.Port;

			PutObject<GenericCommand>(httpClient, ipAdd, ipPort, "api/MonitorAgent/GenericCommand", _logger, genericCommand);
		}

		public void SetCompilerRequest(IpAddress address, CompileInfo compileInfo)
		{
			var ipAdd = address.Ip;
			var ipPort = address.Port;

			PutObject<CompileInfo>(httpClient, ipAdd, ipPort, "api/MonitorAgent/SetCompilerRequest", _logger, compileInfo);
		}

		private Tuple<VmInfo, List<ProcessInstanceInfo>> GetVmInfoAndListProcessInstaceInfo(IpAddress address)
		{
			var ipAdd = address.Ip;
			var ipPort = address.Port;

			var infos = GetObject<Tuple<VmInfo, List<ProcessInstanceInfo>>>(httpClient, ipAdd, ipPort, "api/MonitorAgent/GetVmInfoAndListProcessInstaceInfo", _logger);

			return infos;
		}

        private List<LogInfo> GetVmLogs(IpAddress address)
        {
            var ipAdd = address.Ip;
            var ipPort = address.Port;

            var infos = GetObject<List<LogInfo>>(httpClient, ipAdd, ipPort, "api/MonitorAgent/GetLogs", _logger);

            return infos;
        }

        private static T GetObject<T>(HttpClient httpClient, string ipAdd, string ipPort, string link, ILogger logger)
		{
			T value = default;


			var task = httpClient.GetAsync($"http://{ipAdd}:{ipPort}/{link}");
			task.Wait();

			if (task.Result.IsSuccessStatusCode)
			{
				var task2 = task.Result.Content.ReadAsStringAsync();
				task2.Wait();

				var json = task2.Result;
				value = JsonConvert.DeserializeObject<T>(json);
			}
			else
			{
				logger.LogInformation($"HttpClient get command failed (http://{ipAdd}:{ipPort}/{link})");
			}

			return value;
		}

		private static void PutObject<T>(HttpClient httpClient, string ipAdd, string ipPort, string link, ILogger logger, T data)
		{
			string json = JsonConvert.SerializeObject(data, Formatting.Indented);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			var putAsync = httpClient.PutAsync($"http://{ipAdd}:{ipPort}/{link}", content);
			putAsync.Wait();
			if (putAsync.Result.IsSuccessStatusCode)
			{
				var responseContent = putAsync.Result.Content.ReadAsStringAsync();
				responseContent.Wait();
			}
			else
			{
				logger.LogInformation($"HttpClient put command failed (http://{ipAdd}:{ipPort}/{link})");
			}
		}

		public void SetMonitorAgentSettings(IpAddress address, MonitorAgentSettings monitorAgentSettings)
		{
			var ipAdd = address.Ip;
			var ipPort = address.Port;

            _logger.LogInformation("Sending SetMonitorAgentSettings...");            

            PutObject<MonitorAgentSettings>(httpClient, ipAdd, ipPort, "api/MonitorAgent/SetMonitorAgentSettings", _logger, monitorAgentSettings);
		}
	}
}
