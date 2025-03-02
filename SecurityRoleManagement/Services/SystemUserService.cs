using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CustomAPIs.Services
{
    public interface ISystemUserService
    {
        EntityCollection RetrieveSystemUsersWithRoles(
            IOrganizationService service,
            string[] userIds,
            ITracingService tracer
        );
        EntityCollection RetrieveSystemUsersWithTeams(
            IOrganizationService service,
            string[] userIds,
            ITracingService tracer
        );
    }

    public class SystemUserService : ISystemUserService
    {
        public EntityCollection RetrieveSystemUsersWithRoles(
            IOrganizationService service,
            string[] userIds,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression usersQuery = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet(
                        "systemuserid",
                        "fullname",
                        "internalemailaddress",
                        "businessunitid"
                    ),
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            LinkFromEntityName = "systemuser",
                            LinkFromAttributeName = "systemuserid",
                            LinkToEntityName = "systemuserroles",
                            LinkToAttributeName = "systemuserid",
                            JoinOperator = JoinOperator.LeftOuter,
                            LinkEntities =
                            {
                                new LinkEntity()
                                {
                                    LinkFromEntityName = "systemuserroles",
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
                FilterExpression userFilter = new FilterExpression(LogicalOperator.Or);
                foreach (var user in userIds)
                {
                    userFilter.AddCondition(
                        new ConditionExpression("systemuserid", ConditionOperator.Equal, user)
                    );
                }
                usersQuery.Criteria = userFilter;
                return service.RetrieveMultiple(usersQuery);
            }
            catch (Exception ex)
            {
                tracer.Trace($"An error occurred while retrieving users: {ex.Message}");
                throw;
            }
        }

        public EntityCollection RetrieveSystemUsersWithTeams(
            IOrganizationService service,
            string[] userIds,
            ITracingService tracer
        )
        {
            try
            {
                QueryExpression usersQuery = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet(
                        "systemuserid",
                        "fullname",
                        "internalemailaddress",
                        "businessunitid"
                    ),
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            LinkFromEntityName = "systemuser",
                            LinkFromAttributeName = "systemuserid",
                            LinkToEntityName = "teammembership",
                            LinkToAttributeName = "systemuserid",
                            JoinOperator = JoinOperator.LeftOuter,
                            LinkEntities =
                            {
                                new LinkEntity()
                                {
                                    LinkFromEntityName = "teammembership",
                                    LinkFromAttributeName = "teamid",
                                    LinkToEntityName = "team",
                                    LinkToAttributeName = "teamid",
                                    Columns = new ColumnSet("teamid", "name", "businessunitid"),
                                    EntityAlias = "team",
                                    JoinOperator = JoinOperator.LeftOuter,
                                },
                            },
                        },
                    },
                };
                FilterExpression userFilter = new FilterExpression(LogicalOperator.Or);
                foreach (var user in userIds)
                {
                    userFilter.AddCondition(
                        new ConditionExpression("systemuserid", ConditionOperator.Equal, user)
                    );
                }
                usersQuery.Criteria = userFilter;
                return service.RetrieveMultiple(usersQuery);
            }
            catch (Exception ex)
            {
                tracer.Trace($"An error occurred while retrieving users: {ex.Message}");
                throw;
            }
        }
    }
}
