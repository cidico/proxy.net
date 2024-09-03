using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Proxy;

public static class ExtensionMethods
{
    public static IHostBuilder Setup(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ProxyOptions>(context.Configuration.GetSection("ProxyOptions"));
                services.AddSingleton<IUrlMatcher, UrlMatcher>();
                services.AddSingleton<ProxyService>();
            });
    }
}