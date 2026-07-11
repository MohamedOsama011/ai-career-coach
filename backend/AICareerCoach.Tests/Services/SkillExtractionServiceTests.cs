using AICareerCoach.BLL.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class SkillExtractionServiceTests
    {
        [Fact]
        public void Constructor_Throws_WhenTokenMissing()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var logger = new Mock<ILogger<SkillExtractionService>>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new SkillExtractionService(config, logger.Object));
        }

        [Fact]
        public async Task ExtractSkillsBatchAsync_EmptyJobs_ReturnsEmpty()
        {
            // Arrange
            var settings = new Dictionary<string, string> { { "GitHub:Token", "fake" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var logger = new Mock<ILogger<SkillExtractionService>>();
            var svc = new SkillExtractionService(config, logger.Object);

            // Act
            var result = await svc.ExtractSkillsBatchAsync(new List<(string, string, string)>(), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
