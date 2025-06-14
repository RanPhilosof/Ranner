using Monitor.Blazor.Converters;
using Monitor.Infra;
using Montior.Blazor.Data;
using System.Collections;

public class EnviromentVariablesResolverTests2
{
    private readonly ParametersTree _tree;
    private readonly Dictionary<ServiceIdentifier, List<(string SourcePath, string Key, string ResolvedValue)>> _mapService;

	[Theory]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit:Ip", "127.0.0.1")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit:Port", "1000")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit:Address", "127.0.0.1:1000")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit2:Ip", "127.0.0.2")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit2:Port", "2000")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit2:Address", "word one 127.0.0.2 word two 2000 word three")]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit3:Address1", null)]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit3:Address2", null)]
    [InlineData(new[] { "EnvironmentVariables" }, "Services:Rabbit3:Address3", null)]

    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp1", "127.0.0.2")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp2", "0.0.0.1")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp3", "127.0.0.2 127.0.0.3")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp4", "0.0.0.3")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp5", "0.0.0.4")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "RabbitIp6", "127.0.0.2:2000,127.0.0.3:3000")]

    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp1", "127.0.0.2")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp2", "0.0.0.1")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp3", "127.0.0.2 127.0.0.3 0.0.0.3")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp4", "0.0.0.1 0.0.0.3")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp5", "0.0.0.3 0.0.0.1")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "GrabbitIp6", "0.0.0.1:0.0.0.1,127.0.0.2 127.0.0.3:127.0.0.2 127.0.0.3")]

    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "MixGrabbitIp10", "127.0.0.1 127.0.0.2")]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "CheckActive_RabbitIp1", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters" }, "CheckActive_RabbitIp2", null)]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k1", "vm1_v1")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k2", "vm1_v2 vm1_v1")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k3", "vm1_v3 127.0.0.2")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k4", "vm1_v4 1000")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k5", null)]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k6", null)]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k7", null)]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k8", "vm1_v8_def")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k9", "vm1_v9_def")]
    [InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1" }, "vm1_k10", "vm1_v10_def")]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k1", "vm2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k2", "vm2_v2 vm2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k3", "vm2_v3 127.0.0.2")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k4", "vm2_v4 1000")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k5", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k6", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k7", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k8", "vm2_v8_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k9", "vm2_v9_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2" }, "vm2_k10", "vm2_v10_def")]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k1", "Ser1_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k2", "Ser1_v2 vm1_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k3", "cannot_reach_vm2")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k4", "Ser1_v4 127.0.0.2")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k5", "Ser1_v5 1000")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k6", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k7", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k8", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k9", "Ser1_v9_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k10", "Ser1_v10_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k11", "Ser1_v11_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k12", "Ser1_v12 Ser1_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k13", "Ser1_v13_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser1" }, "Ser1_k14", "Ser1_v14 Ser2_v1")]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k1", "Ser2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k2", "Ser2_v2 vm1_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k3", "cannot_reach_vm2")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k4", "Ser2_v4 127.0.0.2")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k5", "Ser2_v5 1000")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k6", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k7", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k8", null)]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k9", "Ser2_v9_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k10", "Ser2_v10_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k11", "Ser2_v11_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k12", "Ser2_v12 Ser2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k13", "Ser2_v13_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm1", "Ser2" }, "Ser2_k14", "Ser2_v14 Ser1_v1")]

	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2", "Ser3" }, "Ser1_vm2_k1", "Ser1_vm2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2", "Ser3" }, "Ser1_vm2_k2", "Ser1_vm2_v12 Ser1_vm2_v1")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2", "Ser3" }, "Ser1_vm2_k3", "Ser1_vm2_v13_def")]
	[InlineData(new[] { "EnvironmentVariables", "GlobalParameters", "vm2", "Ser3" }, "Ser1_vm2_k4", "Ser1_vm2_v14 Ser1_v1")]

	public void Should_Resolve_Expected_EnvironmentVariables(String[] whereToCheck, string key, string expected)
    {
        var current = _tree;

        foreach (var nodeName in whereToCheck.Skip(1))
        {
            Assert.True(current.Childs.ContainsKey(nodeName), $"Missing node: {nodeName}");
            current = current.Childs[nodeName];
        }

        Assert.True(current.Parameters.ContainsKey(key), $"Missing parameter: {key}");
        Assert.Equal(expected, current.Parameters[key].ResolvedValue);
    }

	[Theory]
	[InlineData(20, "Ser1", "MinioIp1", "255.255.255.251")]
	[InlineData(20, "Ser1", "MinioIp2", "255.255.255.252")]
	[InlineData(20, "Ser1", "MinioIp3", "255.255.255.253")]
	[InlineData(20, "Ser1", "MinioIp4", "255.255.255.254")]
	public void Show_Final_Variables(int id, string service, string key, string expected)
	{
        var si = new ServiceIdentifier(id, service);
        var res = _mapService[si].Where(x => x.Key == key).FirstOrDefault();
		Assert.Equal(expected, res.ResolvedValue);
	}

	public EnviromentVariablesResolverTests2()
    {
        foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            var key = envVar.Key.ToString();
            Environment.SetEnvironmentVariable(key, null);
        }

        Environment.SetEnvironmentVariable("Services:Rabbit:Ip", "127.0.0.1");
        Environment.SetEnvironmentVariable("Services:Rabbit:Port", "1000");
        Environment.SetEnvironmentVariable("Services:Rabbit:Address", "{env[Services:Rabbit:Ip]}:{env[Services:Rabbit:Port]}");

        Environment.SetEnvironmentVariable("Services:Rabbit2:Ip", "127.0.0.2");
        Environment.SetEnvironmentVariable("Services:Rabbit2:Port", "2000");
        Environment.SetEnvironmentVariable("Services:Rabbit2:Address", "word one {env[Services:Rabbit2:Ip]} word two {env[Services:Rabbit2:Port]} word three");

        Environment.SetEnvironmentVariable("Services:Rabbit3:Ip", "127.0.0.3");
        Environment.SetEnvironmentVariable("Services:Rabbit3:Port", "3000");
        Environment.SetEnvironmentVariable("Services:Rabbit3:Address1", "word one {env[Services:Rabbit3:Ip]} word two {env[Services:Rabbit4:Port]} word three");
        Environment.SetEnvironmentVariable("Services:Rabbit3:Address2", "word one {env[Services:Rabbit4:Ip]} word two {env[Services:Rabbit3:Port]} word three");
        Environment.SetEnvironmentVariable("Services:Rabbit3:Address3", "word one {env[Services:Rabbit4:Ip]} word two {env[Services:Rabbit4:Port]} word three");

        #region Init
        var parametersTreeInitiator = new ParametersTreeInitiator();

        _tree = parametersTreeInitiator.Create(
            new Montior.Blazor.Data.UI_GlobalVariables()
            {
                GlobalVariables = new List<Montior.Blazor.Data.KeyValueComplex>()
                    {
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "g_k1", Value = "g_v1", DefaultValue = "g_v1_def", Description = "" },

                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp1", Value = "{env[Services:Rabbit2:Ip]}", DefaultValue = "0.0.0.0", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp2", Value = "{env[Services:Rabbit20:Ip]}", DefaultValue = "0.0.0.1", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp3", Value = "{env[Services:Rabbit2:Ip]} {env[Services:Rabbit3:Ip]}", DefaultValue = "0.0.0.2", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp4", Value = "{env[Services:Rabbit2:Ip]} {env[Services:Rabbit4:Ip]}", DefaultValue = "0.0.0.3", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp5", Value = "{env[Services:Rabbit4:Ip]} {env[Services:Rabbit2:Ip]}", DefaultValue = "0.0.0.4", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "RabbitIp6", Value = "{env[Services:Rabbit2:Ip]}:{env[Services:Rabbit2:Port]},{env[Services:Rabbit3:Ip]}:{env[Services:Rabbit3:Port]}", DefaultValue = "0.0.0.5", Description = "" },

                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp1", Value = "{glb[RabbitIp1]}", DefaultValue = "0.0.0.0", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp2", Value = "{glb[RabbitIp2]}", DefaultValue = "0.0.0.1", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp3", Value = "{glb[RabbitIp3]} {glb[RabbitIp4]}", DefaultValue = "0.0.0.2", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp4", Value = "{glb[RabbitIp2]} {glb[RabbitIp4]}", DefaultValue = "0.0.0.3", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp5", Value = "{glb[RabbitIp4]} {glb[RabbitIp2]}", DefaultValue = "0.0.0.4", Description = "" },
                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "GrabbitIp6", Value = "{glb[RabbitIp2]}:{glb[RabbitIp2]},{glb[RabbitIp3]}:{glb[RabbitIp3]}", DefaultValue = "0.0.0.5", Description = "" },

                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "MixGrabbitIp10", Value = "{env[Services:Rabbit:Ip]} {glb[GrabbitIp1]}", DefaultValue = "0.0.0.2", Description = "" },

						new Montior.Blazor.Data.KeyValueComplex() { Active = false, Key = "CheckActive_RabbitIp1", Value = "{env[Services:Rabbit2:Ip]}", DefaultValue = "0.0.0.0", Description = "" },
						new Montior.Blazor.Data.KeyValueComplex() { Active = false, Key = "CheckActive_RabbitIp2", Value = "{glb[CheckActive_RabbitIp1]}", DefaultValue = "0.0.0.1", Description = "" },

						new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "MinioIp2", Value = "255.255.255.0", DefaultValue = "0"},
						new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "MinioIp3", Value = "255.255.255.253", DefaultValue = "0"},
					},
            },
            new Montior.Blazor.Data.UI_VmsData()
            {
                VmsDataList = new List<Montior.Blazor.Data.UI_VmData>()
                    {
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm1",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValueComplex>()
                            {
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k1", Value = "vm1_v1" },
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k2", Value = "vm1_v2 {vm[vm1_k1]}" },
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k3", Value = "vm1_v3 {glb[RabbitIp1]}" },
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k4", Value = "vm1_v4 {env[Services:Rabbit:Port]}" },
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k5", Value = "vm1_v5 {env[Ran]}", DefaultValue = null},
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k6", Value = "vm1_v6 {glb[Ran]}", DefaultValue = null},
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k7", Value = "vm1_v7 {vm[Ran]}", DefaultValue = null},
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k8", Value = "vm1_v8 {env[Ran]}", DefaultValue = "vm1_v8_def"},
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k9", Value = "vm1_v9 {glb[Ran]}", DefaultValue = "vm1_v9_def"},
                                new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm1_k10", Value = "vm1_v10 {vm[Ran]}", DefaultValue = "vm1_v10_def"},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "MinioIp4", Value = "255.255.255.254", DefaultValue = "0"},
							}
                        },
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm2",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValueComplex>()
                            {
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k1", Value = "vm2_v1" },
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k2", Value = "vm2_v2 {vm[vm2_k1]}" },
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k3", Value = "vm2_v3 {glb[RabbitIp1]}" },
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k4", Value = "vm2_v4 {env[Services:Rabbit:Port]}" },
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k5", Value = "vm2_v5 {env[Ran]}", DefaultValue = null},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k6", Value = "vm2_v6 {glb[Ran]}", DefaultValue = null},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k7", Value = "vm2_v7 {vm[Ran]}", DefaultValue = null},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k8", Value = "vm2_v8 {env[Ran]}", DefaultValue = "vm2_v8_def"},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k9", Value = "vm2_v9 {glb[Ran]}", DefaultValue = "vm2_v9_def"},
								new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "vm2_k10", Value = "vm2_v10 {vm[Ran]}", DefaultValue = "vm2_v10_def"},
							}
                        }
                    }
            },
                new Montior.Blazor.Data.UI_InstancesData()
                {
                    Instances = new List<Montior.Blazor.Data.UI_Instance>()
                        {
                            {
                                new UI_Instance()
                                {
                                    Name = "Ser1",
                                    VmUniqueName = "vm1",
                                    Id = 20,
                                    ExtraVariables = new List<KeyValueComplex>()
                                    {
									    new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k1", Value = "Ser1_v1" },
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k2", Value = "Ser1_v2 {vm[vm1_k1]}", DefaultValue = "def" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k3", Value = "Ser1_v3 {vm[vm2_k1]}", DefaultValue = "cannot_reach_vm2" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k4", Value = "Ser1_v4 {glb[RabbitIp1]}" },
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k5", Value = "Ser1_v5 {env[Services:Rabbit:Port]}" },
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k6", Value = "Ser1_v6 {env[Ran]}", DefaultValue = null},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k7", Value = "Ser1_v7 {glb[Ran]}", DefaultValue = null},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k8", Value = "Ser1_v8 {vm[Ran]}", DefaultValue = null},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k9", Value = "Ser1_v9 {env[Ran]}", DefaultValue = "Ser1_v9_def"},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k10", Value = "Ser1_v10 {glb[Ran]}", DefaultValue = "Ser1_v10_def"},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k11", Value = "Ser1_v11 {vm[Ran]}", DefaultValue = "Ser1_v11_def"},
								        new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k12", Value = "Ser1_v12 {srv[Ser1(Ser1_k1)]}", DefaultValue = "Ser1_v12_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k13", Value = "Ser1_v12 {srv[Ser1_k01]}", DefaultValue = "Ser1_v13_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_k14", Value = "Ser1_v14 {srv[Ser2(Ser2_k1)]}", DefaultValue = "Ser2_v12_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "MinioIp1", Value = "255.255.255.251", DefaultValue = "0"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "MinioIp2", Value = "255.255.255.252", DefaultValue = "0"},
									}
                                }
                            },
                            {
                                new UI_Instance()
                                {
                                    Name = "Ser2",
                                    VmUniqueName = "vm1",
									Id = 21,
									ExtraVariables = new List<KeyValueComplex>()
                                    {
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k1",  Value = "Ser2_v1" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k2",  Value = "Ser2_v2 {vm[vm1_k1]}", DefaultValue = "def" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k3",  Value = "Ser2_v3 {vm[vm2_k1]}", DefaultValue = "cannot_reach_vm2" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k4",  Value = "Ser2_v4 {glb[RabbitIp1]}" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k5",  Value = "Ser2_v5 {env[Services:Rabbit:Port]}" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k6",  Value = "Ser2_v6 {env[Ran]}", DefaultValue = null},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k7",  Value = "Ser2_v7 {glb[Ran]}", DefaultValue = null},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k8",  Value = "Ser2_v8 {vm[Ran]}", DefaultValue = null},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k9",  Value = "Ser2_v9 {env[Ran]}", DefaultValue = "Ser2_v9_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k10", Value = "Ser2_v10 {glb[Ran]}", DefaultValue = "Ser2_v10_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k11", Value = "Ser2_v11 {vm[Ran]}", DefaultValue = "Ser2_v11_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k12", Value = "Ser2_v12 {srv[Ser2(Ser2_k1)]}", DefaultValue = "Ser2_v12_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k13", Value = "Ser2_v12 {srv[Ser2_k01]}", DefaultValue = "Ser2_v13_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser2_k14", Value = "Ser2_v14 {srv[Ser1(Ser1_k1)]}", DefaultValue = "Ser2_v12_def"},
									}
                                }
                            },
							{
								new UI_Instance()
								{
									Name = "Ser3",
									Id = 22,
									VmUniqueName = "vm2",
									ExtraVariables = new List<KeyValueComplex>()
									{
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k1",  Value = "Ser1_vm2_v1" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k2", Value = "Ser1_vm2_v12 {srv[Ser3(Ser1_vm2_k1)]}", DefaultValue = "Ser1_vm2_v12_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k3", Value = "Ser1_vm2_v12 {srv[Ser2_k01]}", DefaultValue = "Ser1_vm2_v13_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k4", Value = "Ser1_vm2_v14 {srv[Ser1(Ser1_k1)]}", DefaultValue = "Ser1_vm2_v12_def"},
									}
								}
							},
														{
								new UI_Instance()
								{
									Name = "Ser2",
									Id = 22,
									VmUniqueName = "vm2",
									SessionName = "Secondary",
									ExtraVariables = new List<KeyValueComplex>()
									{
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k1",  Value = "Ser1_vm2_v1" },
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k2", Value = "Ser1_vm2_v12 {srv[Ser1(Ser1_vm2_k1)]}", DefaultValue = "Ser1_vm2_v12_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k3", Value = "Ser1_vm2_v12 {srv[Ser2_k01]}", DefaultValue = "Ser1_vm2_v13_def"},
										new Montior.Blazor.Data.KeyValueComplex() {  Active = true, Key = "Ser1_vm2_k4", Value = "Ser1_vm2_v14 {srv[Ser1(Ser1_k1)]}", DefaultValue = "Ser1_vm2_v12_def"},
									}
								}
							},
						}
                });
        #endregion Init

        ParametersTreeInitiator.ResolveAllParameters(_tree);

        _mapService = ParametersTreeInitiator.BuildResolvedMapPerService(_tree);

	} 
}