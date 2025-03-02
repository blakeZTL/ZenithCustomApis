using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CustomAPIs.Services
{
    public interface IRoleService
    {
        EntityCollection RetrieveRoles(
            IOrganizationService service,
            string[] roles,
            ITracingService tracer
        );
    }

    public class RoleService : IRoleService
    {
        public EntityCollection RetrieveRoles(
            IOrganizationService service,
            string[] roles,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression roleQuery = new QueryExpression("role")
                {
                    ColumnSet = new ColumnSet("roleid", "name", "businessunitid"),
                };
                roleQuery.Criteria.AddCondition("name", ConditionOperator.In, roles);
                return service.RetrieveMultiple(roleQuery);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error retrieving roles: {ex.Message}");
                throw;
            }
        }
    }
}
