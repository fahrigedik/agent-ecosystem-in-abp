using AgentEcosystem.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace AgentEcosystem.Permissions;

public class AgentEcosystemPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AgentEcosystemPermissions.GroupName);


        
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AgentEcosystemPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AgentEcosystemResource>(name);
    }
}
