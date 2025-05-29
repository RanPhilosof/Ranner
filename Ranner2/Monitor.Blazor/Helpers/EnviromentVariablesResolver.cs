using System.Text.RegularExpressions;

namespace Monitor.Blazor.Helpers
{
	public class EnviromentVariablesResolver
	{
		public EnviromentVariablesResolver() { }

		public string ResloveValue(string value, Dictionary<string, Tuple<string,string>> globalVariables)
		{
			if (value.Contains("%") || value.Contains("{"))
			{
				var needResolve = Extract(value);
				if (needResolve.Count > 0)
				{
					foreach (var item in needResolve)
					{
						var i = item.Replace("%", "").Replace("{", "").Replace("}", "");
						if (globalVariables.ContainsKey(i))
							globalVariables[i]

						var result = ResloveValue(item, globalVariables);
						value = value.Replace(item, result);
					}
				}
			}

			return value;
		}

		private List<string> Extract(string value)
		{
			var hashSet = new HashSet<string>();

			var matches = Regex.Matches(value, @"%(\w+)%|{(\w+)}", RegexOptions.IgnoreCase).ToList();
			foreach (Match match in matches)
			{
				if (match.Groups[1].Success)
				{
					var v = $"%{match.Groups[1].Value}%";

					if (!hashSet.Contains(v))
						hashSet.Add(v);
				}
				else if (match.Groups[2].Success)
				{
					var v = "{" + $"{match.Groups[2].Value}" + "}";

					if (!hashSet.Contains(v))
						hashSet.Add(v);
				}
			}
			
			return hashSet.ToList();
		}
	}
}
