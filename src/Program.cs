using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LocalDownloaderBot;

internal class Program
{
    public static async Task Main(string[] args)
    {
        ThreadPool.SetMaxThreads(16, 16);
        ThreadPool.SetMinThreads(1, 1);
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) => services.AddHostedService<BotService>());
}