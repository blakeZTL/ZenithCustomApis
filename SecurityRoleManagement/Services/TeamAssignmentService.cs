using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace CustomAPI.Services
{
    public interface ITeamAssignmentService
    {
        void AssignUserToTeams(
            IOrganizationService service,
            Guid userId,
            List<Guid> teamIds,
            ITracingService tracer
        );
        void RemoveUserFromTeams(
            IOrganizationService service,
            Guid userId,
            List<Guid> teamIds,
            ITracingService tracer
        );
    }

    public class TeamAssignmentService : ITeamAssignmentService
    {
        public void AssignUserToTeams(
            IOrganizationService service,
            Guid userId,
            List<Guid> teamIds,
            ITracingService tracer
        )
        {
            AssociateRequest request = new AssociateRequest
            {
                Target = new EntityReference("systemuser", userId),
                RelatedEntities = new EntityReferenceCollection(
                    teamIds.Select(id => new EntityReference("team", id)).ToList()
                ),
                Relationship = new Relationship("teammembership"),
            };

            try
            {
                tracer.Trace($"Assigning user to {teamIds.Count} teams");
                service.Execute(request);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error assigning teams: {ex.Message}");
                throw;
            }
        }

        public void RemoveUserFromTeams(
            IOrganizationService service,
            Guid userId,
            List<Guid> teamIds,
            ITracingService tracer
        )
        {
            DisassociateRequest request = new DisassociateRequest
            {
                Target = new EntityReference("systemuser", userId),
                RelatedEntities = new EntityReferenceCollection(
                    teamIds.Select(id => new EntityReference("team", id)).ToList()
                ),
                Relationship = new Relationship("teammembership"),
            };

            try
            {
                tracer.Trace($"Removing user from {teamIds.Count} teams");
                service.Execute(request);
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error removing teams: {ex.Message}");
                throw;
            }
        }
    }
}
