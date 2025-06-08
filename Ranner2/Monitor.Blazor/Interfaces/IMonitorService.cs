using AppMonitoring.SharedTypes;
using Monitor.Infra.LogSink;
using Montior.Blazor.Data;
using Newtonsoft.Json;
using System.Net;

namespace Monitor.Blazor.Interfaces
{
	public class MonitorPageSettings
	{
		public string CurrentFileName { get; set; } = string.Empty;

		public UI_ImagesVariables ImagesVariables { get; set; } = new UI_ImagesVariables();
		public UI_Configuration Configuration { get; set; } = new UI_Configuration();
		public UI_GroupTags GroupTags { get; set; } = new UI_GroupTags();
		public UI_GlobalVariables GlobalVariables { get; set; } = new UI_GlobalVariables();
		public UI_VmsData VmsData { get; set; } = new UI_VmsData();
		public UI_InstancesData InstancesData { get; set; } = new UI_InstancesData();

		public MonitorPageSettings Clone()
		{
			var clone = new MonitorPageSettings();
			
			var settingsJson = JsonConvert.SerializeObject(this, Formatting.Indented);
			clone = JsonConvert.DeserializeObject<MonitorPageSettings>(settingsJson);

			return clone;
		}
	}

	public interface IMonitorHandlingUI
    {
		void SetAllAgentsSettings(
			MonitorPageSettings monitorPageSettings, bool dontSave = false, bool settingsChangedFromRestApi = false);
		MonitorPageSettings GetCurrentSettings();
		Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>> GetCurrentSettingsInAgentsFormat();
		List<string> GetPresetsSettingsList();
		//List<string> GetPossibleProjectsList();
		string UserFileClosetAvailableName(string presetName);
		MonitorPageSettings GetSettingsFromPresetsSettings(string presetName);
		MonitorPageSettings GetSettingsFromUserSettings(string presetName);

		event Action<MonitorPageSettings> SettingsChangedFromRestApi;
		Dictionary<string, Tuple<VmInfo, Dictionary<int, ProcessInstanceInfo>>> GetAgentsPeriodicInfos();

		List<LogInfo> GetCurrentLogs();
	}

    public interface IMonitorHandlingAgents
    {
		void GenericCommand(IpAddress ipAddress, GenericCommand genericCommand);
		void SetCompilerRequest(IpAddress ipAddress, CompileInfo compileInfo);
		void SetMonitorAgentSettings(IpAddress ipAddress, MonitorAgentSettings monitorAgentSettings);
		Tuple<VmInfo, List<ProcessInstanceInfo>> GetVmInfoAndListProcessInstaceInfo(IpAddress ipAddress);
		List<Tuple<string, List<LogInfo>>> GetAllVmsLogs();		

		Action<IpAddress, MonitorAgentSettings> SetMonitorAgentSettingsHandler { get; set; }
		Func<IpAddress, Tuple<VmInfo, List<ProcessInstanceInfo>>> GetVmInfoAndListProcessInstaceInfoHandler { get; set; }
        Func<IpAddress, List<LogInfo>> GetVmLogsHandler { get; set; }
        Action<IpAddress, CompileInfo> SetCompilerRequestHandler { get; set; }
		Action<IpAddress, GenericCommand> GenericCommandHandler { get; set; }
	}

    public interface IMonitorService : IMonitorHandlingUI, IMonitorHandlingAgents
	{
		void KillAll();
		void StartAll();
        List<string> GetAllCsprojs();
        List<string> GetAllCsprojForBuild();
        List<string> GetAllCsProjForPublish();
        List<string> GetAllDeployedFoldersToCopy();

        public static MonitorPageSettings GetSettingsFromFullPath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File {fullPath} doesn't exist");
            }
            string settingsJson = File.ReadAllText(path);
            var instancesDataClone = JsonConvert.DeserializeObject<MonitorPageSettings>(settingsJson);

            instancesDataClone.Configuration.UpgradeIfNeeded();

            var configs = instancesDataClone.Configuration.Configuration.Where(x => x.Active).Reverse().ToList();
            foreach (var config in configs)
            {
                switch (config.Key)
                {
                    case UI_Configuration.ForceStartAllOnLoad:
                        {
                            foreach (var app in instancesDataClone.InstancesData.Instances)
                            {
                                if (app.DisabledByGroups || app.Disabled)
                                    app.RunOrStop = false;
                                else
                                    app.RunOrStop = bool.Parse(config.Value);
                            }
                        }
                        break;
                    case UI_Configuration.ForceActiveGroupsOnLoad:
                        {
                            instancesDataClone.InstancesData.ActiveGroups = config.Value;
                            instancesDataClone.InstancesData.UpdateDisableStateByGroups();
                        }
                        break;
                    case UI_Configuration.ForceConfigurationOnLoad:
                        {
                            foreach (var app in instancesDataClone.InstancesData.Instances)
                            {
                                app.Configuration = config.Value;

                                if (app.ApplicationWorkingDirectory.ToLowerInvariant().Contains("blazor")
                                    ||
                                    app.ApplicationPath.ToLowerInvariant().Contains("blazor")
                                    ||
                                    app.Name.ToLowerInvariant().Contains("blazor"))
                                {
                                    if (config.Value.ToLowerInvariant() == "release")
                                        app.Configuration = "Publish";
                                }
                            }
                        }
                        break;
                    case UI_Configuration.ForceProjectOnLoad:
                        {
                            instancesDataClone.InstancesData.Project = config.Value;
                        }
                        break;
                        //case UI_Configuration.AutodeploymentMinioIp:
                        //	break;
                        //case UI_Configuration.AutodeploymentMinioPort:
                        //	break;
                        //case UI_Configuration.AutodeploymentMinioUsername:
                        //	break;
                        //case UI_Configuration.AutodeploymentMinioPassword:
                        //	break;

                }
            }

            return instancesDataClone;
        }
    }
}
