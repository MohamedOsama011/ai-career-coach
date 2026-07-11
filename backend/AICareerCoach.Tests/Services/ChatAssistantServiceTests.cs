using AICareerCoach.BLL.Services.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class ChatAssistantServiceTests
    {
        [Fact]
        public async Task CreateSessionAsync_CreatesSession()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);
            var toolExec = new Mock<AICareerCoach.BLL.Interfaces.AI.IAgentToolExecutor>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string,string> { { "GitHub:Token", "fake" } }).Build();
            var logger = new Mock<ILogger<ChatAssistantService>>();

            var svc = new ChatAssistantService(context, toolExec.Object, config, logger.Object);

            // Act
            var dto = await svc.CreateSessionAsync("user1");

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("user1", context.ChatSessions.Find(dto.Id)!.UserId);
        }

        [Fact]
        public async Task GetSessionAsync_WhenNotFound_Throws()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);
            var toolExec = new Mock<AICareerCoach.BLL.Interfaces.AI.IAgentToolExecutor>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string,string> { { "GitHub:Token", "fake" } }).Build();
            var logger = new Mock<ILogger<ChatAssistantService>>();

            var svc = new ChatAssistantService(context, toolExec.Object, config, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetSessionAsync("u", 123));
        }

        [Fact]
        public async Task GetUserSessionsAsync_ReturnsSessions()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);
            context.ChatSessions.Add(new ChatSession { UserId = "u1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.SaveChanges();

            var toolExec = new Mock<AICareerCoach.BLL.Interfaces.AI.IAgentToolExecutor>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string,string> { { "GitHub:Token", "fake" } }).Build();
            var logger = new Mock<ILogger<ChatAssistantService>>();

            var svc = new ChatAssistantService(context, toolExec.Object, config, logger.Object);

            // Act
            var list = await svc.GetUserSessionsAsync("u1");

            // Assert
            Assert.NotNull(list);
            Assert.Single(list);
        }
    }
}
