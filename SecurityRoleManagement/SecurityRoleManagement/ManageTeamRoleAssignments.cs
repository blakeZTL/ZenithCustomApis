using System;
using System.Collections.Generic;
using System.Linq;
using CustomAPIs.Services;
using CustomAPIs.Utilities;
using Microsoft.Xrm.Sdk;

namespace CustomAPIs.SecurityRoleManagement
{
    public class ManageTeamRoleAssignments : BaseRoleAssignmentPlugin
    {
        private readonly ITeamService _teamService;

        public ManageTeamRoleAssignments()
            : base(
                typeof(ManageTeamRoleAssignments),
                new RoleService(),
                new RoleAssignmentService()
            )
        {
            _teamService = new TeamService();
        }

        protected override void ExecuteCdsPlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
                throw new InvalidPluginExecutionException("Plugin Context was null");

            var context = localPluginContext.PluginExecutionContext;
            var sysService = localPluginContext.SystemUserService;
            var tracer = localPluginContext.TracingService;

            OutputParameters outputParameters = new OutputParameters(false, "None");
            string wasSuccessfulKey = "zen_ManageTeamRoleAssignments_WasSuccessful";
            string errorMessageKey = "zen_ManageTeamRoleAssignments_ErrorMessage";
            SetOutputParameters(
                outputParameters,
                context,
                tracer,
                wasSuccessfulKey,
                errorMessageKey
            );

            string errorMessage;

            context.InputParameters.TryGetValue(
                "zen_ManageTeamRoleAssignments_RoleNames",
                out string[] roles
            );
            if (roles == null || roles.Length == 0)
            {
                errorMessage = "No roles to manage for teams";
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
                "zen_ManageTeamRoleAssignments_TeamIds",
                out string[] teams
            );
            if (teams == null || teams.Length == 0)
            {
                errorMessage = "No teams to manage roles for";
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
            tracer.Trace($"Assigning roles to {teams.Length} teams");

            context.InputParameters.TryGetValue(
                "zen_ManageTeamRoleAssignments_WillAssign",
                out bool willAssign
            );
            tracer.Trace($"WillAssign is {willAssign}");

            var rolesCollection = PluginUtilities.RetrieveRoles(
                _roleService,
                sysService,
                roles,
                tracer,
                out errorMessage
            );
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

            EntityCollection teamsCollection;
            try
            {
                teamsCollection = _teamService.RetrieveTeamsWithRoles(sysService, teams, tracer);
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
            var distinctTeamIds = PluginUtilities.GetDistinctEntityIds(teamsCollection, "teamid");
            if (distinctTeamIds.Count != teams.Length)
            {
                foreach (var team in teams)
                {
                    if (!distinctTeamIds.Contains(Guid.Parse(team)))
                    {
                        tracer.Trace($"Team {team} was not found");
                    }
                }
                outputParameters.ErrorMessage =
                    $"Not all teams were found. ({distinctTeamIds.Count}/{teams.Length})";
                SetOutputParameters(
                    outputParameters,
                    context,
                    tracer,
                    wasSuccessfulKey,
                    errorMessageKey
                );
                return;
            }
            tracer.Trace($"Retrieved {teamsCollection.Entities.Count} teams");

            foreach (Guid teamId in distinctTeamIds)
            {
                var teamEntity = teamsCollection.Entities.First(t =>
                    t.GetAttributeValue<Guid>("teamid") == teamId
                );
                var rolesToAssign = RoleAssignmentService.ParseRolesToAssignOrRemove(
                    roles,
                    rolesCollection,
                    teamsCollection,
                    teamEntity,
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
                    $"Assigning {rolesToAssign.Count} roles to {teamEntity.GetAttributeValue<string>("name")}"
                );

                try
                {
                    AssignOrRemoveRoles(sysService, teamId, rolesToAssign, tracer, willAssign);
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
                _roleAssignmentService.AssignRolesToTeam(service, entityId, rolesToAssign, tracer);
            }
            else
            {
                _roleAssignmentService.RemoveRolesFromTeam(
                    service,
                    entityId,
                    rolesToAssign,
                    tracer
                );
            }
        }
    }
}
