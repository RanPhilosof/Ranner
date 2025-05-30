using Monitor.Blazor.Converters;
using Monitor.Infra;
using Montior.Blazor.Data;

public class EnviromentVariablesResolverTests2
{
    private readonly ParametersTree _tree;

    [Theory]
    [InlineData("Services:Rabbit:Ip", "127.0.0.1")]
    [InlineData("Services:Rabbit:Port", "1000")]
    [InlineData("Services:Rabbit:Address", "127.0.0.1:1000")]
    [InlineData("Services:Rabbit2:Ip", "127.0.0.2")]
    [InlineData("Services:Rabbit2:Port", "2000")]
    [InlineData("Services:Rabbit2:Address", "word one 127.0.0.2 word two 2000 word three")]
    public void Should_Resolve_Expected_EnvironmentVariables(string key, string expected)
    {
        Assert.Equal(expected, _tree.Parameters[key].ResolvedValue);
    }

    [Theory]
    [InlineData("Services:Rabbit3:Address1")]
    [InlineData("Services:Rabbit3:Address2")]
    [InlineData("Services:Rabbit3:Address3")]
    public void Should_Fail_To_Resolve_Missing_EnvironmentVariables(string key)
    {
        Assert.Null(_tree.Parameters[key].ResolvedValue);
    }

    public EnviromentVariablesResolverTests2()
    {
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
                    },
            },
            new Montior.Blazor.Data.UI_VmsData()
            {
                VmsDataList = new List<Montior.Blazor.Data.UI_VmData>()
                    {
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm1",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValue>()
                            {
                                new Montior.Blazor.Data.KeyValue() { Key = "vm1_k1", Value = "vm1_v1" },
                            }
                        },
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm2",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValue>()
                            {
                                new Montior.Blazor.Data.KeyValue() { Key = "vm2_k1", Value = "vm2_v1" },
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
                                    ExtraVariables = new List<KeyValue>()
                                    {
                                        new Montior.Blazor.Data.KeyValue() { Key = "Ser1_k1", Value = "Ser1_v1" },
                                    }
                                }
                            },
                            {
                                new UI_Instance()
                                {
                                    Name = "Ser2",
                                    VmUniqueName = "vm2",
                                    ExtraVariables = new List<KeyValue>()
                                    {
                                        new Montior.Blazor.Data.KeyValue() { Key = "Ser2_k1", Value = "Ser2_v1" },
                                    }
                                }
                            },
                        }
                });
        #endregion Init

        ParametersTreeInitiator.ResolveAllParameters(_tree);
    }

    [Fact]
    public void ServicesResolverTest()
    {
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

        var parametersTreeInitiator = new ParametersTreeInitiator();

        var tree = parametersTreeInitiator.Create(
            new Montior.Blazor.Data.UI_GlobalVariables()
                {                
                    GlobalVariables = new List<Montior.Blazor.Data.KeyValueComplex>()
                    {
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

                        new Montior.Blazor.Data.KeyValueComplex() { Active = true, Key = "MixGrabbitIp10", Value = "{env[RabbitIp1]} {glb[GrabbitIp1]}", DefaultValue = "0.0.0.2", Description = "" },
                    },
                },
            new Montior.Blazor.Data.UI_VmsData()
                {
                    VmsDataList = new List<Montior.Blazor.Data.UI_VmData>()
                    {
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm1",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValue>()
                            {
                                new Montior.Blazor.Data.KeyValue() { Key = "a1", Value = "b" },
                                new Montior.Blazor.Data.KeyValue() { Key = "a2", Value = "b" },
                                new Montior.Blazor.Data.KeyValue() { Key = "a3", Value = "b" },
                            }
                        },
                        new Montior.Blazor.Data.UI_VmData()
                        {
                            UniqueName = "vm2",
                            ExtraVariables = new List<Montior.Blazor.Data.KeyValue>()
                            {
                                new Montior.Blazor.Data.KeyValue() { Key = "a1", Value = "b" },
                                new Montior.Blazor.Data.KeyValue() { Key = "a2", Value = "b" },
                                new Montior.Blazor.Data.KeyValue() { Key = "a3", Value = "b" },
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
                                    ExtraVariables = new List<KeyValue>()
                                    {
                                        new Montior.Blazor.Data.KeyValue() { Key = "a", Value = "b" },
                                    }
                                } 
                            },
                            {
                                new UI_Instance()
                                {
                                    Name = "Ser2",
                                    VmUniqueName = "vm2",
                                    ExtraVariables = new List<KeyValue>()
                                    {
                                        new Montior.Blazor.Data.KeyValue() { Key = "a", Value = "b" },
                                    }
                                }
                            },
                        }
                    });


        ParametersTreeInitiator.ResolveAllParameters(tree);

        Assert.Equal("100.100.100.101:8001,100.100.100.102:8002,100.100.100.103:8003,100.100.100.104:8004", tree.ToString());
    }
}