using Monitor.Infra;
using Montior.Blazor.Data;
using System.Collections;
using System.Text.RegularExpressions;

namespace Monitor.Blazor.Converters
{
    public class ParametersTreeInitiator
    {
        public const string envTypeName = "EnvironmentVariable";
        public const string globalTypeName = "GlobalParameters";
        public const string vmTypeName = "VmParameters";
        public const string srvTypeName = "ServiceParameters";

        public const string envTemplate = "env[";
        public const string globalTemplate = "glb[";
        public const string sessionTemplate = "ses[";
        public const string vmTemplate = "vm[";
        public const string serviceTemplate = "srv[";

        public ParametersTree Create(
            UI_GlobalVariables uiGlobalVariables,
            UI_VmsData uiVmsData,
            UI_InstancesData uiInstanceData)
        {   
            var root = new ParametersTree();

            root.Name = envTypeName;
            root.TypeName = envTypeName;

            root.Parent = null;

            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                var key = envVar.Key.ToString();
                var value = envVar.Value.ToString();

                root.Parameters.Add(key, new Parameters() { IsActive = true, Key = key, Value = value, DefaultValue = null, Description = "" });
            }

            var globalParametersTree = new ParametersTree() { Name = globalTypeName, TypeName = globalTypeName };
            globalParametersTree.Parent = root;
            root.Childs.Add(globalParametersTree.Name, globalParametersTree);

            foreach (var globParam in uiGlobalVariables.GlobalVariables)
                globalParametersTree.Parameters.Add(globParam.Key, 
                    new Parameters() { 
                        IsActive = globParam.Active,
                        Key = globParam.Key,
                        Value = globParam.Value,
                        DefaultValue = globParam.DefaultValue,
                        Description = globParam.Description
                    });

            foreach (var vmParams in uiVmsData.VmsDataList)
            {
                var vmParametersTree = new ParametersTree() { Name = vmParams.UniqueName, TypeName = vmTypeName };
                vmParametersTree.Parent = globalParametersTree;

                foreach (var vmParam in vmParams.ExtraVariables)
                {
                    vmParametersTree.Parameters.Add(vmParam.Key,
					    new Parameters()
					    {
					    	IsActive = vmParam.Active,
					    	Key = vmParam.Key,
					    	Value = vmParam.Value,
					    	DefaultValue = vmParam.DefaultValue,
					    	Description = vmParam.Description
					    });

                }
                
                globalParametersTree.Childs.Add(vmParametersTree.Name, vmParametersTree);
            }

            foreach (var instanceParams in uiInstanceData.Instances)
            {
                var instanceParametersTree = new ParametersTree() { Name = instanceParams.Name, TypeName = srvTypeName };                

                foreach (var instanceParam in instanceParams.ExtraVariables)
                {
                    instanceParametersTree.Parameters.Add(instanceParam.Key,
                            new Parameters()
                            {
                                IsActive = instanceParam.Active,
                                Key = instanceParam.Key,
                                Value = instanceParam.Value,
                                DefaultValue = instanceParam.DefaultValue,
                                Description = instanceParam.Description,
                            });

                }
                
                globalParametersTree.Childs[instanceParams.VmUniqueName].Childs.Add(instanceParametersTree.Name, instanceParametersTree);
                instanceParametersTree.Parent = globalParametersTree.Childs[instanceParams.VmUniqueName];
            }

            return root;
        }

        public static void ResolveAllParameters(ParametersTree root)
        {
            TraverseAndResolve(root, root);
        }

        private static void TraverseAndResolve(ParametersTree node, ParametersTree root)
        {
            foreach (var param in node.Parameters.Values)
            {
                param.ResolvedValue = ResolveValue(param.Value, node, root, param.DefaultValue);
            }

            foreach (var child in node.Childs.Values)
            {
                TraverseAndResolve(child, root);
            }
        }

        private static string ResolveValue(string value, ParametersTree currentNode, ParametersTree root, string defaultValue)
        {
            string pattern = @"\{(env|glb|vm|srv)\[([^\[\]()]+)(?:\(([^)]+)\))?\]\}";
            bool failed = false;

            string result = Regex.Replace(value, pattern, match =>
            {
                string type = match.Groups[1].Value;
                string varOrService = match.Groups[2].Value;
                string nestedVar = match.Groups[3].Success ? match.Groups[3].Value : null;

                string resolved = type switch
                {
                    "env" => ResolveFromType(envTypeName, varOrService, currentNode),
                    "glb" => ResolveFromType(globalTypeName, varOrService, currentNode),
                    "vm" => ResolveFromVm(varOrService, currentNode),
                    "srv" => ResolveFromService(varOrService, nestedVar, currentNode),
                    _ => null
                };

                if (resolved == null)
                {
                    failed = true;
                }

                return resolved ?? ""; // placeholder ignored if failed
            });

            return failed ? defaultValue : result;
        }

        private static string ResolveFromType(string typeName, string key, ParametersTree current)
        {
            var node = current;
            while (node != null)
            {
                if (node.TypeName == typeName && node.Parameters.TryGetValue(key, out var param))
                {
                    return ResolveValue(param.Value, node, GetRoot(node), param.DefaultValue);
                }
                node = node.Parent;
            }

            return null;
        }

        private static ParametersTree GetRoot(ParametersTree node)
        {
            while (node.Parent != null)
                node = node.Parent;
            return node;
        }

        private static string ResolveFromVm(string key, ParametersTree current)
        {
            var vmNode = current;
            while (vmNode != null && vmNode.TypeName != vmTypeName)
            {
                vmNode = vmNode.Parent;
            }

            if (vmNode != null && vmNode.Parameters.TryGetValue(key, out var param))
            {
                return ResolveValue(param.Value, vmNode, vmNode, param.DefaultValue); // recursive resolve
            }

            return null;
        }

        private static string ResolveFromService(string serviceName, string key, ParametersTree current)
        {
            if (current.TypeName != srvTypeName) return null;

            var vmNode = current.Parent;
            if (vmNode == null || vmNode.TypeName != vmTypeName) return null;

            if (vmNode.Childs.TryGetValue(serviceName, out var serviceNode) &&
                serviceNode.TypeName == srvTypeName &&
                serviceNode.Parameters.TryGetValue(key, out var param))
            {
                return ResolveValue(param.Value, serviceNode, serviceNode, param.DefaultValue); // recursive resolve
            }

            return null;
        }

        private static string GetDefaultValueForMatch(string type, string key, string nestedKey, ParametersTree current)
        {
            return type switch
            {
                "env" => GetDefault(envTypeName, key, current),
                "glb" => GetDefault(globalTypeName, key, current),
                "vm" => GetVmDefault(key, current),
                "srv" => GetSrvDefault(key, nestedKey, current),
                _ => null
            };
        }

        private static string GetDefault(string typeName, string key, ParametersTree current)
        {
            var node = current;
            while (node != null)
            {
                if (node.TypeName == typeName && node.Parameters.TryGetValue(key, out var param))
                {
                    return param.DefaultValue;
                }
                node = node.Parent;
            }
            return null;
        }

        private static string GetVmDefault(string key, ParametersTree current)
        {
            var node = current;
            while (node != null && node.TypeName != vmTypeName)
            {
                node = node.Parent;
            }

            if (node != null && node.Parameters.TryGetValue(key, out var param))
            {
                return param.DefaultValue;
            }

            return null;
        }

        private static string GetSrvDefault(string serviceName, string key, ParametersTree current)
        {
            if (current.TypeName != srvTypeName) return null;
            var vmNode = current.Parent;
            if (vmNode == null || vmNode.TypeName != vmTypeName) return null;

            if (vmNode.Childs.TryGetValue(serviceName, out var serviceNode) &&
                serviceNode.TypeName == srvTypeName &&
                serviceNode.Parameters.TryGetValue(key, out var param))
            {
                return param.DefaultValue;
            }

            return null;
        }
    }
}
