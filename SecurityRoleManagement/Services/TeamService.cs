using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CustomAPIs.Services
{
    public interface ITeamService
    {
        EntityCollection RetrieveTeamsWithRoles(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        );

        EntityCollection RetrieveTeamsWithBusinessUnit(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        );
    }

    public class TeamService : ITeamService
    {
        public EntityCollection RetrieveTeamsWithBusinessUnit(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression teamsQuery = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("teamid", "name", "businessunitid", "isdefault"),
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            LinkFromEntityName = "team",
                            LinkFromAttributeName = "teamid",
                            LinkToEntityName = "businessunit",
                            LinkToAttributeName = "businessunitid",
                            Columns = new ColumnSet("businessunitid", "name"),
                            EntityAlias = "businessunit",
                            JoinOperator = JoinOperator.LeftOuter,
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

        public EntityCollection RetrieveTeamsWithRoles(
            IOrganizationService service,
            string[] teamIds,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression teamsQuery = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("teamid", "name", "businessunitid", "isdefault"),
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
