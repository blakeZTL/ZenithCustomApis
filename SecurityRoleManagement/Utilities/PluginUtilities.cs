using System;
using System.Collections.Generic;
using System.Linq;
using CustomAPIs.Services;
using Microsoft.Xrm.Sdk;

namespace CustomAPIs.Utilities
{
    public static class PluginUtilities
    {
        public static void SetOutputParameters(
            Dictionary<string, object> outputParameters,
            IPluginExecutionContext context,
            ITracingService tracer
        )
        {
            foreach (var param in outputParameters)
            {
                context.OutputParameters[param.Key] = param.Value;
            }
            tracer.Trace("Output parameters set.");
        }

        public static EntityCollection RetrieveEntities(
            IOrganizationService service,
            string[] ids,
            Func<IOrganizationService, string[], ITracingService, EntityCollection> retrieveMethod,
            ITracingService tracer
        )
        {
            try
            {
                return retrieveMethod(service, ids, tracer);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error: {ex.Message}");
                throw;
            }
        }

        public static List<Guid> GetDistinctEntityIds(
            EntityCollection entityCollection,
            string attributeName
        )
        {
            return entityCollection
                .Entities.Select(e => e.GetAttributeValue<Guid>(attributeName))
                .Distinct()
                .ToList();
        }

        public static EntityCollection RetrieveRoles(
            IRoleService roleService,
            IOrganizationService service,
            string[] roles,
            ITracingService tracer,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;
            EntityCollection rolesCollection;
            try
            {
                rolesCollection = roleService.RetrieveRoles(service, roles, tracer);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
                return null;
            }
            if (rolesCollection.Entities.Count < roles.Length)
            {
                errorMessage =
                    $"Not all roles were found. ({rolesCollection.Entities.Count}/{roles.Length})";
                return null;
            }
            tracer.Trace($"Retrieved {rolesCollection.Entities.Count} roles");
            return rolesCollection;
        }
    }
}
