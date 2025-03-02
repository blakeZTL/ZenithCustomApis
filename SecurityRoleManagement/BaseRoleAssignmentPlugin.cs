using System;
using System.Collections.Generic;
using CustomAPIs.Services;
using CustomAPIs.Utilities;
using Microsoft.Xrm.Sdk;

namespace CustomAPIs
{
    public abstract class BaseRoleAssignmentPlugin : PluginBase
    {
        protected readonly IRoleService _roleService;
        protected readonly IRoleAssignmentService _roleAssignmentService;

        protected BaseRoleAssignmentPlugin(
            Type pluginType,
            IRoleService roleService,
            IRoleAssignmentService roleAssignmentService
        )
            : base(pluginType)
        {
            _roleService = roleService;
            _roleAssignmentService = roleAssignmentService;
        }

        protected void SetOutputParameters(
            OutputParameters outputParameters,
            IPluginExecutionContext context,
            ITracingService tracer,
            string wasSuccessfulKey,
            string errorMessageKey
        )
        {
            PluginUtilities.SetOutputParameters(
                outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                context,
                tracer
            );
        }

        protected EntityCollection RetrieveRoles(
            IOrganizationService service,
            string[] roles,
            ITracingService tracer,
            out string errorMessage
        )
        {
            return PluginUtilities.RetrieveRoles(
                _roleService,
                service,
                roles,
                tracer,
                out errorMessage
            );
        }

        protected EntityCollection RetrieveEntities(
            IOrganizationService service,
            string[] ids,
            Func<IOrganizationService, string[], ITracingService, EntityCollection> retrieveMethod,
            ITracingService tracer
        )
        {
            return PluginUtilities.RetrieveEntities(service, ids, retrieveMethod, tracer);
        }

        protected List<Guid> GetDistinctEntityIds(
            EntityCollection entityCollection,
            string attributeName
        )
        {
            return PluginUtilities.GetDistinctEntityIds(entityCollection, attributeName);
        }

        protected abstract void AssignOrRemoveRoles(
            IOrganizationService service,
            Guid entityId,
            List<Entity> rolesToAssign,
            ITracingService tracer,
            bool willAssign
        );
    }
}
