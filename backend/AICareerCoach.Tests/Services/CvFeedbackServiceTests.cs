using AICareerCoach.BLL.Services.AI;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class CvFeedbackServiceTests
    {
        [Fact]
        public async Task GetFeedbackAsync_NoCv_Throws()
        {
            // Arrange
            using var context = TestHelpers.CreateInMemoryContext();
            var pdf = new Mock<AICareerCoach.BLL.Services.Interfaces.IPdfExtractorService>();
            var llm = new Mock<ILlmService>();
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(System.IO.Directory.GetCurrentDirectory());
            var logger = new Mock<ILogger<CvFeedbackService>>();

            var svc = new CvFeedbackService(context, pdf.Object, llm.Object, env.Object, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetFeedbackAsync("u1"));
        }

        [Fact]
        public async Task GetFeedbackAsync_UsesCache_WhenAvailable()
        {
            // Arrange
            using var context = TestHelpers.CreateInMemoryContext();

            var cv = new CV { CVId = 1, UserId = "u1", ExtractedData = "text" };
            context.CVs.Add(cv);

            var cached = new AiFeedbackCache { Id = 1, UserId = "u1", CvHash = "1CB251EC0D568DE6A929B520C4AED8D1", FeedbackJson = JsonSerializer.Serialize(new AICareerCoach.BLL.DTOs.CV.CvFeedbackDto { OverallScore = 10 }) };
            context.AiFeedbackCaches.Add(cached);
            context.SaveChanges();

            var pdf = new Mock<AICareerCoach.BLL.Services.Interfaces.IPdfExtractorService>();
            var llm = new Mock<ILlmService>();
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(System.IO.Directory.GetCurrentDirectory());
            var logger = new Mock<ILogger<CvFeedbackService>>();

            var svc = new CvFeedbackService(context, pdf.Object, llm.Object, env.Object, logger.Object);

            // Act
            var res = await svc.GetFeedbackAsync("u1");

            // Assert
            res.Should().NotBeNull();
            res.FromCache.Should().BeTrue();
        }

        [Fact]
        public async Task GetFeedbackAsync_CallsLlmAndCaches_WhenNoCache()
        {
            // Arrange
            using var context = TestHelpers.CreateInMemoryContext();

            var cv = new CV { CVId = 2, UserId = "u2", ExtractedData = "text to analyze" };
            context.CVs.Add(cv);
            context.SaveChanges();

            var pdf = new Mock<AICareerCoach.BLL.Services.Interfaces.IPdfExtractorService>();
            var llm = new Mock<ILlmService>();
            llm.Setup(x => x.GetCvFeedbackAsync(It.IsAny<string>())).ReturnsAsync(new AICareerCoach.BLL.DTOs.CV.CvFeedbackDto { OverallScore = 20 });
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(System.IO.Directory.GetCurrentDirectory());
            var logger = new Mock<ILogger<CvFeedbackService>>();

            var svc = new CvFeedbackService(context, pdf.Object, llm.Object, env.Object, logger.Object);

            // Act
            var res = await svc.GetFeedbackAsync("u2");

            // Assert
            res.Should().NotBeNull();
            res.FromCache.Should().BeFalse();
            var caches = await context.AiFeedbackCaches.CountAsync(c => c.UserId == "u2");
            caches.Should().Be(1);
        }
    }
}
