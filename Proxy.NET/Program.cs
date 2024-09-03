using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proxy;

class Program
{
    private static HashSet<string> AllowedDomains;

    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .Setup()
            .Build();

        var proxyService = host.Services.GetRequiredService<ProxyService>();
        await proxyService.RunAsync();
    }
}