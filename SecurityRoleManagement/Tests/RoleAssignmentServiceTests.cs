using System;
using System.Collections.Generic;
using CustomAPIs.Services;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CustomAPIs.Tests
{
    public class RoleAssignmentServiceTests
    {
        private readonly Mock<ITracingService> _tracingServiceMock;
        private readonly ITestOutputHelper _output;

        public RoleAssignmentServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _tracingServiceMock = new Mock<ITracingService>();

            // Set up the mock to log messages to the test output
            _tracingServiceMock
                .Setup(t => t.Trace(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>(
                    (message, args) => _output.WriteLine($"TracingService: {message}")
                );
        }

        [Theory]
        [InlineData(AssignmnetType.Assign)]
        [InlineData(AssignmnetType.Remove)]
        public void ParseRolesToAssignOrRemove_ShouldReturnRoles_WhenRolesAreNotAssigned(
            AssignmnetType assignmentType
        )
        {
            // Arrange
            var roleNames = new[] { "Role1", "Role2" };
            var businessUnitRef = new EntityReference("team", Guid.NewGuid());

            var roles = new EntityCollection(
                new List<Entity>
                {
                    new Entity("role")
                    {
                        Id = Guid.NewGuid(),
                        ["name"] = "Role1",
                        ["businessunitid"] = businessUnitRef,
                    },
                    new Entity("role")
                    {
                        Id = Guid.NewGuid(),
                        ["name"] = "Role2",
                        ["businessunitid"] = businessUnitRef,
                    },
                }
            );
            var team = new Entity("team")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Team1",
                ["businessunitid"] = businessUnitRef,
            };
            var teams = new EntityCollection(new List<Entity>());

            // Act
            var result = RoleAssignmentService.ParseRolesToAssignOrRemove(
                roleNames,
                roles,
                teams,
                team,
                assignmentType,
                _tracingServiceMock.Object,
                out string errorMessage
            );

            // Assert
            if (assignmentType == AssignmnetType.Assign)
            {
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Empty(errorMessage);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Empty(result);
                Assert.Empty(errorMessage);
            }
        }

        [Theory]
        [InlineData(AssignmnetType.Assign)]
        [InlineData(AssignmnetType.Remove)]
        public void ParseRolesToAssignOrRemove_ShouldReturnEmpty_WhenAllRolesAreAssigned(
            AssignmnetType assignmentType
        )
        {
            // Arrange
            var roleNames = new[] { "Role1", "Role2" };
            var role1Id = Guid.NewGuid();
            var role2Id = Guid.NewGuid();
            var businessUnitId = Guid.NewGuid();
            var roles = new EntityCollection(
                new List<Entity>
                {
                    new Entity("role")
                    {
                        Id = role1Id,
                        ["name"] = "Role1",
                        ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                    },
                    new Entity("role")
                    {
                        Id = role2Id,
                        ["name"] = "Role2",
                        ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                    },
                }
            );
            var team = new Entity("team")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Team1",
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
            };
            var teams = new EntityCollection(
                new List<Entity>
                {
                    new Entity("team")
                    {
                        Id = team.Id,
                        ["teamid"] = team.Id,
                        ["role.roleid"] = new AliasedValue("role", "roleid", role1Id),
                        ["role.name"] = new AliasedValue("role", "name", "Role1"),
                        ["role.businessunitid"] = new AliasedValue(
                            "role",
                            "businessunitid",
                            businessUnitId
                        ),
                    },
                    new Entity("team")
                    {
                        Id = team.Id,
                        ["teamid"] = team.Id,
                        ["role.roleid"] = new AliasedValue("role", "roleid", role2Id),
                        ["role.name"] = new AliasedValue("role", "name", "Role2"),
                        ["role.businessunitid"] = new AliasedValue(
                            "role",
                            "businessunitid",
                            businessUnitId
                        ),
                    },
                }
            );

            // Act
            var result = RoleAssignmentService.ParseRolesToAssignOrRemove(
                roleNames,
                roles,
                teams,
                team,
                assignmentType,
                _tracingServiceMock.Object,
                out string errorMessage
            );

            // Assert
            if (assignmentType == AssignmnetType.Assign)
            {
                Assert.NotNull(result);
                Assert.Empty(result);
                Assert.Empty(errorMessage);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Empty(errorMessage);
            }
        }

        [Theory]
        [InlineData(AssignmnetType.Assign)]
        [InlineData(AssignmnetType.Remove)]
        public void ParseRolesToAssignOrRemove_ShouldReturnError_WhenNotAllRolesAreFound(
            AssignmnetType assignmentType
        )
        {
            // Arrange
            var roleNames = new[] { "Role1", "Role2", "Role3" };
            var roles = new EntityCollection(
                new List<Entity>
                {
                    new Entity("role")
                    {
                        Id = Guid.NewGuid(),
                        ["name"] = "Role1",
                        ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()),
                    },
                    new Entity("role")
                    {
                        Id = Guid.NewGuid(),
                        ["name"] = "Role2",
                        ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()),
                    },
                }
            );
            var team = new Entity("team")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Team1",
                ["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()),
            };
            var teams = new EntityCollection(new List<Entity>());

            // Act
            var result = RoleAssignmentService.ParseRolesToAssignOrRemove(
                roleNames,
                roles,
                teams,
                team,
                assignmentType,
                _tracingServiceMock.Object,
                out string errorMessage
            );

            // Assert
            Assert.Null(result);
            Assert.NotEmpty(errorMessage);
        }

        [Theory]
        [InlineData(AssignmnetType.Assign)]
        [InlineData(AssignmnetType.Remove)]
        public void ParseRolesToAssignOrRemove_ShouldReturnRoles_WhenRolesAreAssigned(
            AssignmnetType assignmentType
        )
        {
            var roleNames = new[] { "Role1", "Role2" };
            var role1Id = Guid.NewGuid();
            var role2Id = Guid.NewGuid();
            var businessUnitRef = new EntityReference("team", Guid.NewGuid());
            var role1 = new Entity("role", role1Id)
            {
                ["name"] = "Role1",
                ["businessunitid"] = businessUnitRef,
            };
            var role2 = new Entity("role", role2Id)
            {
                ["name"] = "Role2",
                ["businessunitid"] = businessUnitRef,
            };
            var roles = new EntityCollection(new List<Entity> { role1, role2 });
            var team = new Entity("team")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Team1",
                ["businessunitid"] = businessUnitRef,
            };
            var teams = new EntityCollection(
                new List<Entity>
                {
                    new Entity("team")
                    {
                        Id = team.Id,
                        ["teamid"] = team.Id,
                        ["role.roleid"] = new AliasedValue("role", "roleid", role1Id),
                        ["role.name"] = new AliasedValue("role", "name", "Role1"),
                        ["role.businessunitid"] = new AliasedValue(
                            "role",
                            "businessunitid",
                            businessUnitRef.Id
                        ),
                    },
                    new Entity("team")
                    {
                        Id = team.Id,
                        ["teamid"] = team.Id,
                        ["role.roleid"] = new AliasedValue("role", "roleid", role2Id),
                        ["role.name"] = new AliasedValue("role", "name", "Role2"),
                        ["role.businessunitid"] = new AliasedValue(
                            "role",
                            "businessunitid",
                            businessUnitRef.Id
                        ),
                    },
                }
            );

            // Act
            var result = RoleAssignmentService.ParseRolesToAssignOrRemove(
                roleNames,
                roles,
                teams,
                team,
                assignmentType,
                _tracingServiceMock.Object,
                out string errorMessage
            );

            // Assert
            if (assignmentType == AssignmnetType.Assign)
            {
                Assert.NotNull(result);
                Assert.Empty(result);
                Assert.Empty(errorMessage);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Empty(errorMessage);
            }
        }
    }
}
