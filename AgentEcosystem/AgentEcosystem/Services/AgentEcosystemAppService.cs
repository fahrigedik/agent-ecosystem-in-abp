using Volo.Abp.Application.Services;
using AgentEcosystem.Localization;

namespace AgentEcosystem.Services;

/* Inherit your application services from this class. */
public abstract class AgentEcosystemAppService : ApplicationService
{
    protected AgentEcosystemAppService()
    {
        LocalizationResource = typeof(AgentEcosystemResource);
    }
}