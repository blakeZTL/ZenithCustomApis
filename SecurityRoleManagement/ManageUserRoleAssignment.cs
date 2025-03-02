using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SecurityRoleManagement.Services;
using static SecurityRoleManagement.Utilities.OutputUtilities;

namespace SecurityRoleManagement
{
    public class ManageUserRoleAssignment : PluginBase
    {
        internal class OutputParameters
        {
            internal bool ManageUserRoleAssignments_WasSuccessful { get; set; }
            internal string ManageUserRoleAssignments_ErrorMessage { get; set; }

            internal OutputParameters(bool wasSuccesful, string errorMessage)
            {
                ManageUserRoleAssignments_WasSuccessful = wasSuccesful;
                ManageUserRoleAssignments_ErrorMessage = errorMessage;
            }

            internal Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    {
                        "zen_ManageUserRoleAssignments_WasSuccessful",
                        ManageUserRoleAssignments_WasSuccessful
                    },
                    {
                        "zen_ManageUserRoleAssignments_ErrorMessage",
                        ManageUserRoleAssignments_ErrorMessage
                    },
                };
            }
        }

        private readonly IRoleService _roleService;
        private readonly IRoleAssignmentService _roleAssignmentService;
        private readonly ISystemUserService _systemUserService;

        public ManageUserRoleAssignment()
            : base(typeof(ManageUserRoleAssignment))
        {
            _roleService = new RoleService();
            _roleAssignmentService = new RoleAssignmentService();
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

            SetOutputParameters(outputParameters.ToDictionary(), context, tracer);

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_RoleNames",
                out string[] roles
            );
            if (roles == null || roles.Length == 0)
            {
                var errorMessage = "No roles to manage for users";
                tracer.Trace(errorMessage);
                outputParameters.ManageUserRoleAssignments_ErrorMessage = errorMessage;
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Assigning {roles.Length} roles to the team");

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_SystemUserIds",
                out string[] systerUsers
            );
            if (systerUsers == null || systerUsers.Length == 0)
            {
                var errorMessage = "No users to manage roles for";
                tracer.Trace(errorMessage);
                outputParameters.ManageUserRoleAssignments_ErrorMessage = errorMessage;
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Assigning roles to {systerUsers.Length} users");

            context.InputParameters.TryGetValue(
                "zen_ManageUserRoleAssignments_WillAssign",
                out bool willAssign
            );
            tracer.Trace($"WillAssign is {willAssign}");

            EntityCollection rolesCollection;
            try
            {
                rolesCollection = _roleService.RetrieveRoles(sysService, roles, tracer);
            }
            catch (Exception ex)
            {
                outputParameters.ManageUserRoleAssignments_ErrorMessage = $"Error: {ex.Message}";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            if (rolesCollection.Entities.Count < roles.Length)
            {
                outputParameters.ManageUserRoleAssignments_ErrorMessage =
                    $"Not all roles were found. ({rolesCollection.Entities.Count}/{roles.Length})";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Retrieved {rolesCollection.Entities.Count} roles");
            EntityCollection systemUsersCollection;
            try
            {
                systemUsersCollection = _systemUserService.RetrieveSystemUsers(
                    sysService,
                    systerUsers,
                    tracer
                );
            }
            catch (Exception ex)
            {
                outputParameters.ManageUserRoleAssignments_ErrorMessage = $"Error: {ex.Message}";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            var distinctSystemUserIds = systemUsersCollection
                .Entities.Select(t => t.GetAttributeValue<Guid>("systemuserid"))
                .Distinct()
                .ToList();
            if (distinctSystemUserIds.Count != systerUsers.Length)
            {
                foreach (var user in systerUsers)
                {
                    if (!distinctSystemUserIds.Contains(Guid.Parse(user)))
                    {
                        tracer.Trace($"User {user} was not found");
                    }
                }
                outputParameters.ManageUserRoleAssignments_ErrorMessage =
                    $"Not all users were found. ({distinctSystemUserIds.Count}/{systerUsers.Length})";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
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
                    out string errorMessage
                );
                if (rolesToAssign == null || errorMessage != string.Empty)
                {
                    outputParameters.ManageUserRoleAssignments_ErrorMessage = errorMessage;
                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                    return;
                }
                tracer.Trace(
                    $"Assigning {rolesToAssign.Count} roles to {systemUserEntity.GetAttributeValue<string>("internalemailaddress")}"
                );

                try
                {
                    if (willAssign)
                    {
                        _roleAssignmentService.AssignRolesToUser(
                            sysService,
                            systemUserId,
                            rolesToAssign,
                            tracer
                        );
                    }
                    else
                    {
                        _roleAssignmentService.RemoveRolesFromUser(
                            sysService,
                            systemUserId,
                            rolesToAssign,
                            tracer
                        );
                    }
                }
                catch (Exception ex)
                {
                    outputParameters.ManageUserRoleAssignments_ErrorMessage =
                        $"Error: {ex.Message}";
                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                    return;
                }
            }
            outputParameters.ManageUserRoleAssignments_WasSuccessful = true;
            outputParameters.ManageUserRoleAssignments_ErrorMessage = "None";
            SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
        }
    }
}
