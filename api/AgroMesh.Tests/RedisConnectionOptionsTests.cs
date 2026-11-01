using System.Net;
using System.Reflection;
using AgroMesh.Infrastructure;
using StackExchange.Redis;

namespace AgroMesh.Tests;

public class RedisConnectionOptionsTests
{
    private static ConfigurationOptions Build(string connection)
    {
        var method = typeof(DependencyInjection).GetMethod("BuildRedisOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            return (ConfigurationOptions)method.Invoke(null, new object[] { connection })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    [Theory]
    [InlineData("redis:6379", "redis", 6379)]       // docker-compose: a host that is literally called "redis"
    [InlineData("localhost:6379", "localhost", 6379)]
    [InlineData("cache.internal:6380,password=pw", "cache.internal", 6380)]
    public void HostPortStrings_AreParsedAsHostAndPort(string connection, string host, int port)
    {
        var options = Build(connection);

        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(options.EndPoints));
        Assert.Equal(host, endpoint.Host);
        Assert.Equal(port, endpoint.Port);
        Assert.False(options.AbortOnConnectFail);
    }

    [Fact]
    public void RedisUrl_IsParsedWithUserPasswordAndPort()
    {
        var options = Build("redis://default:s3cret@red-abc.render.com:6380");

        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(options.EndPoints));
        Assert.Equal("red-abc.render.com", endpoint.Host);
        Assert.Equal(6380, endpoint.Port);
        Assert.Equal("default", options.User);
        Assert.Equal("s3cret", options.Password);
        Assert.False(options.Ssl);
    }

    [Fact]
    public void RedissUrl_EnablesSsl_AndDefaultsThePort()
    {
        var options = Build("rediss://:pw@secure.example.com");

        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(options.EndPoints));
        Assert.Equal("secure.example.com", endpoint.Host);
        Assert.Equal(6379, endpoint.Port);
        Assert.True(options.Ssl);
        Assert.Equal("pw", options.Password);
    }
}
