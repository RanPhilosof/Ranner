using Monitor.Infra;
using System;
using System.Collections.Generic;
using Xunit;

public class EnviromentVariablesResolverTests
{
    private readonly EnviromentVariablesResolver _resolver = new EnviromentVariablesResolver();

    [Fact]
    public void ServicesResolverTest()
    {
        var result = _resolver.ResloveValue(
            "{Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003", 
            new() {
                { "Services:S1:Ip", Tuple.Create("100.100.100.101", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("100.100.100.102", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("100.100.100.103", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("100.100.100.104", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            }, 
            new(), 
            out bool _);

        Assert.Equal("100.100.100.101:8001,100.100.100.102:8002,100.100.100.103:8003,100.100.100.104:8004", result);
    }

    [Fact]
    public void ServicesResolverTest2()
    {
        var result = _resolver.ResloveValue(
            "{Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003",
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4%", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "Rabbit_Ip1", "1.1.1.1" },
                { "Rabbit_Ip2", "1.1.1.2" },
                { "Rabbit_Ip3", "1.1.1.3" },
                { "Rabbit_Ip4", "1.1.1.4" },
            },
            out bool _);

        Assert.Equal("1.1.1.1:8001,1.1.1.2:8002,1.1.1.3:8003,1.1.1.4:8004", result);
    }

    [Fact]
    public void ServicesResolverTest3()
    {
        var result = _resolver.ResloveValue(
            "{Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003",
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4%", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "Rabbit_Ip1", "1.1.1.1" },
                { "Rabbit_Ip2", "1.1.1.2" },
                { "Rabbit_Ip3", "1.1.1.3" },                
            },
            out bool _);

        Assert.Equal("1.1.1.1:8001,1.1.1.2:8002,1.1.1.3:8003,0.0.0.4:8004", result);
    }

    [Fact]
    public void ServicesResolverTest4()
    {
        var defVal = "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003";

        var result = _resolver.ResloveValue(
            "%A% {Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003",
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4%", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "Rabbit_Ip1", "1.1.1.1" },
                { "Rabbit_Ip2", "1.1.1.2" },
                { "Rabbit_Ip3", "1.1.1.3" },
            },
            out bool _);

        Assert.Equal(defVal, result);
    }

    [Fact]
    public void ServicesResolverTest5()
    {
        var defVal = "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003";

        var result = _resolver.ResloveValue(
            "%A% {Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003",
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4% ds", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "Rabbit_Ip1", "1.1.1.1" },
                { "Rabbit_Ip2", "1.1.1.2" },
                { "Rabbit_Ip3", "1.1.1.3" },
            },
            out bool _);

        Assert.Equal(defVal, result);
    }

    [Fact]
    public void ServicesResolverTest6()
    {
        var defVal = "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003";

        var result = _resolver.ResloveValue(
            "%A% {Services:S1:Ip}:{Services:S1:DPort},{Services:S2:Ip}:{Services:S2:DPort},{Services:S3:Ip}:{Services:S3:DPort},{Services:S4:Ip}:{Services:S4:DPort}", "127.0.0.1:1000,127.0.0.1:1001,127.0.0.1:1002,127.0.0.1:1003",
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4% ds", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "Rabbit_Ip1", "1.1.1.1" },
                { "Rabbit_Ip2", "1.1.1.2" },
                { "Rabbit_Ip3", "1.1.1.3" },
            },
            out bool _);

        Assert.Equal(defVal, result);
    }

    [Fact]
    public void ServicesResolverTest7()
    {
        var defVal = "127.0.0.1:1000,127.0.0.1:1001";

        var result = _resolver.ResloveValue(
            "%Services:S1:Ip%:%Services:S1:DPort%,%Services:S2:Ip%:%Services:S2:DPort%", defVal,
            new() {
                { "Services:S1:Ip", Tuple.Create("%Rabbit_Ip1%", "0.0.0.1") },
                { "Services:S2:Ip", Tuple.Create("%Rabbit_Ip2%", "0.0.0.2") },
                { "Services:S3:Ip", Tuple.Create("%Rabbit_Ip3%", "0.0.0.3") },
                { "Services:S4:Ip", Tuple.Create("%Rabbit_Ip4% ds", "0.0.0.4") },

                { "Services:S1:DPort", Tuple.Create("8001", "1001") },
                { "Services:S2:DPort", Tuple.Create("8002", "1002") },
                { "Services:S3:DPort", Tuple.Create("8003", "1003") },
                { "Services:S4:DPort", Tuple.Create("8004", "1004") },
            },
            new()
            {
                { "[Services:S1:Env]:DPort1", "1000" },
                { "Services:S2:Env:DPort1", "1100" },
                { "Services:S3:Env:DPort1", "1200" },
            },
            out bool _);

        Assert.Equal(defVal, result);
    }

    [Fact]
    public void ReturnsDefaultValue_WhenValueIsNull()
    {
        var result = _resolver.ResloveValue(null, "default", new(), new(), out bool _);
        Assert.Equal("default", result);
    }

    [Fact]
    public void ReturnsDefaultValue_WhenValueIsEmpty()
    {
        var result = _resolver.ResloveValue("", "default", new(), new(), out bool _);
        Assert.Equal("default", result);
    }

    [Fact]
    public void ResolvesEnvironmentVariable_WithPercentSyntax()
    {
        var env = new Dictionary<string, string>
        {
            { "FOO", "bar" }
        };

        var result = _resolver.ResloveValue("Value is %FOO%", "default", new(), env, out bool _);
        Assert.Equal("Value is bar", result);
    }

    [Fact]
    public void ResolvesEnvironmentVariable_WithCurlyBracesSyntax()
    {
        var env = new Dictionary<string, string>
        {
            { "FOO", "123" }
        };

        var result = _resolver.ResloveValue("Hello {FOO}", "default", new(), env, out bool _);
        Assert.Equal("default", result);
    }

    [Fact]
    public void ResolvesGlobalVariable_WhenEnvironmentNotFound()
    {
        var globals = new Dictionary<string, Tuple<string, string>>
        {
            { "%MYVAR%", Tuple.Create("globalVal", "fallback") }
        };

        var result = _resolver.ResloveValue("Use %MYVAR%", "default", globals, new(), out bool _);
        Assert.Equal("default", result);
    }

    [Fact]
    public void ResolvesNestedVariables()
    {
        var env = new Dictionary<string, string>
        {
            { "LEVEL1", "%LEVEL2%" },
            { "LEVEL2", "Final" }
        };

        var result = _resolver.ResloveValue("%LEVEL1%", "default", new(), env, out bool _);
        Assert.Equal("Final", result);
    }

    [Fact]
    public void ReplacesMultipleVariables()
    {
        var env = new Dictionary<string, string>
        {
            { "A", "1" },
            { "B", "2" }
        };

        var result = _resolver.ResloveValue("A=%A% B=%B%", "default", new(), env, out bool _);
        Assert.Equal("A=1 B=2", result);
    }

    [Fact]
    public void IgnoresUnmatchedVariables()
    {
        var result = _resolver.ResloveValue("Path: %NOTFOUND%", "default", new(), new(), out bool _);
        Assert.Equal("default", result); // replaced with null -> recursive call returns defaultVal = empty string
    }
}