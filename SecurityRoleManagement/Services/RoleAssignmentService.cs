using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace SecurityRoleManagement.Services
{
    public interface IRoleAssignmentService
    {
        void AssignRolesToTeam(
            IOrganizationService service,
            Guid teamId,
            List<Entity> rolesToAssign,
            ITracingService tracer
        );
        void RemoveRolesFromTeam(
            IOrganizationService service,
            Guid teamId,
            List<Entity> rolesToRemove,
            ITracingService tracer
        );
        void AssignRolesToUser(
            IOrganizationService service,
            Guid userId,
            List<Entity> rolesToAssign,
            ITracingService tracer
        );
        void RemoveRolesFromUser(
            IOrganizationService service,
            Guid userId,
            List<Entity> rolesToRemove,
            ITracingService tracer
        );
    }

    public enum AssignmnetType
    {
        Assign,
        Remove,
    }

    public class RoleAssignmentService : IRoleAssignmentService
    {
        public void AssignRolesToTeam(
            IOrganizationService service,
            Guid teamId,
            List<Entity> rolesToAssign,
            ITracingService tracer
        )
        {
            AssociateRequest associateRequest = new AssociateRequest
            {
                Target = new EntityReference("team", teamId),
                RelatedEntities = new EntityReferenceCollection(
                    rolesToAssign.Select(r => new EntityReference(r.LogicalName, r.Id)).ToList()
                ),
                Relationship = new Relationship("teamroles_association"),
            };

            try
            {
                tracer.Trace($"Assigning {rolesToAssign.Count} roles to team {teamId}");
                service.Execute(associateRequest);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot insert duplicate key"))
                {
                    tracer.Trace("Role is already assigned to the team");
                    return;
                }
                tracer.Trace($"Error assigning roles: {ex.Message}");
                throw;
            }
        }

        public void RemoveRolesFromTeam(
            IOrganizationService service,
            Guid teamId,
            List<Entity> rolesToRemove,
            ITracingService tracer
        )
        {
            DisassociateRequest disassociateRequest = new DisassociateRequest
            {
                Target = new EntityReference("team", teamId),
                RelatedEntities = new EntityReferenceCollection(
                    rolesToRemove.Select(r => new EntityReference(r.LogicalName, r.Id)).ToList()
                ),
                Relationship = new Relationship("teamroles_association"),
            };
            try
            {
                tracer.Trace($"Removing {rolesToRemove.Count} roles from team {teamId}");
                service.Execute(disassociateRequest);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error removing roles: {ex.Message}");
                throw;
            }
        }

        public void AssignRolesToUser(
            IOrganizationService service,
            Guid userId,
            List<Entity> rolesToAssign,
            ITracingService tracer
        )
        {
            AssociateRequest associateRequest = new AssociateRequest
            {
                Target = new EntityReference("systemuser", userId),
                RelatedEntities = new EntityReferenceCollection(
                    rolesToAssign.Select(r => new EntityReference(r.LogicalName, r.Id)).ToList()
                ),
                Relationship = new Relationship("systemuserroles_association"),
            };
            try
            {
                tracer.Trace($"Assigning {rolesToAssign.Count} roles to user {userId}");
                service.Execute(associateRequest);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot insert duplicate key"))
                {
                    tracer.Trace("Role is already assigned to the user");
                    return;
                }
                tracer.Trace($"Error assigning roles: {ex.Message}");
                throw;
            }
        }

        public void RemoveRolesFromUser(
            IOrganizationService service,
            Guid userId,
            List<Entity> rolesToRemove,
            ITracingService tracer
        )
        {
            DisassociateRequest disassociateRequest = new DisassociateRequest
            {
                Target = new EntityReference("systemuser", userId),
                RelatedEntities = new EntityReferenceCollection(
                    rolesToRemove.Select(r => new EntityReference(r.LogicalName, r.Id)).ToList()
                ),
                Relationship = new Relationship("systemuserroles_association"),
            };
            try
            {
                tracer.Trace($"Removing {rolesToRemove.Count} roles from user {userId}");
                service.Execute(disassociateRequest);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error removing roles: {ex.Message}");
                throw;
            }
        }

        public static List<Entity> ParseRolesToAssignOrRemove(
            string[] roleNames,
            EntityCollection roles,
            EntityCollection entities,
            Entity entity,
            AssignmnetType assignmentType,
            ITracingService tracer,
            out string errorMessage,
            bool requireSameBusinessUnit = true
        )
        {
            tracer.Trace("Start: " + nameof(ParseRolesToAssignOrRemove) + DateTime.Now.ToString());
            errorMessage = string.Empty;
            var rolesToAssignOrRemove = new List<Entity>();

            string entityLogicalName = entity.LogicalName;
            if (entityLogicalName != "team" && entityLogicalName != "systemuser")
            {
                tracer.Trace("Invalid entity type");
                errorMessage = "Invalid entity type";
                return null;
            }
            string entityIdName = entityLogicalName + "id";
            string nameAttribute = entityLogicalName == "team" ? "name" : "internalemailaddress";

            try
            {
                var entityName = entity.GetAttributeValue<string>(nameAttribute);
                var entityBusinessUnit = entity.GetAttributeValue<EntityReference>(
                    "businessunitid"
                );

                // Filter roles based on business unit if required
                if (requireSameBusinessUnit)
                {
                    rolesToAssignOrRemove = roles
                        .Entities.Where(r =>
                            r.GetAttributeValue<EntityReference>("businessunitid").Id
                            == entityBusinessUnit.Id
                        )
                        .ToList();
                }
                else
                {
                    rolesToAssignOrRemove = roleNames
                        .Select(r =>
                            roles.Entities.First(role =>
                                role.GetAttributeValue<string>("name") == r
                            )
                        )
                        .ToList();
                }

                // Ensure all roles are found
                if (rolesToAssignOrRemove.Count != roleNames.Length)
                {
                    var message =
                        $"Not all roles were found. ({rolesToAssignOrRemove.Count}/{roleNames.Length})";
                    tracer.Trace(message);
                    errorMessage = message;
                    return null;
                }

                tracer.Trace(
                    $"{rolesToAssignOrRemove.Count} roles found to {(assignmentType == AssignmnetType.Assign ? "assign to" : "remove from")} {entityName}"
                );

                // Filter out roles that are already assigned to the team
                var relatedRecords = entities
                    .Entities.Where(t => t.GetAttributeValue<Guid>(entityIdName) == entity.Id)
                    .ToList();

                tracer.Trace($"Related {entityLogicalName} records count: {relatedRecords.Count}");

                rolesToAssignOrRemove = rolesToAssignOrRemove
                    .Where(r =>
                    {
                        if (relatedRecords.Count == 0)
                            if (assignmentType == AssignmnetType.Assign)
                                return true;
                            else
                                return false;

                        var roleId = r.Id; // Ensure the role ID is correctly retrieved
                        var isAssigned = relatedRecords.Any(t =>
                        {
                            var roleAlias = t.GetAttributeValue<AliasedValue>("role.roleid");
                            if (roleAlias == null || roleAlias.Value == null)
                                return false;

                            var assigned = (Guid)roleAlias.Value == roleId;
                            tracer.Trace($"Checking if role {roleId} is assigned: {assigned}");
                            return assigned;
                        });

                        tracer.Trace(
                            $"Role {r.GetAttributeValue<string>("name")} is assigned: {isAssigned}"
                        );
                        if (assignmentType == AssignmnetType.Assign)
                            return !isAssigned;
                        return isAssigned;
                    })
                    .ToList();
                tracer.Trace(
                    $"{rolesToAssignOrRemove.Count} roles to {(assignmentType == AssignmnetType.Assign ? "assign" : "remove")} after filtering"
                );
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error parsing roles: {ex.Message}");
                errorMessage = ex.Message;
                return null;
            }
            finally
            {
                tracer.Trace(
                    "End: " + nameof(ParseRolesToAssignOrRemove) + DateTime.Now.ToString()
                );
            }

            return rolesToAssignOrRemove;
        }
    }
}
