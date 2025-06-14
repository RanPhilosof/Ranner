using MudBlazor;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Montior.Blazor.Data;

public class UI_ImagesVariables
{
	public List<SourceValue> SourcesVariables { get; set; } = new List<SourceValue>();
	public List<ImageValue> SelectedImagesVariables { get; set; } = new List<ImageValue>();

	public bool ShowImagesVariables { get; set; } = false;

	public bool Compare(UI_ImagesVariables other)
	{
		if (SourcesVariables.Count != other.SourcesVariables.Count)
			return false;

		for (int i = 0; i < SourcesVariables.Count; i++)
		{
			if (SourcesVariables[i].UniqueName != other.SourcesVariables[i].UniqueName)
				return false;
			if (SourcesVariables[i].Url != other.SourcesVariables[i].Url)
				return false;
			if (SourcesVariables[i].BranchFilter != other.SourcesVariables[i].BranchFilter)
				return false;
			if (SourcesVariables[i].ExtensionFilter != other.SourcesVariables[i].ExtensionFilter)
				return false;
			if (SourcesVariables[i].FileNameFilter != other.SourcesVariables[i].FileNameFilter)
				return false;
			if (SourcesVariables[i].Active != other.SourcesVariables[i].Active)
				return false;
			if (SourcesVariables[i].Description != other.SourcesVariables[i].Description)
				return false;
		}
		
		if (SelectedImagesVariables.Count != other.SelectedImagesVariables.Count)
			return false;

		for (int i = 0; i < SelectedImagesVariables.Count; i++)
		{
			if (SelectedImagesVariables[i].UniqueName != other.SelectedImagesVariables[i].UniqueName)
				return false;
			if (SelectedImagesVariables[i].ZipFileInfo.FileName != other.SelectedImagesVariables[i].ZipFileInfo.FileName)
				return false;
			if (SelectedImagesVariables[i].ZipFileInfo.FullPath != other.SelectedImagesVariables[i].ZipFileInfo.FullPath)
				return false;
			if (SelectedImagesVariables[i].SourceUniqueName != other.SelectedImagesVariables[i].SourceUniqueName)
				return false;
			if (SelectedImagesVariables[i].Active != other.SelectedImagesVariables[i].Active)
				return false;
			if (SelectedImagesVariables[i].Description != other.SelectedImagesVariables[i].Description)
				return false;
		}
		return true;
	}
}
public class SourceValue
{
	public bool Active { get; set; } = false;
	public string UniqueName { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public string BranchFilter { get; set; } = string.Empty;
	public string ExtensionFilter { get; set; } = string.Empty;
	public string FileNameFilter { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
}

public class ImageValue
{
	public bool Active { get; set; } = false;
	public string UniqueName { get; set; } = string.Empty;
	public UI_ZipFileInfo ZipFileInfo { get; set; } = new UI_ZipFileInfo();
	public string SourceUniqueName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	[Newtonsoft.Json.JsonIgnore]
	public List<UI_ZipFileInfo> PossibleZipFilesInfo = new List<UI_ZipFileInfo>();
}

public class UI_ZipFileInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FullPath { get; set; } = string.Empty;

	public override bool Equals(object obj)
	{
		return obj is UI_ZipFileInfo other &&
			   FileName == other.FileName &&
			   FullPath == other.FullPath;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(FileName, FullPath);
	}
}


public class UI_Configuration
{
	static Dictionary<string, KeyValueComplex> ConfigStructure = new Dictionary<string, KeyValueComplex>()
	{
		{ ForceStartAllOnLoad, new KeyValueComplex() { Active = false, Key = ForceStartAllOnLoad, Value = "true", Description = "Override Configuration Run State" } },
		{ ForceActiveGroupsOnLoad, new KeyValueComplex() { Active = false, Key = ForceActiveGroupsOnLoad, Value = "Target", Description = "Override Configuration Active Groups" } },
		{ ForceConfigurationOnLoad, new KeyValueComplex() { Active = false, Key = ForceConfigurationOnLoad, Value = "Release", Description = "Override Configuration Release / Debug" } },
		{ ForceProjectOnLoad, new KeyValueComplex() { Active = false, Key = ForceProjectOnLoad, Value = "Proj1", Description = "Override Configuration Active Project" } },
		{ ImagesCacheFolder, new KeyValueComplex() { Active = true, Key = ImagesCacheFolder, Value = @"C:\Ranner\ImagesCaches", Description = "All Versions Repository Folders" } },
		{ RannerMonitorIpAddressForAgents, new KeyValueComplex() { Active = true, Key = RannerMonitorIpAddressForAgents, Value = "localhost", Description = "Monitor Ip Address For Agents" } },
		{ RannerMonitorPortAddressForAgents, new KeyValueComplex() { Active = true, Key = RannerMonitorPortAddressForAgents, Value = "5087", Description = "Monitor Port Address For Agents" } },
	};

	public const string ForceStartAllOnLoad = "ForceStartAllOnLoad";
	public const string ForceActiveGroupsOnLoad = "ForceActiveGroupsOnLoad";
	public const string ForceConfigurationOnLoad = "ForceConfigurationOnLoad";
	public const string ForceProjectOnLoad = "ForceProjectOnLoad";
	public const string ImagesCacheFolder = "ImagesCacheFolder";
	public const string RannerMonitorIpAddressForAgents = "RannerMonitorIpAddressForAgents";
	public const string RannerMonitorPortAddressForAgents = "RannerMonitorPortAddressForAgents";

	public List<KeyValueComplex> Configuration { get; set; } = new List<KeyValueComplex>();
	public bool ShowConfigurationVariables { get; set; } = false;

	public void UpgradeIfNeeded()
	{
		bool upgradeNeeded = false;

		upgradeNeeded = Configuration.Count != ConfigStructure.Count;
		if (!upgradeNeeded)
			upgradeNeeded = Configuration.Any(x => !ConfigStructure.ContainsKey(x.Key));

		if (upgradeNeeded)
		{
			var currentConfig = Configuration;

			var newConfigDict = Configuration.ToDictionary(x => x.Key, x => x);

			foreach (var elementInCorrectStructure in ConfigStructure)
				if (!newConfigDict.ContainsKey(elementInCorrectStructure.Key))
					newConfigDict.Add(elementInCorrectStructure.Key, elementInCorrectStructure.Value.Clone());

			foreach (var curConf in currentConfig)
				if (!ConfigStructure.ContainsKey(curConf.Key))
					newConfigDict.Remove(curConf.Key);

			Configuration = newConfigDict.Values.ToList();
		}		
	}

	public bool Compare(UI_Configuration other)
	{
		if (Configuration.Count != other.Configuration.Count)
			return false;

		for (int i = 0; i < Configuration.Count; i++)
		{
			if (Configuration[i].Key != other.Configuration[i].Key)
				return false;
			if (Configuration[i].Value != other.Configuration[i].Value)
				return false;
			if (Configuration[i].DefaultValue != other.Configuration[i].DefaultValue)
				return false;
			if (Configuration[i].Active != other.Configuration[i].Active)
				return false;
			if (Configuration[i].Description != other.Configuration[i].Description)
				return false;
		}
		return true;
	}
}

public class UI_SessionsData
{
	public List<UI_SessionData> SessionDataList { get; set; } = new List<UI_SessionData>();

	public bool Compare(UI_SessionsData other)
	{
		if (SessionDataList.Count != other.SessionDataList.Count)
			return false;

		for (int i = 0; i < SessionDataList.Count; i++)
		{
			if (!SessionDataList[i].Compare(other.SessionDataList[i]))
				return false;
		}

		return true;
	}
}

public class UI_VmsData
{
	public List<UI_VmData> VmsDataList { get; set; } = new List<UI_VmData>();

	public bool Compare(UI_VmsData other)
	{
		if (VmsDataList.Count != other.VmsDataList.Count)
			return false;
		
		for (int i = 0; i < VmsDataList.Count; i++)
		{
			if (!VmsDataList[i].Compare(other.VmsDataList[i]))
				return false;
		}

		return true;
	}
}

public class UI_GroupTags
{
	public List<KeyListValue> GroupTags { get; set; } = new List<KeyListValue>();

    public bool Compare(UI_GroupTags other)
	{
		if (GroupTags.Count != other.GroupTags.Count)
			return false;

		for (int i = 0; i < GroupTags.Count; i++)
		{
			if (GroupTags[i].ShowValues != other.GroupTags[i].ShowValues)
				return false;
			if (GroupTags[i].Key != other.GroupTags[i].Key)
				return false;
			if (GroupTags[i].Values.Count != other.GroupTags[i].Values.Count)
				return false;
			for (int j = 0; j < GroupTags[i].Values.Count; j++)
			{
				if (GroupTags[i].Values[j].IsActive != other.GroupTags[i].Values[j].IsActive)
					return false;
				if (GroupTags[i].Values[j].Value != other.GroupTags[i].Values[j].Value)
					return false;
			}
		}

        return true;
	}
}

public class UI_GlobalVariables
{
	public List<KeyValueComplex> GlobalVariables { get; set; } = new List<KeyValueComplex>();

	public bool ShowGlobalVariables { get; set; } = false;

	public bool Compare(UI_GlobalVariables other)
	{
		if (GlobalVariables.Count != other.GlobalVariables.Count)
			return false;

		for (int i = 0; i < GlobalVariables.Count; i++)
		{
			if (GlobalVariables[i].Key != other.GlobalVariables[i].Key)
				return false;
			if (GlobalVariables[i].Value != other.GlobalVariables[i].Value)
				return false;
			if (GlobalVariables[i].DefaultValue != other.GlobalVariables[i].DefaultValue)
				return false;
			if (GlobalVariables[i].Active != other.GlobalVariables[i].Active)
				return false;
			if (GlobalVariables[i].Description != other.GlobalVariables[i].Description)
				return false;
		}
		return true;
	}
}

public class UI_VmData
{
	public string UniqueName { get; set; } = string.Empty;

	public string IpAddress { get; set; } = string.Empty;
	public string Port { get; set; } = string.Empty;

	public bool ShowCompilerInfo { get; set; } = false;
	public List<UI_VmCompiler> Compilers { get; set; } = new List<UI_VmCompiler>();

	public bool ShowExtraEnviromentsVariables { get; set; }
	public List<KeyValueComplex> ExtraVariables { get; set; } = new List<KeyValueComplex>();

	public bool ShowVmActualEnviromentVariables { get; set; }
	[Newtonsoft.Json.JsonIgnore]
	public List<KeyValue> ActualVariables { get; set; } = new List<KeyValue>();
	[Newtonsoft.Json.JsonIgnore]
	public TimeInfo LastUpdateTime { get; set; }
	public bool Compare(UI_VmData other)
	{
		if (UniqueName != other.UniqueName)
			return false;
		if (IpAddress != other.IpAddress)
			return false;
		if (Port != other.Port) 
			return false;
		if (Compilers.Count != other.Compilers.Count)
			return false;
		for (int i = 0; i < Compilers.Count; i++)
		{
			if (Compilers[i].CompilerName != other.Compilers[i].CompilerName)
				return false;
			if (Compilers[i].CompilerPath != other.Compilers[i].CompilerPath)
				return false;
		}

		if (ExtraVariables.Count != other.ExtraVariables.Count)
			return false;
		
		for (int i=0; i<ExtraVariables.Count; i++)
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


		return true;
	}

	public string GetIpAndPortKey()
	{
		return $"{IpAddress}_{Port}";
	}
}

public class UI_VmCompiler
{
   public string CompilerName { get; set; } = string.Empty;
   public string CompilerPath { get; set; } = string.Empty;
}

public class UI_SessionData
{
	public string UniqueName { get; set; } = string.Empty;
	public int UniqueId { get; set; }
	
	public bool ShowExtraEnviromentsVariables { get; set; }
	public List<KeyValueComplex> ExtraVariables { get; set; } = new List<KeyValueComplex>();

	public bool Compare(UI_SessionData other)
	{
		if (UniqueName != other.UniqueName)
			return false;
		if (UniqueId != other.UniqueId)
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

		return true;
	}
}

public class KeyValue
{
   public string Key { get; set; } = string.Empty;
   public string Value { get; set; } = string.Empty;
}

public class KeyListValue
{
   public string Key { get; set; } = string.Empty;
   public bool ShowValues { get; set; } = false;
   public List<StringWrapper> Values { get; set; } = new List<StringWrapper>();
}

public class KeyValueComplex : KeyValue
{
    public bool Active { get; set; } = false;
    public string DefaultValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

	public KeyValueComplex Clone()
	{
		var clone = new KeyValueComplex();

		clone.Key = Key;
		clone.Value = Value;
		clone.Active = Active;
		clone.DefaultValue = DefaultValue;
		clone.Description = Description;

		return clone;
	}
}


public class StringWrapper
{
   public bool IsActive { get; set; } = true;
   public string Value { get; set; } = string.Empty;
}

public class IpAddress
{
	public string Ip { get; set; }
	public string Port { get; set; }
}

public class TimeInfo
{
	public DateTime Time = DateTime.MinValue;
}