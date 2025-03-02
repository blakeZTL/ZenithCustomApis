using System;
using System.Collections.Generic;
using System.Linq;
using CustomAPIs.Services;
using CustomAPIs.Utilities;
using Microsoft.Xrm.Sdk;

namespace CustomAPIs.SecurityRoleManagement
{
    public class ManageUserRoleAssignment : BaseRoleAssignmentPlugin
    {
        private readonly ISystemUserService _systemUserService;

        public ManageUserRoleAssignment()
            : base(typeof(ManageUserRoleAssignment), new RoleService(), new RoleAssignmentService())
        {
            _systemUserService = new SystemUserService();
        }

        protected override void ExecuteCdsPlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
                throw new InvalidPluginExecutionException("Plugin Context was null");

            var context = localPluginContext.PluginExecutionContext;
            var sysService = localPluginContext.SystemUserService;
            var tracer = localPluginContext.TracingService;

            OutputParameters outputParameters = new OutputParameters(false, "None");
            string wasSuccessfulKey = "zen_ManageUserRoleAssignments_WasSuccessful";
            string errorMessageKey = "zen_ManageUserRoleAssignments_ErrorMessage";

            SetOutputParameters(
                outputParameters,
                context,
                tracer,
                wasSuccessfulKey,
                errorMessageKey
            );

            string errorMessage;

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_RoleNames",
                out string[] roles
            );
            if (roles == null || roles.Length == 0)
            {
                errorMessage = "No roles to manage for users";
                tracer.Trace(errorMessage);
                outputParameters.ErrorMessage = errorMessage;
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }
            tracer.Trace($"Assigning {roles.Length} roles to the team");

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_SystemUserIds",
                out string[] systerUsers
            );
            if (systerUsers == null || systerUsers.Length == 0)
            {
                errorMessage = "No users to manage roles for";
                tracer.Trace(errorMessage);
                outputParameters.ErrorMessage = errorMessage;
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }
            tracer.Trace($"Assigning roles to {systerUsers.Length} users");

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_WillAssign",
                out bool willAssign
            );
            tracer.Trace($"WillAssign is {willAssign}");

            var rolesCollection = RetrieveRoles(sysService, roles, tracer, out errorMessage);
            if (rolesCollection == null)
            {
                outputParameters.ErrorMessage = errorMessage;
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }

            EntityCollection systemUsersCollection;
            try
            {
                systemUsersCollection = RetrieveEntities(
                    sysService,
                    systerUsers,
                    _systemUserService.RetrieveSystemUsersWithRoles,
                    tracer
                );
            }
            catch (Exception ex)
            {
                outputParameters.ErrorMessage = $"Error: {ex.Message}";
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }

            var distinctSystemUserIds = GetDistinctEntityIds(systemUsersCollection, "systemuserid");
            if (distinctSystemUserIds.Count != systerUsers.Length)
            {
                foreach (var user in systerUsers)
                {
                    if (!distinctSystemUserIds.Contains(Guid.Parse(user)))
                    {
                        tracer.Trace($"User {user} was not found");
                    }
                }
                outputParameters.ErrorMessage =
                    $"Not all users were found. ({distinctSystemUserIds.Count}/{systerUsers.Length})";
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }
            tracer.Trace($"Retrieved {systemUsersCollection.Entities.Count} users");

            foreach (Guid systemUserId in distinctSystemUserIds)
            {
                var systemUserEntity = systemUsersCollection.Entities.First(t =>
                    t.GetAttributeValue<Guid>("systemuserid") == systemUserId
                );
                var rolesToAssign = RoleAssignmentService.ParseRolesToAssignOrRemove(
                    roles,
                    rolesCollection,
                    systemUsersCollection,
                    systemUserEntity,
                    willAssign ? AssignmnetType.Assign : AssignmnetType.Remove,
                    tracer,
                    out errorMessage
                );
                if (rolesToAssign == null || errorMessage != string.Empty)
                {
                    outputParameters.ErrorMessage = errorMessage;
                    SetOutputParameters(
                        outputParameters,
                        context,
                        tracer,
                        wasSuccessfulKey,
                        errorMessageKey
                    );
                    return;
                }
                tracer.Trace(
                    $"Assigning {rolesToAssign.Count} roles to {systemUserEntity.GetAttributeValue<string>("internalemailaddress")}"
                );

                try
                {
                    AssignOrRemoveRoles(
                        sysService,
                        systemUserId,
                        rolesToAssign,
                        tracer,
                        willAssign
                    );
                }
                catch (Exception ex)
                {
                    outputParameters.ErrorMessage = $"Error: {ex.Message}";
                    SetOutputParameters(
                        outputParameters,
                        context,
                        tracer,
                        wasSuccessfulKey,
                        errorMessageKey
                    );
                    return;
                }
            }
            outputParameters.WasSuccessful = true;
            outputParameters.ErrorMessage = "None";
            SetOutputParameters(
                outputParameters,
                context,
                tracer,
                wasSuccessfulKey,
                errorMessageKey
            );
        }

        protected override void AssignOrRemoveRoles(
            IOrganizationService service,
            Guid entityId,
            List<Entity> rolesToAssign,
            ITracingService tracer,
            bool willAssign
        )
        {
            if (willAssign)
            {
                _roleAssignmentService.AssignRolesToUser(service, entityId, rolesToAssign, tracer);
            }
            else
            {
                _roleAssignmentService.RemoveRolesFromUser(
                    service,
                    entityId,
                    rolesToAssign,
                    tracer
                );
            }
        }
    }
}
