using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace SecurityRoleManagement.Services
{
    public interface ITeamService
    {
        EntityCollection RetrieveTeams(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        );
    }

    public class TeamService : ITeamService
    {
        public EntityCollection RetrieveTeams(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression teamsQuery = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("teamid", "name", "businessunitid"),
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            LinkFromEntityName = "team",
                            LinkFromAttributeName = "teamid",
                            LinkToEntityName = "teamroles",
                            LinkToAttributeName = "teamid",
                            JoinOperator = JoinOperator.LeftOuter,
                            LinkEntities =
                            {
                                new LinkEntity()
                                {
                                    LinkFromEntityName = "teamroles",
                                    LinkFromAttributeName = "roleid",
                                    LinkToEntityName = "role",
                                    LinkToAttributeName = "roleid",
                                    Columns = new ColumnSet("roleid", "name", "businessunitid"),
                                    EntityAlias = "role",
                                    JoinOperator = JoinOperator.LeftOuter,
                                },
                            },
                        },
                    },
                };
                FilterExpression teamFilter = new FilterExpression(LogicalOperator.Or);
                foreach (var team in teamIds)
                {
                    teamFilter.AddCondition(
                        new ConditionExpression("teamid", ConditionOperator.Equal, team)
                    );
                }
                teamsQuery.Criteria.AddFilter(teamFilter);

                return service.RetrieveMultiple(teamsQuery);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error retrieving teams: {ex.Message}");
                throw;
            }
        }
    }
}
