using System;
using System.Collections.Generic;
using System.Linq;
using CustomAPI.Services;
using CustomAPIs;
using CustomAPIs.Services;
using CustomAPIs.Utilities;
using Microsoft.Xrm.Sdk;

namespace CustomAPI.UserTeamManagement
{
    public class ManageUserTeamAssignment : PluginBase
    {
        internal class InputParameters
        {
            public const string TeamIds = "zen_ManageUserTeamAssignments_TeamIds";
            public const string UserIds = "zen_ManageUserTeamAssignments_SystemUserIds";
            public const string WillAssign = "zen_ManageUserTeamAssignments_WillAssign";
        }

        private readonly ITeamService _teamService;
        private readonly ISystemUserService _systemUserService;
        private readonly ITeamAssignmentService _teamAssignmentService;

        public ManageUserTeamAssignment()
            : base(typeof(ManageUserTeamAssignment))
        {
            _teamService = new TeamService();
            _systemUserService = new SystemUserService();
            _teamAssignmentService = new TeamAssignmentService();
        }

        protected override void ExecuteCdsPlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
                throw new InvalidPluginExecutionException("Plugin Context was null");

            var context = localPluginContext.PluginExecutionContext;
            var sysService = localPluginContext.SystemUserService;
            var tracer = localPluginContext.TracingService;

            OutputParameters outputParameters = new OutputParameters(false, "None");
            string wasSuccessfulKey = "zen_ManageUserTeamAssignments_WasSuccessful";
            string errorMessageKey = "zen_ManageUserTeamAssignments_ErrorMessage";
            PluginUtilities.SetOutputParameters(
                outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                context,
                tracer
            );

            string errorMessage;

            context.InputParameters.TryGetValue(InputParameters.TeamIds, out string[] teamIds);
            context.InputParameters.TryGetValue(InputParameters.UserIds, out string[] userIds);
            context.InputParameters.TryGetValue(InputParameters.WillAssign, out bool willAssign);
            if (teamIds == null || teamIds.Length == 0 || userIds == null || userIds.Length == 0)
            {
                errorMessage = "Users and teams required";
                tracer.Trace(errorMessage);
                tracer.Trace(
                    teamIds == null ? "TeamIds is null" : $"TeamIds length: {teamIds.Length}"
                );
                tracer.Trace(
                    userIds == null ? "UserIds is null" : $"UserIds length: {userIds.Length}"
                );
                outputParameters.ErrorMessage = errorMessage;
                PluginUtilities.SetOutputParameters(
                    outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                    context,
                    tracer
                );
                return;
            }

            tracer.Trace($"Assigning {userIds.Length} users to {teamIds.Length} teams");

            EntityCollection teams = PluginUtilities.RetrieveEntities(
                sysService,
                teamIds,
                _teamService.RetrieveTeamsWithBusinessUnit,
                tracer
            );
            var containsDefaultTeam = teams.Entities.Any(e =>
                e.GetAttributeValue<bool>("isdefault")
            );
            if (containsDefaultTeam)
            {
                errorMessage = "Default team cannot be assigned users";
                tracer.Trace(errorMessage);
                outputParameters.ErrorMessage = errorMessage;
                PluginUtilities.SetOutputParameters(
                    outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                    context,
                    tracer
                );
                return;
            }

            EntityCollection users = PluginUtilities.RetrieveEntities(
                sysService,
                userIds,
                _systemUserService.RetrieveSystemUsersWithTeams,
                tracer
            );

            List<Guid> teamIdsList = PluginUtilities.GetDistinctEntityIds(teams, "teamid");
            List<Guid> userIdsList = PluginUtilities.GetDistinctEntityIds(users, "systemuserid");

            var teamUserAssignments = new List<Entity>();
            foreach (var userId in userIdsList)
            {
                try
                {
                    AssignOrRemoveTeam(sysService, userId, teamIdsList, willAssign, tracer);
                }
                catch (Exception ex)
                {
                    tracer.Trace($"Error: {ex.Message}");
                    errorMessage = ex.Message;
                    outputParameters.ErrorMessage = errorMessage;
                    PluginUtilities.SetOutputParameters(
                        outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                        context,
                        tracer
                    );
                    return;
                }
            }

            outputParameters.WasSuccessful = true;
            PluginUtilities.SetOutputParameters(
                outputParameters.ToDictionary(wasSuccessfulKey, errorMessageKey),
                context,
                tracer
            );
        }

        internal void AssignOrRemoveTeam(
            IOrganizationService service,
            Guid userId,
            List<Guid> teamIds,
            bool willAssign,
            ITracingService tracer
        )
        {
            if (willAssign)
            {
                _teamAssignmentService.AssignUserToTeams(service, userId, teamIds, tracer);
            }
            else
            {
                _teamAssignmentService.RemoveUserFromTeams(service, userId, teamIds, tracer);
            }
        }
    }
}
