using MudBlazor;
using System.Text.Json.Serialization;

namespace Montior.Blazor.Data;

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