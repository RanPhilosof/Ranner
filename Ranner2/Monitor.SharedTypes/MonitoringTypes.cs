using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Linq;

namespace AppMonitoring.SharedTypes
{
    public class CompileInfo
    {
        public int InstanceId { get; set; }
        public string Configuration { get; set; } = "Debug";
    }
    public class GenericCommand
    {
        public string Command { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
    }
    public class Monitoring
    {
        public List<Vm> Vms { get; set; } = new List<Vm>();
        public List<ProcessInstance> ProcessInstances { get; set; } = new List<ProcessInstance>(); 
    }

    #region Vms
    public class Vm
    {
        #region Id
        public Vm(string uniqueName) { UniqueName = uniqueName; }

        public string UniqueName { get; set; }
        #endregion Id

        public VmInstanceSettings VmInstanceSettings { get; set; } = new VmInstanceSettings();
        public VmInfo VmInfo { get; set; } = new VmInfo();
        public VmNetworkInfo VmNetworkInfo { get; set; } = new VmNetworkInfo();
    }

    public class VmNetworkInfo
    {
        public string VM_MonitorAgent_IpAddress { get; set; } = string.Empty;
        public string VM_MonitorAgent_IpPort { get; set; } = string.Empty;

		public VmNetworkInfo Clone()
		{
			var clone = new VmNetworkInfo();

            clone.VM_MonitorAgent_IpAddress = VM_MonitorAgent_IpAddress;
            clone.VM_MonitorAgent_IpPort = VM_MonitorAgent_IpPort;

			return clone;
		}
	}

    public class VmInstanceSettings
    {
        public string GuidString { get; set; } = Guid.NewGuid().ToString();
		public List<Tuple<string, string>> CompileTool { get; set; } = new List<Tuple<string, string>>();
		public List<Tuple<string, string>> Variables { get; set; } = new List<Tuple<string, string>>();

        public VmInstanceSettings Clone()
        {
			var clone = new VmInstanceSettings();

            clone.GuidString = GuidString;

			foreach (var var in CompileTool)
				clone.CompileTool.Add(Tuple.Create(var.Item1, var.Item2));

			foreach (var var in Variables)
				clone.Variables.Add(Tuple.Create(var.Item1, var.Item2));

			return clone;
		}
	}

    public class VmInfo
    {
        public string LastSetGuid { get; set; } = Guid.NewGuid().ToString();
        public List<Tuple<string, string>> Variables { get; set; } = new List<Tuple<string, string>>();

        public VmInfo Clone()
        {
            var clone = new VmInfo();

            clone.LastSetGuid = LastSetGuid;

            foreach (var var in Variables)
                clone.Variables.Add(Tuple.Create(var.Item1, var.Item2));

            return clone;
        }
    }
    #endregion Vms

    #region Process Instance

    public class ProcessInstance
    {
        public ProcessInstanceSettings Settings { get; set; } = new ProcessInstanceSettings();
        public ProcessInstanceRunStateSettings RunStateSettings { get; set; } = new ProcessInstanceRunStateSettings(id: null);
        public ProcessInstanceInfo Info { get; set; } = new ProcessInstanceInfo(id: null);
    }

    public class ProcessInstanceSettings
    {
        #region Id
        public static int idGenerator = 0;
        public ProcessInstanceSettings() { Id = Interlocked.Increment(ref idGenerator); }
        public ProcessInstanceSettings(int id) { Id = id; }

        public int Id { get; set; }
        #endregion Id

        #region Vm Info
        public string VmUniqueName { get; set; } = string.Empty;

        #endregion Vm Info

        #region Process Info
        public string Name { get; set; } = string.Empty;
        public string CsProj { get; set; } = string.Empty;

        public string ApplicationFileName { get; set; } = string.Empty;
        public string ApplicationPath { get; set; } = string.Empty;
        public string ApplicationWorkingDirectory { get; set; } = string.Empty;
        public string RestApiPort { get; set; } = string.Empty;
		public string InstanceId { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;

        public List<Tuple<string, string>> Variables { get; set; } = new List<Tuple<string, string>>();
        
        #endregion Process Info

        public bool Compare(ProcessInstanceSettings other)
        {
            if (Id != other.Id) return false;
            if (VmUniqueName != other.VmUniqueName) return false;
            if (Name != other.Name) return false;
            if (CsProj != other.CsProj) return false;
            if (ApplicationFileName != other.ApplicationFileName) return false;
            if (ApplicationPath != other.ApplicationPath) return false;
            if (ApplicationWorkingDirectory != other.ApplicationWorkingDirectory) return false;
            if (RestApiPort != other.RestApiPort) return false;
            if (InstanceId != other.InstanceId) return false;
            if (Arguments != other.Arguments) return false;
            if (!Variables.Compare(other.Variables)) return false;

            return true;
        }
    }

    public class ProcessInstanceRunStateSettings
    {
        #region Id
        public ProcessInstanceRunStateSettings(int? id) { Id = id; }
        public int? Id { get; set; }
        #endregion Id

        public RunStateEnum RunState { get; set; }
        public bool Compare(ProcessInstanceRunStateSettings other)
        {
            if (Id != other.Id) return false;
            if (RunState != other.RunState) return false;

            return true;
        }
    }

    public enum RunStateEnum
    {
        Run,
        Stop,
    }

    public class ProcessInstanceInfo
    {
        #region Id
        public ProcessInstanceInfo(int? id) { Id = id; }
        public int? Id { get; set; }
        #endregion Id

        #region Process Instance Info
        public bool IsCompiling { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public DateTime? LastStartTime { get; set; }        
        public int? ProcessId { get; set; }
        #endregion Process Instance Info

        public ProcessInstanceInfo Clone()
        {
            var clone = new ProcessInstanceInfo(null);

            clone.Id = Id;
            clone.IsRunning = IsRunning;
            clone.LastStartTime = LastStartTime;
            clone.ProcessId = ProcessId;
            clone.ProcessName = ProcessName;

            clone.IsCompiling = IsCompiling;

            return clone;
        }
    }

    public static class Extensions
    {
        public static bool Compare(this List<Tuple<string, string>> variables, List<Tuple<string, string>> other)
        {
            if (variables == null && other == null) return true;
            if (variables == null && other != null) return false;
            if (variables != null && other == null) return false;

            if (variables.Count != other.Count) return false;
            
            var ordered1 = variables.OrderBy(x => x.Item1).ToList();
            var ordered2 = other.OrderBy(x => x.Item1).ToList();

            for (int i=0; i<ordered1.Count; i++)
            {
                if (ordered1[i].Item1 !=  ordered2[i].Item1) return false;
                if (ordered1[i].Item2 != ordered2[i].Item2) return false;
            }

            return true;
        }
    }
    #endregion Process Instance
}



