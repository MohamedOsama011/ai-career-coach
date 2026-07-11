using AICareerCoach.BLL.Services.AI;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class JobRecommendationServiceTests
    {
        [Fact]
        public async Task IndexJobsAsync_GeneratesEmbeddings_ForJobsWithoutEmbedding()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);
            context.Jobs.Add(new Job { Id = 1, Title = "T", Company = "C", Description = "D", RequiredSkills = JsonSerializer.Serialize(new List<string> { "s" }) });
            context.SaveChanges();

            var embedMock = new Mock<IEmbeddingService>();
            embedMock.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f, 0.2f });
            var llmExplMock = new Mock<ILlmExplanationService>();
            var gateMock = new Mock<AICareerCoach.BLL.Interfaces.ISubscriptionGateService>();
            var logger = new Mock<ILogger<JobRecommendationService>>();

            var svc = new JobRecommendationService(context, embedMock.Object, llmExplMock.Object, gateMock.Object, logger.Object);

            // Act
            await svc.IndexJobsAsync();

            // Assert
            var embeddings = await context.JobEmbeddings.ToListAsync();
            embeddings.Should().HaveCount(1);
            embeddings[0].JobId.Should().Be(1);
        }

        [Fact]
        public async Task GetRecommendationsAsync_WhenNoCv_Throws()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);

            var embedMock = new Mock<IEmbeddingService>();
            var llmExplMock = new Mock<ILlmExplanationService>();
            var gateMock = new Mock<AICareerCoach.BLL.Interfaces.ISubscriptionGateService>();
            var logger = new Mock<ILogger<JobRecommendationService>>();

            var svc = new JobRecommendationService(context, embedMock.Object, llmExplMock.Object, gateMock.Object, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetRecommendationsAsync("u1"));
        }

        [Fact]
        public async Task GetRecommendationsAsync_ComputesAndCaches_WhenJobsExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);

            var cv = new CV { CVId = 10, UserId = "u1", ExtractedData = "cv text" };
            context.CVs.Add(cv);

            var job = new Job { Id = 5, Title = "Dev", Company = "C", Description = "D", RequiredSkills = JsonSerializer.Serialize(new List<string> { "s" }) };
            context.Jobs.Add(job);
            // add job embedding
            context.JobEmbeddings.Add(new JobEmbedding { Id = 1, JobId = 5, Embedding = new float[] { 0.1f, 0.2f }, ComputedAt = DateTime.UtcNow });
            context.SaveChanges();

            var embedMock = new Mock<IEmbeddingService>();
            embedMock.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f, 0.2f });
            var aiExpl = new Mock<ILlmExplanationService>();
            aiExpl.Setup(a => a.GenerateExplanationsAsync(It.IsAny<string>(), It.IsAny<List<Job>>()))
                .ReturnsAsync(new Dictionary<int, AICareerCoach.BLL.DTOs.Job.JobExplanationDto>());
            var gateMock = new Mock<AICareerCoach.BLL.Interfaces.ISubscriptionGateService>();
            gateMock.Setup(g => g.HasActiveSubscriptionAsync(It.IsAny<string>())).ReturnsAsync(false);
            var logger = new Mock<ILogger<JobRecommendationService>>();

            var svc = new JobRecommendationService(context, embedMock.Object, aiExpl.Object, gateMock.Object, logger.Object);

            // Act
            var res = await svc.GetRecommendationsAsync("u1");

            // Assert
            res.Should().NotBeNull();
            res.Recommendations.Should().NotBeNull();
            var cached = await context.JobRecommendationCaches.CountAsync(c => c.UserId == "u1");
            cached.Should().Be(1);
        }
    }
}
