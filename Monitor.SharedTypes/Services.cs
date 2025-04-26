namespace AppMonitoring.SharedTypes
{
    public class MonitorAgentSettings
    {
        public VmInstanceSettings VmInstanceSettings { get; set; } = new VmInstanceSettings();
        public List<ProcessInstanceRunStateSettings> ProcessInstancesRunStateSettings { get; set; } = new List<ProcessInstanceRunStateSettings>();
        public List<ProcessInstanceSettings> ProcessInstancesSettings { get; set; } = new List<ProcessInstanceSettings>();
    }

    public interface IMonitorAgentService
    {
        void InvokeNewSettings(MonitorAgentSettings monitorAgentSettings);
        void InvokeKillAll();
        void InvokeStartAll();
        void InvokeCompile(int id, string configuration);
        void InvokeGenericCommand(GenericCommand genericCommand);

		MonitorAgentSettings GetMonitorAgentSettings();
        Tuple<VmInfo, List<ProcessInstanceInfo>> GetVmInfoAndListProcessInstaceInfo();
    }
}



