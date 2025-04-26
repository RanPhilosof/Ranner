using MudBlazor;
using System.Text.Json.Serialization;

namespace Montior.Blazor.Data;

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

public class UI_VmData
{
	public string UniqueName { get; set; } = string.Empty;

	public string IpAddress { get; set; } = string.Empty;
	public string Port { get; set; } = string.Empty;

	public bool ShowCompilerInfo { get; set; } = false;
	public List<UI_VmCompiler> Compilers { get; set; } = new List<UI_VmCompiler>();

	public bool ShowExtraEnviromentsVariables { get; set; }
	public List<KeyValue> ExtraVariables { get; set; } = new List<KeyValue>();

	public bool ShowVmActualEnviromentVariables { get; set; }
	[Newtonsoft.Json.JsonIgnore]
	public List<KeyValue> ActualVariables { get; set; } = new List<KeyValue>();

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
