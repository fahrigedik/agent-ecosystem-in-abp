using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentEcosystem.Data;

public class AgentEcosystemDbContextFactory : IDesignTimeDbContextFactory<AgentEcosystemDbContext>
{
    public AgentEcosystemDbContext CreateDbContext(string[] args)
    {
        AgentEcosystemGlobalFeatureConfigurator.Configure();
        AgentEcosystemModuleExtensionConfigurator.Configure();

        AgentEcosystemEfCoreEntityExtensionMappings.Configure();
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<AgentEcosystemDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new AgentEcosystemDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}