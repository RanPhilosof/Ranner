using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Monitor.Infra
{
	public class EnviromentVariablesResolver
	{
		public EnviromentVariablesResolver() { }

		public string ResloveValue(string value, string defaultValue, 
			Dictionary<string, Tuple<string,string>> globalVariables,
            Dictionary<string, string> envVariables, out bool forceRetDefault)
		{
			forceRetDefault = false;

            if (string.IsNullOrEmpty(value))
			{
				forceRetDefault = true;
                return defaultValue;
			}			

			if (forceRetDefault)
                return defaultValue;

			if (value.Contains("%") || value.Contains("{"))
			{
				var needResolve = Extract(value);
				if (needResolve.Count > 0)
				{
					foreach (var item in needResolve)
					{
						string resVal = null;
						string defaultVal = null;

						if (item.Item1 == ResolveType.EnvironmentVar)
						{
							if (envVariables.ContainsKey(item.Item3))
							{
								resVal = envVariables[item.Item3];
							}
							else
							{
								if (needResolve.Count != 1 || value != needResolve[0].Item2)
									forceRetDefault = true;

								return defaultValue;
							}

                            defaultVal = string.Empty;
                        }
						else if (item.Item1 == ResolveType.GlobalVar)
                        {
							if (globalVariables.ContainsKey(item.Item3))
							{
								resVal = globalVariables[item.Item3].Item1;
                                defaultVal = globalVariables[item.Item3].Item2;
                            }
							else
							{
                                forceRetDefault = true;
                                if (forceRetDefault)
                                    return defaultValue;
                            }
                        }

                        var result = ResloveValue(resVal, defaultVal, globalVariables, envVariables, out forceRetDefault);

						if (forceRetDefault)
							return defaultVal;

						value = value.Replace(item.Item2, result);
					}
				}
			}

			return value;
		}

		private List<Tuple<ResolveType, string, string>> Extract(string value)
		{
			var list = new List<Tuple<ResolveType, string, string>>();

            var matches = Regex.Matches(value, @"%([^%]+)%|{([^}]+)}", RegexOptions.IgnoreCase).ToList();
			foreach (Match match in matches)
			{
				if (match.Groups[1].Success)
				{
					var v = $"%{match.Groups[1].Value}%";

					list.Add(Tuple.Create(ResolveType.EnvironmentVar, v, match.Groups[1].Value));
				}
				else if (match.Groups[2].Success)
				{
					var v = "{" + $"{match.Groups[2].Value}" + "}";

                    list.Add(Tuple.Create(ResolveType.GlobalVar, v, match.Groups[2].Value));
                }
			}

			return list.DistinctBy(x => x.Item2).ToList();
		}
	}

	public enum ResolveType
	{
		None,
		EnvironmentVar,
		GlobalVar,
	}

	public class ParametersTree
	{
		public string Name;
        public string TypeName;
		public string SessionName;
		public int Id;

        public ParametersTree Parent;
		public Dictionary<string, ParametersTree> Childs = new Dictionary<string, ParametersTree>();        
		public Dictionary<string, Parameters> Parameters = new Dictionary<string, Parameters>();
    }

	public class Parameters
	{
		public bool IsActive { get; set; }
		public string Key { get; set; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
		public string ResolvedValue { get; set; } = null;

        public Parameters Clone()
		{
			var clone = new Parameters();

			clone.IsActive = IsActive;
			clone.Key = Key;
			clone.Value = Value;
            clone.DefaultValue = DefaultValue;
            clone.Description = Description;

			return clone;
		}
    }
}
