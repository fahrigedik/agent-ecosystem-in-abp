using Volo.Abp.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace AgentEcosystem.Data;

public class AgentEcosystemDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public AgentEcosystemDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        
        /* We intentionally resolving the AgentEcosystemDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AgentEcosystemDbContext>()
            .Database
            .MigrateAsync();

    }
}
