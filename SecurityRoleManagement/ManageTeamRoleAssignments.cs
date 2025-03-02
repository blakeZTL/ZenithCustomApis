using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SecurityRoleManagement.Services;
using static SecurityRoleManagement.Utilities.OutputUtilities;

namespace SecurityRoleManagement
{
    public class ManageTeamRoleAssignments : PluginBase
    {
        internal class OutputParameters
        {
            internal bool ManageTeamRoleAssignments_WasSuccessful { get; set; }
            internal string ManageTeamRoleAssignments_ErrorMessage { get; set; }

            internal OutputParameters(bool wasSuccesful, string errorMessage)
            {
                ManageTeamRoleAssignments_ErrorMessage = errorMessage;
                ManageTeamRoleAssignments_WasSuccessful = wasSuccesful;
            }

            internal Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    {
                        "zen_ManageTeamRoleAssignments_WasSuccessful",
                        ManageTeamRoleAssignments_WasSuccessful
                    },
                    {
                        "zen_ManageTeamRoleAssignments_ErrorMessage",
                        ManageTeamRoleAssignments_ErrorMessage
                    },
                };
            }
        }

        private readonly IRoleService _roleService;
        private readonly ITeamService _teamService;
        private readonly IRoleAssignmentService _roleAssignmentService;

        public ManageTeamRoleAssignments()
            : base(typeof(ManageTeamRoleAssignments))
        {
            _roleService = new RoleService();
            _teamService = new TeamService();
            _roleAssignmentService = new RoleAssignmentService();
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
                "zen_ManageTeamRoleAssignments_RoleNames",
                out string[] roles
            );
            if (roles == null || roles.Length == 0)
            {
                var errorMessage = "No roles to manage for teams";
                tracer.Trace(errorMessage);
                outputParameters.ManageTeamRoleAssignments_ErrorMessage = errorMessage;
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Assigning {roles.Length} roles to the team");

            context.InputParameters.TryGetValue(
                "zen_ManageTeamRoleAssignments_TeamIds",
                out string[] teams
            );
            if (teams == null || teams.Length == 0)
            {
                var errorMessage = "No teams to manage roles for";
                tracer.Trace(errorMessage);
                outputParameters.ManageTeamRoleAssignments_ErrorMessage = errorMessage;
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Assigning roles to {teams.Length} teams");

            context.InputParameters.TryGetValue(
                "zen_ManageTeamRoleAssignments_WillAssign",
                out bool willAssign
            );
            tracer.Trace($"WillAssign is {willAssign}");

            //context.InputParameters.TryGetValue(
            //    "zen_ManageTeamRoleAssignments_RequireSameBusinessUnit",
            //    out bool requireSameBusinessUnit
            //);
            //if (requireSameBusinessUnit)
            //    tracer.Trace("RequireSameBusinessUnit is true");

            EntityCollection rolesCollection;
            try
            {
                rolesCollection = _roleService.RetrieveRoles(sysService, roles, tracer);
            }
            catch (Exception ex)
            {
                outputParameters.ManageTeamRoleAssignments_ErrorMessage = $"Error: {ex.Message}";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            if (rolesCollection.Entities.Count < roles.Length)
            {
                outputParameters.ManageTeamRoleAssignments_ErrorMessage =
                    $"Not all roles were found. ({rolesCollection.Entities.Count}/{roles.Length})";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            tracer.Trace($"Retrieved {rolesCollection.Entities.Count} roles");

            EntityCollection teamsCollection;
            try
            {
                teamsCollection = _teamService.RetrieveTeams(sysService, teams, tracer);
            }
            catch (Exception ex)
            {
                outputParameters.ManageTeamRoleAssignments_ErrorMessage = $"Error: {ex.Message}";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                return;
            }
            var distinctTeamIds = teamsCollection
                .Entities.Select(t => t.GetAttributeValue<Guid>("teamid"))
                .Distinct()
                .ToList();
            if (distinctTeamIds.Count != teams.Length)
            {
                foreach (var team in teams)
                {
                    if (!distinctTeamIds.Contains(Guid.Parse(team)))
                    {
                        tracer.Trace($"Team {team} was not found");
                    }
                }
                outputParameters.ManageTeamRoleAssignments_ErrorMessage =
                    $"Not all teams were found. ({distinctTeamIds.Count}/{teams.Length})";
                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
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
                    out string errorMessage
                );
                if (rolesToAssign == null || errorMessage != string.Empty)
                {
                    outputParameters.ManageTeamRoleAssignments_ErrorMessage = errorMessage;
                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                    return;
                }
                tracer.Trace(
                    $"Assigning {rolesToAssign.Count} roles to {teamEntity.GetAttributeValue<string>("name")}"
                );

                try
                {
                    if (willAssign)
                    {
                        _roleAssignmentService.AssignRolesToTeam(
                            sysService,
                            teamId,
                            rolesToAssign,
                            tracer
                        );
                    }
                    else
                    {
                        _roleAssignmentService.RemoveRolesFromTeam(
                            sysService,
                            teamId,
                            rolesToAssign,
                            tracer
                        );
                    }
                }
                catch (Exception ex)
                {
                    outputParameters.ManageTeamRoleAssignments_ErrorMessage =
                        $"Error: {ex.Message}";
                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
                    return;
                }
            }
            outputParameters.ManageTeamRoleAssignments_WasSuccessful = true;
            outputParameters.ManageTeamRoleAssignments_ErrorMessage = "None";
            SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
        }
    }
}
