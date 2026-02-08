using Microsoft.Extensions.Localization;
using AgentEcosystem.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AgentEcosystem;

[Dependency(ReplaceServices = true)]
public class AgentEcosystemBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AgentEcosystemResource> _localizer;

    public AgentEcosystemBrandingProvider(IStringLocalizer<AgentEcosystemResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}