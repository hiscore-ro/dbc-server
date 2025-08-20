using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DbcServer.IntegrationTests.TestFixtures;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override the DBF path for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbfPath"] = "tmp"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Additional test-specific services can be configured here
        });

        builder.UseEnvironment("Testing");
    }
}