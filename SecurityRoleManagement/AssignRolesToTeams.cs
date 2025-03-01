//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.Xrm.Sdk;
//using SecurityRoleManagement.Services;
//using static SecurityRoleManagement.Utilities.OutputUtilities;

//namespace SecurityRoleManagement
//{
//    class OutputParameters
//    {
//        public bool AssignRolesToTeams_WasSuccessfull { get; set; }
//        public string AssignRolesToTeams_ErrorMessage { get; set; }

//        public OutputParameters(bool wasSuccesful, string errorMessage)
//        {
//            AssignRolesToTeams_ErrorMessage = errorMessage;
//            AssignRolesToTeams_WasSuccessfull = wasSuccesful;
//        }

//        public Dictionary<string, object> ToDictionary()
//        {
//            return new Dictionary<string, object>
//            {
//                { "zen_AssignRolesToTeams_WasSuccessfull", AssignRolesToTeams_WasSuccessfull },
//                { "zen_AssignRolesToTeams_ErrorMessage", AssignRolesToTeams_ErrorMessage },
//            };
//        }
//    }

//    public class AssignRolesToTeams : PluginBase
//    {
//        private readonly IRoleService _roleService;
//        private readonly ITeamService _teamService;
//        private readonly IRoleAssignmentService _roleAssignmentService;

//        public AssignRolesToTeams()
//            : base(typeof(AssignRolesToTeams))
//        {
//            _roleService = new RoleService();
//            _teamService = new TeamService();
//            _roleAssignmentService = new RoleAssignmentService();
//        }

//        protected override void ExecuteCdsPlugin(ILocalPluginContext localPluginContext)
//        {
//            if (localPluginContext == null)
//                throw new InvalidPluginExecutionException("Plugin Context was null");

//            var context = localPluginContext.PluginExecutionContext;
//            var sysService = localPluginContext.SystemUserService;
//            var tracer = localPluginContext.TracingService;

//            OutputParameters outputParameters = new OutputParameters(false, "None");

//            SetOutputParameters(outputParameters.ToDictionary(), context, tracer);

//            context.InputParameters.TryGetValue(
//                "zen_AssignRolesToTeams_RoleNames",
//                out string[] roles
//            );
//            if (roles == null || roles.Length == 0)
//            {
//                var errorMessage = "No roles to assign to the team";
//                tracer.Trace(errorMessage);
//                outputParameters.AssignRolesToTeams_ErrorMessage = errorMessage;
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            tracer.Trace($"Assigning {roles.Length} roles to the team");

//            context.InputParameters.TryGetValue("zen_AssignRolesToTeams_Teams", out string[] teams);
//            if (teams == null || teams.Length == 0)
//            {
//                var errorMessage = "No teams to assign roles to";
//                tracer.Trace(errorMessage);
//                outputParameters.AssignRolesToTeams_ErrorMessage = errorMessage;
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            tracer.Trace($"Assigning roles to {teams.Length} teams");

//            context.InputParameters.TryGetValue(
//                "zen_AssignRolesToTeams_RequireSameBusinessUnit",
//                out bool requireSameBusinessUnit
//            );
//            if (requireSameBusinessUnit)
//                tracer.Trace("RequireSameBusinessUnit is true");

//            EntityCollection rolesCollection;
//            try
//            {
//                rolesCollection = _roleService.RetrieveRoles(sysService, roles, tracer);
//            }
//            catch (Exception ex)
//            {
//                outputParameters.AssignRolesToTeams_ErrorMessage = $"Error: {ex.Message}";
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            if (rolesCollection.Entities.Count < roles.Length)
//            {
//                outputParameters.AssignRolesToTeams_ErrorMessage =
//                    $"Not all roles were found. ({rolesCollection.Entities.Count}/{roles.Length})";
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            tracer.Trace($"Retrieved {rolesCollection.Entities.Count} roles");

//            EntityCollection teamsCollection;
//            try
//            {
//                teamsCollection = _teamService.RetrieveTeams(sysService, teams, tracer);
//            }
//            catch (Exception ex)
//            {
//                outputParameters.AssignRolesToTeams_ErrorMessage = $"Error: {ex.Message}";
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            var distinctTeamIds = teamsCollection
//                .Entities.Select(t => t.GetAttributeValue<Guid>("teamid"))
//                .Distinct()
//                .ToList();
//            if (distinctTeamIds.Count != teams.Length)
//            {
//                foreach (var team in teams)
//                {
//                    if (!distinctTeamIds.Contains(Guid.Parse(team)))
//                    {
//                        tracer.Trace($"Team {team} was not found");
//                    }
//                }
//                outputParameters.AssignRolesToTeams_ErrorMessage =
//                    $"Not all teams were found. ({distinctTeamIds.Count}/{teams.Length})";
//                SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                return;
//            }
//            tracer.Trace($"Retrieved {teamsCollection.Entities.Count} teams");

//            foreach (Guid teamId in distinctTeamIds)
//            {
//                var teamEntity = teamsCollection.Entities.First(t =>
//                    t.GetAttributeValue<Guid>("teamid") == teamId
//                );
//                var rolesToAssign = RoleAssignmentService.ParseRolesToAssignOrRemove(
//                    roles,
//                    rolesCollection,
//                    teamsCollection,
//                    teamEntity,
//                    AssignmnetType.Assign,
//                    tracer,
//                    out string errorMessage,
//                    requireSameBusinessUnit
//                );
//                if (rolesToAssign == null || errorMessage != string.Empty)
//                {
//                    outputParameters.AssignRolesToTeams_ErrorMessage = errorMessage;
//                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                    return;
//                }
//                tracer.Trace(
//                    $"Assigning {rolesToAssign.Count} roles to {teamEntity.GetAttributeValue<string>("name")}"
//                );

//                try
//                {
//                    _roleAssignmentService.AssignRolesToTeam(
//                        sysService,
//                        teamId,
//                        rolesToAssign,
//                        tracer
//                    );
//                }
//                catch (Exception ex)
//                {
//                    outputParameters.AssignRolesToTeams_ErrorMessage = $"Error: {ex.Message}";
//                    SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//                    return;
//                }
//            }
//            outputParameters.AssignRolesToTeams_WasSuccessfull = true;
//            outputParameters.AssignRolesToTeams_ErrorMessage = "None";
//            SetOutputParameters(outputParameters.ToDictionary(), context, tracer);
//        }
//    }
//}
