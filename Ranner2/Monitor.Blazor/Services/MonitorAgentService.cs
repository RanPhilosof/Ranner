using AppMonitoring.SharedTypes;
using CommandLine;
using Monitor.Blazor.Converters;
using Monitor.Blazor.Interfaces;
using Monitor.Infra;
using Montior.Blazor.Data;
using MudBlazor;
using MudBlazor.Extensions;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using static MudBlazor.CategoryTypes;

namespace Monitor.Blazor.Services
{
	public class BlazorOptions
	{
		[Option('l', "lastSettings", Required = false, HelpText = "Is To Start With Last Settings")]
		public bool IsToStartWithLastSettings { get; set; } = true;

		[Option('c', "configurationSettings", Required = false, HelpText = "Configuration File")]
		public string ConfigurationFile { get; set; } = string.Empty;

		[Option('m', "multiFileConfigurationSettings", Required = false, HelpText = "Multi Configuration File. (ConfigRepo1Json:RootFolderRepo1),(ConfigRepo2Json:RootFolderRepo2),. RootFolderRepo Template In App {rootFolder}")]
		public string MultiConfigurationFiles { get; set; } = string.Empty;

		[Option('g', "groups", Required = false, HelpText = "Groups to do action on. ,SV")]
		public string Groups { get; set; } = string.Empty;
		
        [Option('a', "action", Required = false, HelpText = "Group to do action on")]
		public string Action { get; set; } = string.Empty;
        [Option('t', "addGroupTags", Required = false, HelpText = "Additional group tags")]
        public string AddGroupTags { get; set; } = string.Empty;
        [Option('r', "startAgent", Required = false, HelpText = "Start Agent Automaticaly")]
		public bool StartAgent{ get; set; } = false;
		[Option('p', "port", Required = false, HelpText = "Port")]
		public string Port { get; set; } = string.Empty;
        [Option('w', "releaseOrDebug", Required = false, HelpText = "releaseOrDebug")]
        public string ReleaseOrDebug { get; set; } = "Debug";
		[Option('z', "project", Required = false, HelpText = "Argos / Mewp / etc...")]
		public string Project { get; set; } = string.Empty;
		[Option('u', "loggerFilePath", Required = false, HelpText = "File Path To Write The Logs")]
		public string LoggerFilePath { get; set; } = string.Empty;		
		[Option('b', "packageFolder", Required = false, HelpText = "Package Folder Template {packageFolder}")]
		public string PackageFolder { get; set; } = string.Empty;
		[Option('d', "fetchInfoTimeInterval", Required = false, HelpText = "Fetch Info From Agents Timer Time Interval_mSec")]
		public int FetchInfoFromAgentsTimerTimeInterval_mSec { get; set; } = 5000;
		[Option('e', "vms", Required = false, HelpText = "Override Vms By Order. ;SV in next format vm1,10.10.40.101,4231;vm2,10.10.40.102,4231;")]
		public string Vms { get; set; } = "vm1,localhost,4231;";
        [Option('f', "appsStartStateOn", Required = false, HelpText = "Apps Start State On")]
        public bool AppsStartState { get; set; } = false;
        [Option('h', "forceBlazorDir", Required = false, HelpText = "Dont Force Blazor Configuration Floder To Publish")]
        public bool DontForceBlazorConfigurationFloderToPublish { get; set; } = false;


        public override string ToString()
		{
			return $"IsToStartWithLastSettings: {IsToStartWithLastSettings}, ConfigurationFile: {ConfigurationFile}, Groups: {Groups}, Action: {Action}, AddGroupTags: {AddGroupTags}, StartAgent:{StartAgent}, Port: {Port}, ReleaseOrDebug: {ReleaseOrDebug}, Project: {Project}, LoggerFilePath: {LoggerFilePath}";
		}
	}

	public class MonitorAgentService : IMonitorService 
	{
		private Timer updateTimer;
		private int fetchInfoFromAgentsTimerTimeInterval_mSec = 5000;

		public MonitorAgentService(ILogger<MonitorAgentService> logger)
        {
			//Debugger.Launch();

	        _logger = logger;

			var parsedArgs = Parser.Default.ParseArguments<BlazorOptions>(Environment.GetCommandLineArgs());

			if (parsedArgs.Value == null)
			{
				_logger.LogError("Invalid Arguments");
				return;
			}

			if (parsedArgs != null && parsedArgs.Value != null) 
				fetchInfoFromAgentsTimerTimeInterval_mSec = parsedArgs.Value.FetchInfoFromAgentsTimerTimeInterval_mSec;

			updateTimer = new Timer(UpdateAgentsPeriodicInfosWrapper, null, fetchInfoFromAgentsTimerTimeInterval_mSec, Timeout.Infinite);

			_logger.LogInformation("Parsed Args: {ParsedArgs}",parsedArgs.Value);



            if (!string.IsNullOrEmpty(parsedArgs.Value.MultiConfigurationFiles))
			{
                string activeGroupsOverride = string.Empty;
				if (!string.IsNullOrEmpty(parsedArgs.Value.Groups))
					activeGroupsOverride = parsedArgs.Value.Groups;

				string activeProjectOverride = string.Empty;
				if (!string.IsNullOrEmpty(parsedArgs.Value.Project))
					activeProjectOverride = parsedArgs.Value.Project;

				string configurationOverride = string.Empty;
				if (!string.IsNullOrEmpty(parsedArgs.Value.ReleaseOrDebug))
					configurationOverride = parsedArgs.Value.ReleaseOrDebug;

				string packageFolderOverride = string.Empty;
				if (!string.IsNullOrEmpty(parsedArgs.Value.PackageFolder))
					packageFolderOverride = parsedArgs.Value.PackageFolder;

				bool appsStartState = parsedArgs.Value.AppsStartState;

                bool dontForceBlazorConfigurationFloderToPublish = parsedArgs.Value.DontForceBlazorConfigurationFloderToPublish;

                var jsonEnvironmentArgs = parsedArgs.Value.MultiConfigurationFiles.Split(",").Where(x => !string.IsNullOrEmpty(x)).ToList();
                
                var files = new List<string>();
                foreach (var env in jsonEnvironmentArgs)
                {
                    if (!string.IsNullOrEmpty(env))
                    {
                        string? filePath = Environment.GetEnvironmentVariable(env);
                        if (filePath != null)
                        {
                            files.Add(filePath);
                        }
                    }
                }

                var filesWithRootDir = new List<Tuple<string, string>>();

                foreach (var file in files)
                {
                    var fileAndRootFolder = file.Split("|").Where(x => !string.IsNullOrEmpty(x)).ToList();
                    if (fileAndRootFolder.Count == 1)
                        filesWithRootDir.Add(Tuple.Create(fileAndRootFolder[0], ""));
                    else
						filesWithRootDir.Add(Tuple.Create(fileAndRootFolder[0], fileAndRootFolder[1]));
				}

                string vmsOverride = string.Empty;
                if (!string.IsNullOrEmpty(parsedArgs.Value.Vms))
                    vmsOverride = parsedArgs.Value.Vms;

				lastAppliedSettings = LoadMultiConfigFile(
					filesWithRootDir,
					activeGroupsOverride, 
                    activeProjectOverride,
					configurationOverride,
                    packageFolderOverride,
                    vmsOverride,
                    appsStartState,
                    !dontForceBlazorConfigurationFloderToPublish);

				_logger.LogInformation("Will attempt to start with a MultiConfigurationFiles. " +
                                       "The file pathes are defined in the following environment variables: {MultiConfigurationFiles}",
									   parsedArgs.Value.MultiConfigurationFiles);
				return;
			}            

			if (!string.IsNullOrEmpty(parsedArgs.Value.ConfigurationFile))
            {
	            _logger.LogInformation("Will attempt to start with a configuration file: {ConfigurationFile}",
	                                   parsedArgs.Value.ConfigurationFile);
	            return;
            }

            if (parsedArgs.Value.IsToStartWithLastSettings)
            {
	            _logger.LogInformation("Attempting to start with a last settings");
	            //we get here only if IsToStartWithLastSetting is true.
	            LoadLastConfiguration();
            }

        }

        private MonitorPageSettings LoadMultiConfigFile(
            List<Tuple<string,string>> configFiles,
            string activeGroupsOverride,
            string activeProjectOverride,
            string configurationOverride,            
			string packageFolderOverride,
			string overrideVms,
            bool appsStartState,
            bool forceBlazorConfigurationFolderToPublish)
        {
            var combinedSettings = new MonitorPageSettings();

			var monitorPageSettings = new List<MonitorPageSettings>();

			#region Load From Files
			foreach (var fullPath in configFiles)
            {
                if (File.Exists(fullPath.Item1))
                {
                    string settingsJson = File.ReadAllText(fullPath.Item1);
                    var instancesDataClone = JsonConvert.DeserializeObject<MonitorPageSettings>(settingsJson);

                    foreach (var instanceData in instancesDataClone.InstancesData.Instances)
                    {
                        string ProjectAndInstanceName = RemoveSpaces(instancesDataClone.InstancesData.Project) +
                                                        RemoveSpaces(instanceData.Name);

                        var currentInstancesPackageFolderEnvName = ProjectAndInstanceName + "PackageFolder";
                        var packageFolderEnvValue = Environment.GetEnvironmentVariable(currentInstancesPackageFolderEnvName);
                        if (packageFolderEnvValue != null)
                        {
                            instanceData.PackageFolder = packageFolderEnvValue;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get PackageFolder from env of instance {name}, " +
                                               "dont set this instance", instanceData.Name);
                        }

                        var currentInstancesRootFolderEnvName = ProjectAndInstanceName + "RootFolder";
                        var rootFolderEnvValue = Environment.GetEnvironmentVariable(currentInstancesRootFolderEnvName);
                        if (rootFolderEnvValue != null)
                        {
                            instanceData.RootFolder = rootFolderEnvValue;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get RootFolder from env of instance {name}, " +
                                               "dont set this instance", instanceData.Name);
                        }
                    }

                    if (!string.IsNullOrEmpty(activeGroupsOverride))
						instancesDataClone.InstancesData.ActiveGroups = activeGroupsOverride;

					if (!string.IsNullOrEmpty(activeProjectOverride))
                        instancesDataClone.InstancesData.Project = activeProjectOverride;

					if (!string.IsNullOrEmpty(configurationOverride))
                        foreach (var instance in instancesDataClone.InstancesData.Instances)
							instance.Configuration = configurationOverride;

                    if (forceBlazorConfigurationFolderToPublish)
                    {
                        var blazorInstances = instancesDataClone.InstancesData.Instances.Where(x => !string.IsNullOrEmpty(x.CsProj) && x.CsProj.ToLowerInvariant().Contains(blazorCsprojKeyword)).ToList();
                        foreach (var blazorInstance in blazorInstances)
                            blazorInstance.Configuration = "Publish";
                    }

                    if (!string.IsNullOrEmpty(fullPath.Item2))
						foreach (var instance in instancesDataClone.InstancesData.Instances)
							instance.RootFolder = fullPath.Item2;

                    if (!string.IsNullOrEmpty(packageFolderOverride))
                        foreach (var instance in instancesDataClone.InstancesData.Instances)
                            instance.PackageFolder = packageFolderOverride;

					monitorPageSettings.Add(instancesDataClone);
				}
            }
            #endregion Load From Files

            #region Combine Tag Groups
            var combinedGroupTags = new UI_GroupTags();
            var groupsDict = new Dictionary<string, List<StringWrapper>>();

			for (int i = 0; i < monitorPageSettings.Count; i++)
            {
				for (int j = 0; j < monitorPageSettings[i].GroupTags.GroupTags.Count; j++)
                {
                    var groupT = monitorPageSettings[i].GroupTags.GroupTags[j];
                    if (!groupsDict.ContainsKey(groupT.Key))
                    {
                        groupsDict.Add(groupT.Key, new List<StringWrapper>());
                        
                        foreach (var value in groupT.Values)
                            groupsDict[groupT.Key].Add(value);
					}
                    else
                    {
						_logger.LogError($"TagsGroup With This Name Already Exists. Keeping The Previous Value Of {groupT.Key}: {string.Join(";",groupsDict[groupT.Key].Select(x=>$"{x.IsActive}:{x.Value}"))} and not the new one: {string.Join(";", groupT.Values.Select(x => $"{x.IsActive}:{x.Value}"))}");
					}

				}
			}

            foreach (var groupTa in groupsDict)
                combinedGroupTags.GroupTags.Add(new KeyListValue() { Key = groupTa.Key, Values = groupTa.Value });

			combinedSettings.GroupTags = combinedGroupTags;
			#endregion Combine Tag Groups

			#region Combine Vms
			var combinedVms = new UI_VmsData();

            var vmsToCombinePerKey = new Dictionary<string, List<UI_VmData>>();			

			for (int i = 0; i < monitorPageSettings.Count; i++)
            {
                for (int j = 0; j < monitorPageSettings[i].VmsData.VmsDataList.Count; j++)
                {
                    if (!vmsToCombinePerKey.ContainsKey(monitorPageSettings[i].VmsData.VmsDataList[j].GetIpAndPortKey()))
                        vmsToCombinePerKey.Add(monitorPageSettings[i].VmsData.VmsDataList[j].GetIpAndPortKey(), new List<UI_VmData>());

                    vmsToCombinePerKey[monitorPageSettings[i].VmsData.VmsDataList[j].GetIpAndPortKey()].Add(monitorPageSettings[i].VmsData.VmsDataList[j]);
				}
			}

			var vmsOverride = overrideVms.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToList();
			var vmsEachHasNameIpPort = new List<Tuple<string, string, string>>();
			foreach (var vmsText in vmsOverride)
			{
                var vmsSpt = vmsText.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
				vmsEachHasNameIpPort.Add(Tuple.Create(vmsSpt[0], vmsSpt[1], vmsSpt[2]));
            }

            int vmCounter = 0;

            var ipAndPortToVmUniqueName = new Dictionary<string, string>();
			
            foreach (var vmToCombine in vmsToCombinePerKey.Values)
            {
                var vmData = new UI_VmData();
				
				int vmCountToTake = 0;
				if (vmCounter > vmsEachHasNameIpPort.Count - 1)
					vmCountToTake = vmsEachHasNameIpPort.Count - 1;
				else
					vmCountToTake = vmCounter;

                var vmInf = vmsEachHasNameIpPort[vmCountToTake];

				vmData.UniqueName = vmInf.Item1; // $"Vm_{vmCounter++}";								
				vmData.IpAddress = vmInf.Item2; // vmToCombine[0].IpAddress;
				vmData.Port = vmInf.Item3; // vmToCombine[0].Port;

                if (!ipAndPortToVmUniqueName.ContainsKey(vmToCombine[0].GetIpAndPortKey()))
                    ipAndPortToVmUniqueName.Add(vmToCombine[0].GetIpAndPortKey(), vmData.UniqueName);

                vmCounter++;

                var vmDataVars = new Dictionary<string, string>();

                for (int i = 0; i < vmToCombine.Count; i++)
                {
                    foreach (var envVar in vmToCombine[i].ExtraVariables)
                    {
                        if (!vmDataVars.ContainsKey(envVar.Key))
                        {
                            vmDataVars.Add(envVar.Key, envVar.Value);
                        }
                        else
                        {
                            _logger.LogError($"Env Var With This Name On This Machine Already Exists. Keeping The Previous Value Of {envVar.Key}: {vmDataVars[envVar.Key]} and not the new one: {envVar.Value}");
                        }

                    }
                }

                foreach (var kvp in vmDataVars)
                    vmData.ExtraVariables.Add(new KeyValueComplex() { Key = kvp.Key, Value = kvp.Value });

                var vmDataCompilers = new Dictionary<string, string>();

                for (int i = 0; i < vmToCombine.Count; i++)
                {
                    foreach (var compiler in vmToCombine[i].Compilers)
                    {
                        if (!vmDataCompilers.ContainsKey(compiler.CompilerName))
                        {
                            vmDataCompilers.Add(compiler.CompilerName, compiler.CompilerPath);
                        }
                        else
                        {
                            _logger.LogError($"Compiler With This Name On This Machine Already Exists. Keeping The Previous Value Of {compiler.CompilerName}: {vmDataCompilers[compiler.CompilerName]} and not the new one: {compiler.CompilerPath}");
                        }

                    }
                }

                foreach (var compiler in vmDataCompilers)
                    vmData.Compilers.Add(new UI_VmCompiler() { CompilerName = compiler.Key, CompilerPath = compiler.Value});

				combinedVms.VmsDataList.Add(vmData);
			}

            var vmUniqueNames = new HashSet<string>();

            for (int i=0; i<combinedVms.VmsDataList.Count; i++)
            {                
                if (!vmUniqueNames.Contains(combinedVms.VmsDataList[i].UniqueName))
                {
                    vmUniqueNames.Add(combinedVms.VmsDataList[i].UniqueName);
                }
                else
                {
                    combinedVms.VmsDataList[i] = null;
                }
            }

            combinedVms.VmsDataList.RemoveAll(x => x == null);

            combinedSettings.VmsData = combinedVms;
            #endregion Combine Vms

            #region Combine Instances
            var combinedInstances = new UI_InstancesData();

            var project = monitorPageSettings.Select(x => x.InstancesData.Project).Distinct().ToList();
            if (project.Count > 1)
                throw new Exception($"Cannot Mix Between Projects. {string.Join(",", project)}");

            combinedInstances.Project = project[0];

			var groups = monitorPageSettings.Select(x => x.InstancesData.ActiveGroups).SelectMany(y => y.Split(";")).Where(z => !string.IsNullOrEmpty(z)).Distinct().ToList();
			combinedInstances.ActiveGroups = string.Join(";", groups);

			int appsCounter = 1;

			for (int i = 0; i < monitorPageSettings.Count; i++)
            {
                for (int j = 0; j < monitorPageSettings[i].InstancesData.Instances.Count; j++)
                {
                    var vmMap = monitorPageSettings[i].VmsData.VmsDataList.ToDictionary(x => x.UniqueName, x => x);
                    var vmName = ipAndPortToVmUniqueName[vmMap[monitorPageSettings[i].InstancesData.Instances[j].VmUniqueName].GetIpAndPortKey()];
					monitorPageSettings[i].InstancesData.Instances[j].VmUniqueName = vmName;

					monitorPageSettings[i].InstancesData.Instances[j].Id = appsCounter++;
					combinedInstances.Instances.Add(monitorPageSettings[i].InstancesData.Instances[j]);
				}
            }

			combinedSettings.InstancesData = combinedInstances;
            combinedSettings.CurrentFileName = "autoGeneratedFromManyFiles";
            #endregion Combine Instances

            combinedSettings.InstancesData.UpdateDisableStateByGroups();

			foreach (var ins in combinedSettings.InstancesData.Instances)
			{
                if (!ins.Disabled && !ins.DisabledByGroups)
				    ins.RunOrStop = appsStartState;

				ins.RunningFor = new TimeSpan();
				ins.IsRunning = "Unknown";
				ins.ProcessName = "";
				ins.ProcessId = null;
			}

			return combinedSettings;
		}

        private string RemoveSpaces(string str)
        {
            return str.Replace(" ", "");
        }
		private void LoadLastConfiguration()
		{
			_logger.LogInformation("Loading Last Configuration file: {LastConfigurationFile}", lastConfigFileName);
			if (!File.Exists(lastConfigFileName))
			{
				_logger.LogError("Last Configuration file doesn't exist: {LastConfigurationFile}", lastConfigFileName);
				return;
			}

			string fileName = File.ReadAllText(lastConfigFileName);
			string settingsName = Path.GetFileNameWithoutExtension(fileName);

			MonitorPageSettings userSettings = GetSettingsFromUserSettings(settingsName);

			foreach (var ins in userSettings.InstancesData.Instances)
			{
				ins.RunningFor = new TimeSpan();
				ins.IsRunning = "Unknown";
				ins.ProcessName = "";
				ins.ProcessId = null;
			}

			lastAppliedSettings = userSettings.Clone();
		}

		#region IMonitorHandlingAgents
		#region Event Driven Triggers
		public Action<IpAddress, CompileInfo> SetCompilerRequestHandler { get; set; }
		public Action<IpAddress, MonitorAgentSettings> SetMonitorAgentSettingsHandler { get; set; }
		public Func<IpAddress, Tuple<VmInfo, List<ProcessInstanceInfo>>> GetVmInfoAndListProcessInstaceInfoHandler { get; set; }
        public Action<IpAddress, GenericCommand> GenericCommandHandler { get; set; }
		#endregion Event Driven Triggers

        public Tuple<VmInfo, List<ProcessInstanceInfo>> GetVmInfoAndListProcessInstaceInfo(IpAddress ipAddress)
        {
            return GetVmInfoAndListProcessInstaceInfoHandler?.Invoke(ipAddress);
		}

        public Dictionary<string, Tuple<VmInfo, Dictionary<int, ProcessInstanceInfo>>> GetAgentsPeriodicInfos()
        {
            return agentsPeriodicInfos;
		}

		private Dictionary<string, Tuple<VmInfo, Dictionary<int, ProcessInstanceInfo>>> agentsPeriodicInfos = new Dictionary<string, Tuple<VmInfo, Dictionary<int, ProcessInstanceInfo>>>();

		private void UpdateAgentsPeriodicInfosWrapper(object obj)
		{
			try
			{
				UpdateAgentsPeriodicInfos();
			}
			catch { }
			finally
			{
				updateTimer.Change(fetchInfoFromAgentsTimerTimeInterval_mSec, Timeout.Infinite);
			}
		}

		public void UpdateAgentsPeriodicInfos()
        {
            var lAppliedSettings = GetCurrentSettings();

			var infos = new Dictionary<string, Tuple<VmInfo, Dictionary<int, ProcessInstanceInfo>>>();

			bool syncNeeded = false;

			foreach (var vm in lAppliedSettings.VmsData.VmsDataList.Select(x => new { vmName = x.UniqueName, ipAdd = new IpAddress() { Ip = x.IpAddress, Port = x.Port } }))
            {
                try
                {
					var v = GetVmInfoAndListProcessInstaceInfo(vm.ipAdd);
					if (v.Item1.LastSetGuid != lastGuidString)
						syncNeeded = true;

					var vv = Tuple.Create(v.Item1, v.Item2.Where(x => x.Id.HasValue).ToDictionary(x => x.Id.Value, x => x));
					infos.TryAdd(vm.vmName, vv);
				}
				catch (Exception ex)
				{
                    _logger.LogInformation(ex.ToString());
				}
			}

            agentsPeriodicInfos = infos;

			if (syncNeeded)
				SetAllAgentsSettings(lastAppliedSettings);
		}


		public void SetMonitorAgentSettings(IpAddress ipAddress, MonitorAgentSettings monitorAgentSettings)
		{
			SetMonitorAgentSettingsHandler?.Invoke(ipAddress, monitorAgentSettings);
		}
		#endregion IMonitorHandlingAgents

		public void SetCompilerRequest(IpAddress ipAddress, CompileInfo compileInfo)
        {
            Task.Run(
                () =>
                {
					SetCompilerRequestHandler?.Invoke(ipAddress, compileInfo);
                });
		}

        public void GenericCommand(IpAddress ipAddress, GenericCommand genericCommand)
        {
            Task.Run(() =>
                {
                    GenericCommandHandler?.Invoke(ipAddress, genericCommand);
                });
        }

		private const string lastConfigFileName = "lastConfigFileName.json";
		private const string presetsFolder = "Presets";
		private const string projectsFilePath = "Projects/ProjectsNames.json";
		private const string userSettingsFolder = "UserSettings";

        public string UserFileClosetAvailableName(string presetName)
        {			
            var availabeFileName = string.Empty;

			int i = 0;
			while (true)
			{
				var potentialFile = Path.Combine(userSettingsFolder, $"{presetName}_{i}.json");
				if (!File.Exists(potentialFile))
				{
					availabeFileName = Path.GetFileNameWithoutExtension(potentialFile);
					break;
				}
				i++;
			}

            return availabeFileName;
		}

		public MonitorPageSettings GetSettingsFromPresetsSettings(string presetName)
        {
            var file = Path.Combine(presetsFolder, presetName) + ".json";

            return GetSettingsFromFullPath(file);

		}
		public MonitorPageSettings GetSettingsFromUserSettings(string userSettingsName)
        {
			string file = Path.Combine(userSettingsFolder, userSettingsName) + ".json";
			
			_logger.LogInformation("Loading settings from {UserSettingsName}", file);

			return GetSettingsFromFullPath(file);
		}

		private static MonitorPageSettings GetSettingsFromFullPath(string path)
        {
	        string fullPath = Path.GetFullPath(path);
	        if (!File.Exists(fullPath))
	        {
		        throw new FileNotFoundException($"File {fullPath} doesn't exist");
	        }
			string settingsJson = File.ReadAllText(path);
			var instancesDataClone = JsonConvert.DeserializeObject<MonitorPageSettings>(settingsJson);

            return instancesDataClone;
		}

		public List<string> GetPresetsSettingsList()
        {
			var files = Directory.GetFiles(presetsFolder, "*.json");
			return new (files.ToList().Select(x => Path.GetFileNameWithoutExtension(x)));
		}

		public List<string> GetPossibleProjectsList()
        {
            var projects = new List<string>();

            try
            {
                projects = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(presetsFolder, projectsFilePath)));
            }
            catch (Exception ex) 
            {
                _logger.LogInformation(ex.ToString());                
            }

            return projects;
        }

        public void KillAll()
        {
            try
            {				
				foreach (var app in lastAppliedSettings.InstancesData.Instances)
                    if (!app.DisabledByGroups && !app.Disabled)
                        app.RunOrStop = false;

				SetAllAgentsSettings(lastAppliedSettings, false, true);

			}
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
		}

		public void StartAll()
		{
			try
			{
				foreach (var app in lastAppliedSettings.InstancesData.Instances)
					if (!app.DisabledByGroups && !app.Disabled)
						app.RunOrStop = true;

				SetAllAgentsSettings(lastAppliedSettings, false, true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.ToString());
			}
		}

        public List<string> GetAllDeployedFoldersToCopy()
        {
            var applicationPath = new List<string>();

            try
            {
                var monitorAgentObjects =
                    CreateMonitorAgentObjects(
						lastAppliedSettings.GlobalVariables,
						lastAppliedSettings.GroupTags,
                        lastAppliedSettings.VmsData,
                        lastAppliedSettings.InstancesData);

                var onlyAppsThatHadOnlyOneInstanceInTheOriginalTable = new List<string>();

                var appsDictionary = new Dictionary<string, int>();
				var apps = monitorAgentObjects.SelectMany(y => y.Value.Item2.ProcessInstancesSettings.Select(x => x.ApplicationFileName)).ToList();
				foreach (var app in apps)
				{
					if (!appsDictionary.ContainsKey(app))
                        appsDictionary.Add(app, 0);

					appsDictionary[app]++;
                }

				foreach (var app in appsDictionary)
				{
					if (app.Value == 1)
						onlyAppsThatHadOnlyOneInstanceInTheOriginalTable.Add(app.Key);

                }

                foreach (var monitorAgentObject in monitorAgentObjects)
                {
                    foreach (var app in monitorAgentObject.Value.Item2.ProcessInstancesSettings)
                    {
                        if (string.IsNullOrEmpty(app.CsProj) && onlyAppsThatHadOnlyOneInstanceInTheOriginalTable.Contains(app.ApplicationFileName))
                            applicationPath.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.ApplicationPath)).Replace(UI_InstancesData.ProjectTemplateString, lastAppliedSettings.InstancesData.Project));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return applicationPath.Distinct().ToList();
        }

        public const string blazorCsprojKeyword = "blazor";

		public List<string> GetAllCsProjForPublish()
		{
            var csproj = new List<string>();

            try
            {
                var monitorAgentObjects =
                    CreateMonitorAgentObjects(
						lastAppliedSettings.GlobalVariables,
						lastAppliedSettings.GroupTags,
                        lastAppliedSettings.VmsData,
                        lastAppliedSettings.InstancesData);

                foreach (var monitorAgentObject in monitorAgentObjects)
                {
                    foreach (var app in monitorAgentObject.Value.Item2.ProcessInstancesSettings)
                    {
                        if (!string.IsNullOrEmpty(app.CsProj))
                        {
                            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.CsProj)).Replace(UI_InstancesData.ProjectTemplateString, lastAppliedSettings.InstancesData.Project);
                            if (path.ToLowerInvariant().Contains(blazorCsprojKeyword))                            
                                csproj.Add(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return csproj.Distinct().ToList();
        }
        public List<string> GetAllCsprojs()
        {
            var csproj = new List<string>();

            try
            {
                var monitorAgentObjects =
                    CreateMonitorAgentObjects(
						lastAppliedSettings.GlobalVariables,
						lastAppliedSettings.GroupTags,
                        lastAppliedSettings.VmsData,
                        lastAppliedSettings.InstancesData);

                foreach (var monitorAgentObject in monitorAgentObjects)
                {
                    foreach (var app in monitorAgentObject.Value.Item2.ProcessInstancesSettings)
                    {
                        if (!string.IsNullOrEmpty(app.CsProj))
                        {
                            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.CsProj)).Replace(UI_InstancesData.ProjectTemplateString, lastAppliedSettings.InstancesData.Project);
                            csproj.Add(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return csproj.Distinct().ToList();
        }

        public List<string> GetAllCsprojForBuild()
        {
            var csproj = new List<string>();

			try
			{
				var monitorAgentObjects = 
                    CreateMonitorAgentObjects(
						lastAppliedSettings.GlobalVariables,
						lastAppliedSettings.GroupTags, 
                        lastAppliedSettings.VmsData, 
                        lastAppliedSettings.InstancesData);

                foreach (var monitorAgentObject in monitorAgentObjects)
                {
                    foreach (var app in monitorAgentObject.Value.Item2.ProcessInstancesSettings)
                    {
                        if (!string.IsNullOrEmpty(app.CsProj))
                        { 
                            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.CsProj)).Replace(UI_InstancesData.ProjectTemplateString, lastAppliedSettings.InstancesData.Project);
                            if (!path.ToLowerInvariant().Contains(blazorCsprojKeyword))
                            csproj.Add(path);
                        }
                    }
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.ToString());
			}

            return csproj.Distinct().ToList();
		}

		private object locker = new object();

        public event Action<MonitorPageSettings> SettingsChangedFromRestApi;

		private string lastGuidString = Guid.NewGuid().ToString();

		public void SetAllAgentsSettings(
			MonitorPageSettings monitorPageSettings, bool dontSave = false, bool settingsChangedFromRestApi = false)
        {
            lock (locker)
            {
				lastGuidString = Guid.NewGuid().ToString();

				if (!dontSave)
                {
                    if (!Directory.Exists(userSettingsFolder))
                        Directory.CreateDirectory(userSettingsFolder);

                    File.WriteAllText(Path.Combine(userSettingsFolder, monitorPageSettings.CurrentFileName + ".json"), JsonConvert.SerializeObject(monitorPageSettings, Formatting.Indented));
                    File.WriteAllText(lastConfigFileName, monitorPageSettings.CurrentFileName + ".json");
                }

                var monitorAgentObjects = CreateMonitorAgentObjects(monitorPageSettings.GlobalVariables, monitorPageSettings.GroupTags, monitorPageSettings.VmsData, monitorPageSettings.InstancesData);

                Task.Run(
                    () =>
                    {
                        foreach (var agentObject in monitorAgentObjects)
                        {
							agentObject.Value.Item2.VmInstanceSettings.GuidString = lastGuidString;
							SetMonitorAgentSettingsHandler?.Invoke(agentObject.Value.Item1, agentObject.Value.Item2);
                        }
                    });

                lastMonitorAgentSettings = monitorAgentObjects;
                lastAppliedSettings = monitorPageSettings;

				if (settingsChangedFromRestApi)
				{
					Task.Run(() =>
					{						
							SettingsChangedFromRestApi?.Invoke(lastAppliedSettings.Clone());
					});
				}
			}
		}

        public Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>> GetCurrentSettingsInAgentsFormat()
        {
            return lastMonitorAgentSettings;
		}

        private Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>> lastMonitorAgentSettings = new Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>>();
		private MonitorPageSettings lastAppliedSettings = new MonitorPageSettings();
		private readonly ILogger<MonitorAgentService> _logger;

		public MonitorPageSettings GetCurrentSettings()
        {
            return lastAppliedSettings;			
        }

		private Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>> CreateMonitorAgentObjects(
			UI_GlobalVariables globalVariablesClone,
			UI_GroupTags groupTagsClone,
            UI_VmsData vmsDataClone,
            UI_InstancesData instancesDataClone)
        {
            #region Combine Inherit Tags
            var groupDict = groupTagsClone.GroupTags.ToDictionary(x => x.Key, x => x.Values.Where(x => x.IsActive).Select(x => x.Value).ToList());
            foreach (var key in groupDict.Keys.ToList())
                if (groupDict[key].Count == 0)
                    groupDict.Remove(key);

            var instanceByIdTags = new Dictionary<int, List<string>>();

            foreach (var instance in instancesDataClone.Instances)
            {
				if (!instanceByIdTags.ContainsKey(instance.Id))
					instanceByIdTags.Add(instance.Id, new List<string>());

                if (!string.IsNullOrEmpty(instancesDataClone.Project))
                    instanceByIdTags[instance.Id].Add(instancesDataClone.Project.ToLowerInvariant());

				instanceByIdTags[instance.Id].AddRange(instance.TagsStr.Split(";").ToList());

				foreach (var inheritTagsGroup in instance.InheritTagsFromGroup)
                {
                    if (inheritTagsGroup.IsActive)
                    {
                        if (groupDict.TryGetValue(inheritTagsGroup.Value, out List<string> values))
                        {
                            foreach (var val in values)
                            {
                                instanceByIdTags[instance.Id].Add(val);
                            }
                        }
                    }
                }
				instanceByIdTags[instance.Id] = instanceByIdTags[instance.Id].Distinct().ToList();
            }
			#endregion Combine Inherit Tags


			#region Add Environment Variables Of All Service IPs
			var parametersTreeInitiator = new ParametersTreeInitiator();

			var tree = parametersTreeInitiator.Create(
                globalVariablesClone, 
                vmsDataClone,
				instancesDataClone);

            // Add Here To Global Parameters (The Right Place Is Session Parameters But It Is Not Exist Yet)            


			ParametersTreeInitiator.ResolveAllParameters(tree);
			var mapService = ParametersTreeInitiator.BuildResolvedMapPerService(tree);

			var instanceByIdEnvVars = new Dictionary<int, List<KeyValue>>();

            foreach (var mapped in mapService)
                instanceByIdEnvVars[mapped.Key.Id] = mapped.Value.Select(x => new KeyValue() { Key = x.Key, Value = x.ResolvedValue }).ToList();

			#endregion Add Environment Variables Of All Service IPs

			//#region Add Environment Variables Of All Service IPs
			//
			//var instanceByIdEnvVars = new Dictionary<int, List<KeyValue>>();
			//foreach (var instance in instancesDataClone.Instances)
			//    if (!instanceByIdEnvVars.ContainsKey(instance.Id))
			//		instanceByIdEnvVars.Add(instance.Id, new List<KeyValue>());
			//
			//var servicesNameAndAddress = new List<Tuple<int, string, string, string, bool>>();
			//var vmsByName = vmsDataClone.VmsDataList.ToDictionary(x => x.UniqueName, x => x);
			//foreach (var instance in instancesDataClone.Instances)
			//{
			//    if (vmsByName.ContainsKey(instance.VmUniqueName) && (!(instance.Disabled || instance.DisabledByGroups)))
			//        servicesNameAndAddress.Add(Tuple.Create(instance.Id, instance.RestApiPort, instance.Name, vmsByName[instance.VmUniqueName].IpAddress, instance.SupportProberMonitor));
			//}
			//
			//foreach (var instance in instancesDataClone.Instances)
			//{
			//	instanceByIdEnvVars[instance.Id].AddRange(instance.ExtraVariables.Select(x => new KeyValue() { Key = x.Key, Value = x.Value }));
			//
			//    if (!string.IsNullOrEmpty(instance.Name))
			//        instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = "Services:MyService:Name", Value = instance.Name });
			//
			//    if (!string.IsNullOrEmpty(instance.InstanceId))
			//		instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = "Services:MyService:InstanceId", Value = instance.InstanceId });
			//
			//    if (!string.IsNullOrEmpty(instance.RestApiPort))
			//    {                    
			//        instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = $"Services:MyService:RestApiPort", Value = instance.RestApiPort });
			//
			//        _logger.LogInformation($"{instance.Id}: {instanceByIdEnvVars[instance.Id][instanceByIdEnvVars[instance.Id].Count - 1].Key} = {instanceByIdEnvVars[instance.Id][instanceByIdEnvVars[instance.Id].Count - 1].Value}");
			//	}
			//
			//	foreach (var serviceNameAndAddress in servicesNameAndAddress)
			//    {                    
			//        instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = $"Services:{serviceNameAndAddress.Item3}:Ip", Value = serviceNameAndAddress.Item4 });
			//        if (!string.IsNullOrEmpty(serviceNameAndAddress.Item2))
			//        {
			//            instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = $"Services:{serviceNameAndAddress.Item3}:RestApiPort", Value = serviceNameAndAddress.Item2 });
			//			instanceByIdEnvVars[instance.Id].Add(new KeyValue() { Key = $"Services:{serviceNameAndAddress.Item3}:SupportProberMonitor", Value = serviceNameAndAddress.Item5 ? "true" : "false" });
			//		}
			//	}
			//}
			//
			//#endregion Add Environment Variables Of All Service IPs

			var monitorAgentObjects = new Dictionary<string, Tuple<IpAddress, MonitorAgentSettings>>();

            foreach (var instance in instancesDataClone.Instances)
            {
                var instVmUniqueName = instance.VmUniqueName;
                if (!monitorAgentObjects.ContainsKey(instVmUniqueName))
                {
                    var vmData = vmsDataClone.VmsDataList.FirstOrDefault(x => x.UniqueName == instVmUniqueName);
                    if (vmData != null)
                    {
                        var monitorAgentSettings = new MonitorAgentSettings();

						monitorAgentObjects.Add(instVmUniqueName, Tuple.Create(
                            new IpAddress() { Ip = vmData.IpAddress, Port = vmData.Port }, monitorAgentSettings));


                        foreach (var compiler in vmData.Compilers)
                            monitorAgentSettings.VmInstanceSettings.CompileTool.Add(Tuple.Create(compiler.CompilerName, compiler.CompilerPath));

						//foreach (var extraVariables in vmData.ExtraVariables)
						//	monitorAgentSettings.VmInstanceSettings.Variables.Add(Tuple.Create(extraVariables.Key, extraVariables.Value));
					}
                }

                var arguments = instance.Arguments;
                if (instance.SupportTags)
                {
                    if (instanceByIdTags.ContainsKey(instance.Id))
                        if (instanceByIdTags[instance.Id].Count > 0)
							arguments += $" -tags {string.Join(";", instanceByIdTags[instance.Id])};";
				}

                if (!instance.Disabled && !instance.DisabledByGroups)
                {
                    List<Tuple<string, string>> extraVariables = new List<Tuple<string, string>>();

					if (instanceByIdEnvVars.ContainsKey(instance.Id))
                        extraVariables = instanceByIdEnvVars[instance.Id].Select(x => Tuple.Create(x.Key, x.Value)).ToList();

					monitorAgentObjects[instVmUniqueName].Item2.ProcessInstancesSettings.Add(new ProcessInstanceSettings()
                    {
                        Id = instance.Id,
                        Name = instance.Name,
                        VmUniqueName = instVmUniqueName,
                        ApplicationFileName = instance.ApplicationFileName,
                        ApplicationPath = instance.ApplicationPath,
                        ApplicationWorkingDirectory = instance.ApplicationWorkingDirectory,
						RestApiPort = instance.RestApiPort,                        
						InstanceId = instance.InstanceId,
						Arguments = arguments,
                        CsProj = instance.CsProj,
                        Variables = extraVariables,
					});

                    monitorAgentObjects[instVmUniqueName].Item2.ProcessInstancesRunStateSettings.Add(new ProcessInstanceRunStateSettings(instance.Id)
                    {
                        RunState = instance.RunOrStop ? RunStateEnum.Run : RunStateEnum.Stop,
                    });
                }
			}

			foreach (var vmSettings in monitorAgentObjects.Values)
			{
				foreach (var inst in vmSettings.Item2.ProcessInstancesSettings)
				{
					if (instancesDataClone.Project != null)
					{
						inst.ApplicationWorkingDirectory = inst.ApplicationWorkingDirectory.Replace(UI_InstancesData.ProjectTemplateString, instancesDataClone.Project);
						inst.ApplicationWorkingDirectory = inst.ApplicationWorkingDirectory.Replace(UI_InstancesData.ConfigurationTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).Configuration);
						inst.ApplicationWorkingDirectory = inst.ApplicationWorkingDirectory.Replace(UI_InstancesData.RootFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).RootFolder);
						inst.ApplicationWorkingDirectory = inst.ApplicationWorkingDirectory.Replace(UI_InstancesData.packageFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).PackageFolder);

						inst.ApplicationPath = inst.ApplicationPath.Replace(UI_InstancesData.ProjectTemplateString, instancesDataClone.Project);
						inst.ApplicationPath = inst.ApplicationPath.Replace(UI_InstancesData.ConfigurationTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).Configuration);
						inst.ApplicationPath = inst.ApplicationPath.Replace(UI_InstancesData.RootFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).RootFolder);
						inst.ApplicationPath = inst.ApplicationPath.Replace(UI_InstancesData.packageFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).PackageFolder);

						inst.ApplicationFileName = inst.ApplicationFileName.Replace(UI_InstancesData.ProjectTemplateString, instancesDataClone.Project);

						inst.CsProj = inst.CsProj.Replace(UI_InstancesData.ProjectTemplateString, instancesDataClone.Project);
						inst.CsProj = inst.CsProj.Replace(UI_InstancesData.RootFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).RootFolder);
						inst.CsProj = inst.CsProj.Replace(UI_InstancesData.packageFolderTemplateString, instancesDataClone.Instances.First(x => x.Id == inst.Id).PackageFolder);
					}
				}
			}

			return monitorAgentObjects;
		}


        #region Load & Save Operations

        #endregion Load & Save Operations

    }
}