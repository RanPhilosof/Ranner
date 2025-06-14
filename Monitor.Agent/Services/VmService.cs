using AppMonitoring.SharedTypes;
using CommandLine;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Monitor.Infra;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace Monitor.Agent.Services
{
    public class MonitorAgentOptions
    {
        [Option('l', "lastSettings", Required = false, HelpText = "Is To Start With Last Settings")]
        public bool IsToStartWithLastSettings { get; set; } = false;

        [Option('c', "configurationSettings", Required = false, HelpText = "Configuration File")]
        public string ConfigurationFile { get; set; } = string.Empty;
		[Option('p', "port", Required = false, HelpText = "Port")]
		public string Port { get; set; } = string.Empty;
		[Option('z', "loggerFilePath", Required = false, HelpText = "File Path To Write The Logs")]
		public string LoggerFilePath { get; set; } = string.Empty;

		public override string ToString()
        {
            return $"IsToStartWithLastSettings: {IsToStartWithLastSettings}, ConfigurationFile: {ConfigurationFile}, Port: {Port}, LoggerFilePath: {LoggerFilePath}";
        }
    }

    public class VmService : IMonitorAgentService
    {
        private const string settingsFile = "lastSettings.json";

        #region C'tors
        public VmService(ILogger<VmService> logger)
        {
            _logger = logger;            

            try
            {
                ParserResult<MonitorAgentOptions>? parsedArgs = Parser.Default.ParseArguments<MonitorAgentOptions>(Environment.GetCommandLineArgs());
                _logger.LogInformation("Parsed arguments are :{ParsedArgs} ", parsedArgs.Value);
                string localSettingsFile = !string.IsNullOrEmpty(parsedArgs.Value.ConfigurationFile) ? parsedArgs.Value.ConfigurationFile : parsedArgs.Value.IsToStartWithLastSettings ? settingsFile : string.Empty;

                if (!string.IsNullOrEmpty(localSettingsFile))
                { 
                    string json = File.ReadAllText(localSettingsFile);
                    var settings = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);
                    newMonitorAgentSettings = settings;
				}
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
            }

            aboutTime = true;
            timer = new Timer(new TimerCallback(OneLoopCycleWrapper), null, 10_000, Timeout.Infinite);
        }
        #endregion C'tors

        #region Fields
        private Timer timer;

        private Microsoft.Extensions.Logging.ILogger _logger;

        private ConcurrentQueue<Action> jobs = new ConcurrentQueue<Action>();

        private MonitorAgentSettings newMonitorAgentSettings = new MonitorAgentSettings();
        private MonitorAgentSettings currentMonitorAgentSettings = new MonitorAgentSettings();

        private VmInfo _vmInfo = new VmInfo();
        private List<ProcessInstanceInfo> _processInstancesInfo = new List<ProcessInstanceInfo>();

        private VmInfo _vmInfoCopy = new VmInfo();
        private List<ProcessInstanceInfo> _processInstancesInfoCopy = new List<ProcessInstanceInfo>();

        #endregion Fields

        #region Interface IMonitorAgentService
        public MonitorAgentSettings GetMonitorAgentSettings()
        {
            return currentMonitorAgentSettings;
        }
        public Tuple<VmInfo, List<ProcessInstanceInfo>> GetVmInfoAndListProcessInstaceInfo()
        {
            var local_vmInfoCopy = _vmInfoCopy;
            local_vmInfoCopy.LastSetGuid = lastGuid;

			return Tuple.Create(local_vmInfoCopy, _processInstancesInfoCopy);
        }

        private string lastGuid = Guid.NewGuid().ToString();

        public void InvokeNewSettings(MonitorAgentSettings monitorAgentSettings)
        {
            if (monitorAgentSettings != null && monitorAgentSettings.VmInstanceSettings != null && monitorAgentSettings.VmInstanceSettings.GuidString != null)
				lastGuid = monitorAgentSettings.VmInstanceSettings.GuidString;

			lock (jobs)
            {
                jobs.Enqueue(() =>
                {
                    newMonitorAgentSettings = monitorAgentSettings;
                });
            }

            lock (timer)
            {
                aboutTime = true;
                timer.Change(100, Timeout.Infinite);
            }

		}

		public void InvokeKillAll()
		{
            lock (jobs)
            {
                jobs.Enqueue(() =>
                {
                    if (currentMonitorAgentSettings != null)
                    {
                        var json = JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented);
                        var clone = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);

                        clone.ProcessInstancesRunStateSettings.ForEach(x => x.RunState = RunStateEnum.Stop);

                        newMonitorAgentSettings = clone;
                    }
                });
            }

            lock (timer)
            {
                aboutTime = true;
				timer.Change(100, Timeout.Infinite);
            }
		}

		public void InvokeGenericCommand(GenericCommand genericCommand)
        {
            if (genericCommand.Command.ToLower() == "kill")
            {
                lock (jobs)
                {
                    // Shutdown Agent
                    _logger.LogInformation($"Shutdown Agent Added To Jobs");					
					jobs.Enqueue(
                        () =>
                        {
                            Process.GetCurrentProcess().Kill();
                        });
                }
            }
        }


		public void InvokeCompile(int id, string configuration)
        {
            lock (jobs)
            {
                object originalState = null;

                // Shutdown Process
                jobs.Enqueue(
                    () =>
                    {
						if (currentMonitorAgentSettings != null)
						{
							var json = JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented);
							var clone = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);

                            var inst = clone.ProcessInstancesRunStateSettings.FirstOrDefault(x => x.Id == id);
                            if (inst != null)
                            {
								originalState = inst.RunState;
								inst.RunState = RunStateEnum.Stop;								
							}

							newMonitorAgentSettings = clone;
						}

                        CompilingInfo.InCompile.AddOrUpdate(id, true, (id, boolean) => true);
					}
                    );

				// Compile
				jobs.Enqueue(
	                () =>
	                {
                        _logger.LogInformation("!!!!!!!!!!!!!!!! Start Compiling !!!!!!!!!!!!!!!!!!");
		                if (currentMonitorAgentSettings != null)
		                {
							//_logger.LogInformation("currentMonitorAgentSettings != null");
							var json = JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented);
			                var clone = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);

							//_logger.LogInformation($"Instances Count: {clone.ProcessInstancesSettings.Count}");

							var inst = clone.ProcessInstancesSettings.FirstOrDefault(x => x.Id == id);

			                if (inst != null)
			                {
								//_logger.LogInformation("inst != null");
								var compilerInfo = clone.VmInstanceSettings.CompileTool.FirstOrDefault();
                                if (compilerInfo != null)
                                {
									//_logger.LogInformation("compilerInfo != null");

									var processInfo = ProcessHelper.StartAppInstance(
                                        compilerInfo.Item1,                                        
                                        "", //Path.GetDirectoryName(compilerInfo.Item2),										
										compilerInfo.Item2, //Path.GetFileName()
                                        Directory.GetCurrentDirectory(),
										//Path.GetDirectoryName(compilerInfo.Item2),
										configuration.ToLowerInvariant().Contains("publish") ?
										$"publish \"{inst.CsProj}\" -c RELEASE" :
										$"build \"{inst.CsProj}\" -c {configuration.ToUpper()}",
                                        new List<Tuple<string, string>>(),
                                        _logger,
                                        true);

                                    if (processInfo != null && processInfo.ProcessInstance != null)
                                    {										
										processInfo.ProcessInstance.WaitForExit();

                                        //Console.WriteLine($"Complie Process: {processInfo.ProcessInstance.HasExited}");
                                        //Thread.Sleep(15_000);
                                    }
                                    else
                                    {
                                        //Console.WriteLine($"Complie Process Ended Very Fast.");

									}
								}
			                }
		                }
	                });

				// Return To Original Running State
				jobs.Enqueue(
                    () =>
                    {
						if (currentMonitorAgentSettings != null)
						{
							var json = JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented);
							var clone = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);

                            var inst = clone.ProcessInstancesRunStateSettings.FirstOrDefault(x => x.Id == id);
                            if (originalState != null && inst != null)
                            {
								inst.RunState = (RunStateEnum) originalState;
							}

							newMonitorAgentSettings = clone;
						}

						CompilingInfo.InCompile.AddOrUpdate(id, false, (id, boolean) => false);
					}
                    );
            }

			lock (timer)
			{
				aboutTime = true;
				timer.Change(100, Timeout.Infinite);
			}
		}


		public void InvokeStartAll()
		{
			jobs.Enqueue(() =>
			{
				if (currentMonitorAgentSettings != null)
				{
					var json = JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented);
					var clone = JsonConvert.DeserializeObject<MonitorAgentSettings>(json);

					clone.ProcessInstancesRunStateSettings.ForEach(x => x.RunState = RunStateEnum.Run);

					newMonitorAgentSettings = clone;
				}
			});

            lock (timer)
            {
                aboutTime = true;
                timer.Change(100, Timeout.Infinite);
            }
		}
        #endregion Interface IMonitorAgentService

        #region Private Methods - Main Loop
        private object oneLoopCycleLocker = new object();
        private bool aboutTime = false;

		private void OneLoopCycleWrapper(object obj)
        {
            lock (oneLoopCycleLocker)
            {
                try
                {
                    while (jobs.Count > 0 || aboutTime)
                    {
                        OneLoopCycle();
                        aboutTime = false;
					 }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                finally
                {
                    lock (timer)
                    {
                        aboutTime = true;
                        timer.Change(10_000, Timeout.Infinite);
                    }
                }
            }
        }

        private void OneLoopCycle()
        {
            DoOneWaitingJob();

			ChangesInstructions(
                out bool vmInstanceSettingsUpdated,
                out bool processInstancesSettingsUpdated,
                out bool processInstancesRunStateSettingsUpdated);
            
            if (vmInstanceSettingsUpdated || processInstancesSettingsUpdated)
            {                
                _logger.LogInformation("Settings Changed. About to kill all apps from old settings ...");

                KillAllAndClearMonitoredData(currentMonitorAgentSettings);

                // Apply new settings
                currentMonitorAgentSettings = newMonitorAgentSettings;

                // Set/Override New Env Params
                //foreach (var envVar in currentMonitorAgentSettings.VmInstanceSettings.Variables)
                //    Environment.SetEnvironmentVariable(envVar.Item1, envVar.Item2, EnvironmentVariableTarget.User);

                try
                {
                    File.WriteAllText(settingsFile, JsonConvert.SerializeObject(currentMonitorAgentSettings, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
            else
            {
                if (processInstancesRunStateSettingsUpdated)
                {
                    currentMonitorAgentSettings = newMonitorAgentSettings;
                    _logger.LogInformation("Settings app states changed. About to start / Kill needed apps ...");
                }

                UpdateMonitoredParamsStatus(
					currentMonitorAgentSettings.VmInstanceSettings,
					currentMonitorAgentSettings.ProcessInstancesSettings, 
                    _vmInfo, 
                    _processInstancesInfo);
                KillIntractableInstances(currentMonitorAgentSettings.ProcessInstancesSettings, _processInstancesInfo);
            }

            WatchDogHandling(
                currentMonitorAgentSettings.VmInstanceSettings,
                currentMonitorAgentSettings.ProcessInstancesSettings,
                currentMonitorAgentSettings.ProcessInstancesRunStateSettings,
                _vmInfo,
                _processInstancesInfo);

            CleanDoneJobsAndCreateNewInfosCopy();
        }
        #endregion Private Methods - Main Loop

        public void KillAllAndClearMonitoredData(MonitorAgentSettings monitorAgentSettings)
        {
            _logger?.LogInformation($"count to kill: {monitorAgentSettings.ProcessInstancesSettings.Count}");

            foreach (var app in monitorAgentSettings.ProcessInstancesSettings)
            {
                if (string.IsNullOrEmpty(app.ApplicationFileName))
                    continue;

                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(app.ApplicationFileName));

                foreach (var proc in processes)
                {
					_logger?.LogInformation($"kill pid: {proc.Id}");					
					ProcessHelper.KillAppInstance(proc.Id, null, _logger);
                }
            }

            _vmInfo = new VmInfo();
            _processInstancesInfo = new List<ProcessInstanceInfo>();
        }

		#region Private Methods
		private void EnqueueNewJob(Action job)
        {
            jobs.Enqueue(job);
        }

        private void DoOneWaitingJob()
        {
            if (jobs.TryDequeue(out var job))
                job();
        }
        #endregion Private Methods

        private void ChangesInstructions(
            out bool vmInstanceSettingsUpdated, 
            out bool processInstancesSettingsUpdated, 
            out bool processInstancesRunStateSettingsUpdated)
        {
            vmInstanceSettingsUpdated = false;
            if (newMonitorAgentSettings != null && newMonitorAgentSettings.VmInstanceSettings != null )
            {
                if (currentMonitorAgentSettings.VmInstanceSettings == null)
                {
                    vmInstanceSettingsUpdated = true;
                }
                else
                {
                    vmInstanceSettingsUpdated =
                        !newMonitorAgentSettings.VmInstanceSettings.CompileTool.Compare(currentMonitorAgentSettings.VmInstanceSettings.CompileTool)
                        ||
                        !newMonitorAgentSettings.VmInstanceSettings.Variables.Compare(currentMonitorAgentSettings.VmInstanceSettings.Variables);
                }
            }                        
            
            processInstancesSettingsUpdated = false;
            if (newMonitorAgentSettings != null && newMonitorAgentSettings.ProcessInstancesSettings != null)
            {
                if (currentMonitorAgentSettings.ProcessInstancesSettings == null)
                {
                    processInstancesSettingsUpdated = true;
                }
                else
                {
                    processInstancesSettingsUpdated = newMonitorAgentSettings.ProcessInstancesSettings.Count != currentMonitorAgentSettings.ProcessInstancesSettings.Count;
                    if  (!processInstancesSettingsUpdated)
                    {
                        var newOrdered = newMonitorAgentSettings.ProcessInstancesSettings.OrderBy(x => x.Id).ToList();
                        var currentOrdered = currentMonitorAgentSettings.ProcessInstancesSettings.OrderBy(x => x.Id).ToList();

                        for (int i = 0; i < newOrdered.Count; i++)
                        {
                            if (!newOrdered[i].Compare(currentOrdered[i]))
                            {
                                processInstancesSettingsUpdated = true;
                                break;
                            }
                        }
                    }
                }
            }

            processInstancesRunStateSettingsUpdated = false;
            if (newMonitorAgentSettings != null && newMonitorAgentSettings.ProcessInstancesRunStateSettings != null)
            {
                if (currentMonitorAgentSettings.ProcessInstancesRunStateSettings == null)
                {
                    processInstancesRunStateSettingsUpdated = true;
                }
                else
                {
                    processInstancesRunStateSettingsUpdated = newMonitorAgentSettings.ProcessInstancesRunStateSettings.Count != currentMonitorAgentSettings.ProcessInstancesRunStateSettings.Count;
                    if (!processInstancesRunStateSettingsUpdated)
                    {
                        var newOrdered = newMonitorAgentSettings.ProcessInstancesRunStateSettings.OrderBy(x => x.Id).ToList();
                        var currentOrdered = currentMonitorAgentSettings.ProcessInstancesRunStateSettings.OrderBy(x => x.Id).ToList();

                        for (int i=0; i< newOrdered.Count;i++)
                        {
                            if (!newOrdered[i].Compare(currentOrdered[i]))
                            {
                                processInstancesRunStateSettingsUpdated = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void CleanDoneJobsAndCreateNewInfosCopy() 
        {
            newMonitorAgentSettings = null;

            _vmInfo.LastSetGuid = lastGuid;
            _vmInfoCopy = _vmInfo.Clone();
			_processInstancesInfoCopy = _processInstancesInfo.Select(x => x.Clone()).ToList();
        }

        private void UpdateMonitoredParamsStatus(
			VmInstanceSettings vmInstanceSettings,
			List<ProcessInstanceSettings> processInstancesSettings,
            VmInfo vmInfo,
            List<ProcessInstanceInfo> processInstancesInfo)
        {
            var envVars = Environment.GetEnvironmentVariables();

            var envList = new List<Tuple<string, string>>();
            foreach (string evnVar in envVars.Keys)
                envList.Add(Tuple.Create<string, string>(evnVar, (string)envVars[evnVar]));
            
			vmInfo.Variables = envList;

            var settingsProcInstIds = processInstancesSettings.Select(x => x.Id).ToHashSet();

            for (int i=0; i<processInstancesInfo.Count; i++)
            {
                var proc = processInstancesInfo[i];

                if (!proc.Id.HasValue || !settingsProcInstIds.Contains(proc.Id.Value))
                {
                    processInstancesInfo[i] = null;
                    continue;
                }
                
                if (proc.ProcessId.HasValue)
                {
                    var p = ProcessHelper.GetProcessInfo(proc.ProcessId.Value, _logger);
                    if (!p.IsRunning || p.ProcessName != proc.ProcessName)
                    {
                        proc.ProcessId = null;
                        proc.IsRunning = false;
                    }
                }

                if (CompilingInfo.InCompile.ContainsKey(proc.Id.Value))
                    proc.IsCompiling = CompilingInfo.InCompile[proc.Id.Value];

			}

            processInstancesInfo.RemoveAll(x => x == null);
        }
        
        private void KillIntractableInstances(
            List<ProcessInstanceSettings> processInstancesSettings,
            List<ProcessInstanceInfo> processInstancesInfo)
        {
            foreach (var app in processInstancesSettings)
            {
                if (string.IsNullOrEmpty(app.ApplicationFileName))
                    continue;

                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(app.ApplicationFileName));

                foreach (var proc in processes)
                {
                    if (!(processInstancesInfo.Count > 0 &&
                        processInstancesInfo.Where(x => x.IsRunning).Count() > 0 &&
                        processInstancesInfo.Where(x => x.IsRunning).Any(x => x.ProcessId.HasValue && x.ProcessId.Value == proc.Id)))
                    {
                        _logger.LogInformation($"Closing App {proc.ProcessName} ({proc.Id}) beacuse it not being started by the monitor agent ...");
                        ProcessHelper.KillAppInstance(proc.Id, null, _logger);
                    }
                }
            }
        }

        private void WatchDogHandling(
            VmInstanceSettings vmInstanceSettings,
            List<ProcessInstanceSettings> processInstancesSettings,
            List<ProcessInstanceRunStateSettings> processInstancesRunStateSettings,
            VmInfo vmInfo,
            List<ProcessInstanceInfo> processInstancesInfo)
        {            
            var dictionaryProcessInstancesRunStateSettings = processInstancesRunStateSettings.Where(x => x.Id.HasValue).ToDictionary(x => x.Id.Value, x => x);
            var processInstancesInfoHashSet = processInstancesInfo.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToHashSet();
            foreach (var processInstance in processInstancesSettings)
                if (!processInstancesInfoHashSet.Contains(processInstance.Id))
                    processInstancesInfo.Add(new ProcessInstanceInfo(processInstance.Id));

            var processInstancesInfoDictionary = processInstancesInfo.Where(x => x.Id.HasValue).ToDictionary(x => x.Id.Value, x => x);

            foreach (var appInstance in processInstancesSettings)
            {
                try
                {
                    var runState = dictionaryProcessInstancesRunStateSettings[appInstance.Id];
                    if (runState.RunState == RunStateEnum.Run)
                    {
                        if (processInstancesInfoDictionary[appInstance.Id].IsRunning)
                        {
                            continue;
                        }
                        else
                        {
                            var appName = appInstance.Name;
                            var appPath = appInstance.ApplicationPath;
                            var appWorkingDir = appInstance.ApplicationWorkingDirectory;
                            var appFileName = appInstance.ApplicationFileName;
                            var appArguments = appInstance.Arguments;
                            var appEnviromentVars = appInstance.Variables;

                            var appEnvironmentFinalVars = new List<Tuple<string, string>>();
                            var envKeys = new HashSet<String>();
                            foreach (var env in appEnviromentVars.Concat(vmInstanceSettings.Variables))
                            {
                                if (!envKeys.Contains(env.Item1))
                                    envKeys.Add(env.Item1);
                                else
                                    continue;

                                appEnvironmentFinalVars.Add(Tuple.Create<string, string>(env.Item1, env.Item2));
                            }

                            _logger.LogInformation($"App is not in running state ({appName}) ...");

                            if (!File.Exists(Path.Combine(appPath, appFileName)) && appInstance.UseImage)
                            {
								_logger.LogInformation($"Downloading & Extracting ({appName}) ...");

                                DownloadFromMonitor(
                                    appInstance.RannerMonitorBaseUrl, 
                                    appInstance.UniqueImageName, 
                                    appInstance.ZipFileName,
									Path.Combine(Path.Combine(appInstance.FolderToExtract, ".."), appInstance.ZipFileName));

                                ExtractZip(Path.Combine(Path.Combine(appInstance.FolderToExtract, ".."), appInstance.ZipFileName), appInstance.FolderToExtract);
							}

                            var res = ProcessHelper.StartAppInstance(
                                appName,
                                appPath,
                                appFileName,
                                appWorkingDir,
                                appArguments,
                                appEnvironmentFinalVars,
                                _logger,
                                true);

                            processInstancesInfoDictionary[appInstance.Id].IsRunning = res.IsRunning;
                            processInstancesInfoDictionary[appInstance.Id].ProcessId = res.ProcessId;
                            processInstancesInfoDictionary[appInstance.Id].ProcessName = res.ProcessName;
                            processInstancesInfoDictionary[appInstance.Id].LastStartTime = DateTime.Now;
                            if (appInstance.UseImage)
							    processInstancesInfoDictionary[appInstance.Id].ZipFileName = Path.GetFileNameWithoutExtension(appInstance.ZipFileName);
                            else
								processInstancesInfoDictionary[appInstance.Id].ZipFileName = string.Empty;

						}
                    }
                    else
                    {
                        if (processInstancesInfoDictionary[appInstance.Id].IsRunning)
                        {
                            _logger.LogInformation($"About to kill ({processInstancesInfoDictionary[appInstance.Id].ProcessName}) because run status has changed to stop...");

                            if (processInstancesInfoDictionary[appInstance.Id].ProcessId.HasValue)
                                ProcessHelper.KillAppInstance(processInstancesInfoDictionary[appInstance.Id].ProcessId.Value, null, _logger);

                            processInstancesInfoDictionary[appInstance.Id].ProcessId = null;
                            processInstancesInfoDictionary[appInstance.Id].IsRunning = false;

                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (CompilingInfo.InCompile.ContainsKey(appInstance.Id))
                        processInstancesInfoDictionary[appInstance.Id].IsCompiling = CompilingInfo.InCompile[appInstance.Id];
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex.ToString());
                }
            }
        }

		private void ExtractZip(string fullPathZipFileName, string folderToExtract)
		{
			var zipExtractor = new ZipExtractor();
			zipExtractor.ExtractAllFiles(fullPathZipFileName, folderToExtract);
		}

		private void DownloadFromMonitor(string baseUrl, string uniqueImageName, string zipFileName, string saveToFullPathAndFileName)
		{
			var dir = Path.GetDirectoryName(saveToFullPathAndFileName);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			var webDownloader = new WebFileDownloader();
            var response = webDownloader.DownloadFileWithExtraInfoAsync($"{baseUrl}/download/", zipFileName, uniqueImageName, saveToFullPathAndFileName);

			response.Wait();
		}
	}

    public static class ProcessHelper
    {
        public static ProcessInfo GetProcessInfo(int id, Microsoft.Extensions.Logging.ILogger logger)
        {
            var processInfo = new ProcessInfo();
            
            try
            {
                var proc = Process.GetProcessById(id);

                if (proc != null)
                {
                    processInfo.IsRunning = true;
                    processInfo.ProcessId = proc.Id;
                    processInfo.ProcessName = proc.ProcessName;
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.EndsWith("not running."))
                    logger.LogError(ex.ToString());                
            }
            

            return processInfo;
        }

        public static ProcessInfo StartAppInstance(
            string appName,
            string appPath,
            string appFileName,
            string workingDirectory,
            string arguments,
            List<Tuple<string, string>> enviromentVariables,
			Microsoft.Extensions.Logging.ILogger logger,
            bool logConsoleOutput = false,
			bool runInSeperateConsole = false)
        {
            

			var process = new Process();

            process.StartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(appPath, appFileName),
                WorkingDirectory = workingDirectory,
                Arguments = arguments,               
            };

            if (runInSeperateConsole)
            {
                process.StartInfo.FileName = appFileName;
				process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.CreateNoWindow = false;
            }

			if (!process.StartInfo.UseShellExecute)
            {
                var sb = new StringBuilder();
                foreach (var envVar in enviromentVariables)
                {
                    process.StartInfo.Environment.Add(envVar.Item1, envVar.Item2); //process.StartInfo.EnvironmentVariables.Add(envVar.Item1, envVar.Item2);
					sb.AppendLine($@"{'\t'}{'\t'}{envVar.Item1} {envVar.Item2}");
                }
            
    			logger.LogInformation(
                    $@"{'\n'}Starting App {appName}: {'\n'}{'\t'}WorkDir: {process.StartInfo.WorkingDirectory} {'\n'}{'\t'}FileName: {process.StartInfo.FileName}, {'\n'}{'\t'}Args: {process.StartInfo.Arguments} {'\n'}{'\t'}EnvVars: {'\n'}{sb.ToString()}");
			}

			if (logConsoleOutput)
            {
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.OutputDataReceived += (sender, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
					{
                        logger.LogInformation($"[{appName}] {args.Data}");
					}
				};

				process.ErrorDataReceived += (sender, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
					{
						logger.LogInformation($"[{appName}] {args.Data}");
					}
				};
            }

			process.Start();

            try
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex) { logger.LogError(ex.ToString()); }

			return new ProcessInfo() { IsRunning = true, ProcessId = process.Id, ProcessName = process.ProcessName, ProcessInstance = process };
        }

        public static void KillAppInstance(
            int? processId,
            string appFileName,
			Microsoft.Extensions.Logging.ILogger logger)
        {
            if (!processId.HasValue)
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(appFileName));
                foreach (var proc in processes)
                {
                    logger.LogInformation($"Killed: {proc.ProcessName} ({proc.Id})");
                    proc.Kill();
                }
            }
            else
            {
                var proc = Process.GetProcessById(processId.Value);
                logger.LogInformation($"Killed: {proc.ProcessName} ({proc.Id})");
                proc.Kill();
            }
        }
    }

    public class ProcessInfo
    {
        public bool IsRunning { get; set; }
        public int? ProcessId { get; set; }
        public string ProcessName { get; set; }

        public Process ProcessInstance { get; set; }
	}

    public static class CompilingInfo
    {
        public static ConcurrentDictionary<int, bool> InCompile = new ConcurrentDictionary<int, bool>();
    }
}
