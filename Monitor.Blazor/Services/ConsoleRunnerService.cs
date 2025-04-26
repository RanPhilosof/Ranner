using CommandLine;
using Monitor.Blazor.Interfaces;
using Monitor.Blazor.Pages;
using Montior.Blazor.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Monitor.Blazor.Services
{
	public class ConsoleRunnerServier : IConsoleRunnerService
	{
		public ConsoleRunnerServier(
			IMonitorAgentCommunicationLayer mL,
			IMonitorService mS)
		{
			//Debugger.Launch();
			Process agentProcess = null;
            var parsedArgs = Parser.Default.ParseArguments<BlazorOptions>(Environment.GetCommandLineArgs());

			if (parsedArgs.Value != null)
			{
				if (parsedArgs.Value.StartAgent)
				{
					var agents = Process.GetProcessesByName("Monitor.Agent").ToList();
					Console.WriteLine($"Killing Old Agents {agents.Count}");
					foreach (var agent in agents)
						agent.Kill();

					agentProcess = new Process();
					var startInfo = new ProcessStartInfo()
					{
						FileName = "Monitor.Agent.exe",
					};

					agentProcess.StartInfo = startInfo;

					agentProcess.Start();
				}

				Thread.Sleep(1000);				

				if (!string.IsNullOrEmpty(parsedArgs.Value.ConfigurationFile))
				{
					MonitorPageSettings configSettings = null;
					try
					{
						configSettings = mS.GetSettingsFromPresetsSettings(parsedArgs.Value.ConfigurationFile);
					}
					catch (Exception ex) 
					{
                        Console.WriteLine($"Failed To Load Config {parsedArgs.Value.ConfigurationFile}");
						configSettings = new MonitorPageSettings();
					}

					if (parsedArgs != null && parsedArgs.Value != null && parsedArgs.Value.Project != null)
						configSettings.InstancesData.Project = parsedArgs.Value.Project;

					//Debugger.Launch();					

					Console.WriteLine($"Before Groups Filtering: {configSettings.InstancesData.Instances.Count}");
					configSettings.InstancesData.ActiveGroups = parsedArgs.Value.Groups;
					configSettings.InstancesData.Instances.ForEach(x => x.Disabled = false);
					configSettings.InstancesData.Instances.ForEach(x => 
					{
						if (x.SupportTags) 
						{
                            x.TagsStr += parsedArgs.Value.AddGroupTags;
                        }
                    });
					configSettings.InstancesData.UpdateDisableStateByGroups();
					configSettings.InstancesData.Instances = configSettings.InstancesData.Instances.Where(x => !x.DisabledByGroups).ToList();
					Console.WriteLine($"After Groups Filtering: {configSettings.InstancesData.Instances.Count}");

					configSettings.InstancesData.Instances.ForEach(x => x.Configuration = "RELEASE");

					if (!string.IsNullOrEmpty(parsedArgs.Value.ReleaseOrDebug))
						if (parsedArgs.Value.ReleaseOrDebug.ToUpper() == "DEBUG")
							configSettings.InstancesData.Instances.ForEach(x => x.Configuration = "DEBUG");

                    if (configSettings.InstancesData.Instances.Count > 0)
					{
						if (!string.IsNullOrEmpty(parsedArgs.Value.Action))
						{
							var vms = configSettings.VmsData.VmsDataList.ToDictionary(x => x.UniqueName, x => new IpAddress() { Ip = x.IpAddress, Port = x.Port });

							switch (parsedArgs.Value.Action.ToLowerInvariant())
							{
								case "kill":
									Console.WriteLine($"kill command count: {configSettings.InstancesData.Instances.Count}");
									configSettings.InstancesData.Instances.ForEach(x => x.RunOrStop = false);
									mS.SetAllAgentsSettings(configSettings, true);
									mS.SetAllAgentsSettings(configSettings, true);
									break;
								case "start":
									configSettings.InstancesData.Instances.ForEach(x => x.RunOrStop = true);
									Console.WriteLine($"Starting Instances: {configSettings.InstancesData.Instances.Count}");
									mS.SetAllAgentsSettings(configSettings, true);
									configSettings.InstancesData.Instances.ForEach(x => x.RunOrStop = true);
									mS.SetAllAgentsSettings(configSettings, true);
									break;
								case "compile":
									configSettings.InstancesData.Instances.ForEach(x => x.RunOrStop = false);
									mS.SetAllAgentsSettings(configSettings, true);
									mS.SetAllAgentsSettings(configSettings, true);

									Thread.Sleep(2000);

									foreach (var instance in configSettings.InstancesData.Instances)
										if (!string.IsNullOrEmpty(instance.CsProj))
											mS.SetCompilerRequest(vms[instance.VmUniqueName], new AppMonitoring.SharedTypes.CompileInfo() { InstanceId = instance.Id, Configuration = instance.Configuration });

									break;
							}

							Thread.Sleep(1000);

							foreach (var vm in vms)
								mS.GenericCommand(vm.Value, new AppMonitoring.SharedTypes.GenericCommand() { Command = "kill" });
							Console.WriteLine($"Sending Killing Request For Agent, Waiting...");
						}
                        Thread.Sleep(1000);
						agentProcess.WaitForExit();                        

                        Console.WriteLine($"Agent Finished And Killed, Killing Monitor...");

                        Process.GetCurrentProcess().Kill();
					}
				}
			}
		}
	}
}
