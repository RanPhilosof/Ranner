namespace Montior.Blazor.Data;

public class UI_InstancesData
{
	public const string ProjectTemplateString = "{project}";
	public const string ConfigurationTemplateString = "{configuration}";
	public const string RootFolderTemplateString = "{rootFolder}";
	public const string packageFolderTemplateString = "{packageFolder}";

	public string Project { get; set; } = string.Empty;
    public List<UI_Instance> Instances { get; set; } = new List<UI_Instance>();
	public string ActiveGroups { 
		get; 
		set; } = string.Empty;

	public void UpdateDisableStateByGroups()
	{
		Instances.ForEach(x => x.DisabledByGroups = true);

		if (ActiveGroups != string.Empty)
		{
			var groups = ActiveGroups.Split(";").Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToLowerInvariant()).ToHashSet();
			var instances = Instances.Where(x => x.Groups.Split(";").Where(x => !string.IsNullOrEmpty(x)).ToList().Any(x => groups.Contains(x.ToLowerInvariant()))).ToList();
			instances.ForEach(x => x.DisabledByGroups = false);
		}
		else
		{
			Instances.ForEach(x => x.DisabledByGroups = false);
		}
	}

	public bool Compare(UI_InstancesData other)
	{
		if (Project != other.Project) 
			return false;
		
		if (Instances.Count != other.Instances.Count)
			return false;

		if (ActiveGroups != other.ActiveGroups)
			return false;

			for (int i = 0; i < Instances.Count; i++)
		{
			if (!Instances[i].Compare(other.Instances[i]))
				return false;
		}

		return true;
	}
}

public class UI_Instance
{
    private static int idGenerator = 0;
    public UI_Instance()
    {
		Id = Interlocked.Increment(ref idGenerator);
	}

	#region Basic Table
	public int Id { get; set; }

    public string VmUniqueName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
    public bool RunOrStop { get; set; }
    public int StartDelayTime_mSec { get; set; }


	#region Monitored Data - Readonly
	[Newtonsoft.Json.JsonIgnore]
	public string ProcessName { get; set; } = string.Empty;
	[Newtonsoft.Json.JsonIgnore]
	public string IsRunning { get; set; } = "Unknown";
	[Newtonsoft.Json.JsonIgnore]
	public TimeSpan? RunningFor { get; set; }
	[Newtonsoft.Json.JsonIgnore]
	public int? ProcessId { get; set; }
    #endregion Monitored Data - Readonly
    #endregion Basic Table
    
    public bool ShowAdvancedConfigurations { get; set; }
    public string RootFolder { get; set; } = string.Empty;
	public string PackageFolder { get; set; } = string.Empty;
	public string Configuration { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
	public string CsProj { get; set; } = string.Empty;
	public string Groups { get; set; } = string.Empty;
	public string ApplicationFileName { get; set; } = string.Empty;
	public string ApplicationPath { get; set; } = string.Empty;
	public string ApplicationWorkingDirectory { get; set; } = string.Empty;
	public string RestApiPort { get; set; } = string.Empty;
	public string InstanceId { get; set; } = string.Empty;
	public bool SupportProberMonitor { get; set; } = false;

	public bool ShowInheritTagsFromGroup { get; set; }
	public List<StringWrapper> InheritTagsFromGroup { get; set; } = new List<StringWrapper>();
	
	public bool SupportTags { get; set; }	
	public string TagsStr { get; set; } = string.Empty;

	public bool ShowExtraEnvironmentVariables { get; set; }
    public List<KeyValueComplex> ExtraVariables { get; set; } = new List<KeyValueComplex>();

	public bool Disabled { get; set; }
	public bool DisabledByGroups { get; set; }

	public bool Compare(UI_Instance other)
	{
		if (Id != other.Id)
			return false;
		if (VmUniqueName != other.VmUniqueName)
			return false;
		if (Name != other.Name)
			return false;
		if (Team != other.Team) 
			return false;
		if (RunOrStop != other.RunOrStop)
			return false;
		if (StartDelayTime_mSec != other.StartDelayTime_mSec)
			return false;

		if (RootFolder != other.RootFolder)
			return false;
		if (PackageFolder != other.PackageFolder)
			return false;
		if (Configuration != other.Configuration)
			return false;
		if (Arguments != other.Arguments)
			return false;
		if (CsProj != other.CsProj)
			return false;
		if (Groups != other.Groups)
			return false;
		if (ApplicationFileName != other.ApplicationFileName)
			return false;
		if (ApplicationPath != other.ApplicationPath)
			return false;
		if (ApplicationWorkingDirectory != other.ApplicationWorkingDirectory)
			return false;
		if (RestApiPort != other.RestApiPort)
			return false;
		if (SupportProberMonitor != other.SupportProberMonitor)
			return false;
        if (InstanceId != other.InstanceId)
			return false;


		if (InheritTagsFromGroup.Count != other.InheritTagsFromGroup.Count)
			return false;

		for (int i = 0; i < InheritTagsFromGroup.Count; i++)
		{
			if (InheritTagsFromGroup[i].IsActive != other.InheritTagsFromGroup[i].IsActive)
				return false;
			if (InheritTagsFromGroup[i].Value != other.InheritTagsFromGroup[i].Value)
				return false;
		}

		if (SupportTags != other.SupportTags)
			return false;

		if (TagsStr != other.TagsStr) 
			return false;

		if (ExtraVariables.Count != other.ExtraVariables.Count)
			return false;

        for (int i = 0; i < ExtraVariables.Count; i++)
        {
            if (ExtraVariables[i].Active != other.ExtraVariables[i].Active)
                return false;
            if (ExtraVariables[i].DefaultValue != other.ExtraVariables[i].DefaultValue)
                return false;
            if (ExtraVariables[i].Description != other.ExtraVariables[i].Description)
                return false;
            if (ExtraVariables[i].Key != other.ExtraVariables[i].Key)
                return false;
            if (ExtraVariables[i].Value != other.ExtraVariables[i].Value)
                return false;
        }

        if (Disabled != other.Disabled)
			return false;

		if (DisabledByGroups != other.DisabledByGroups)
			return false;

		return true;
	}

}