using AICareerCoach.BLL.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class EmbeddingServiceTests
    {
        [Fact]
        public void Constructor_Throws_WhenTokenMissing()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var logger = new Mock<ILogger<EmbeddingService>>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new EmbeddingService(config, logger.Object));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_EmptyText_ReturnsEmptyArray()
        {
            // Arrange
            var settings = new System.Collections.Generic.Dictionary<string, string> { { "GitHub:Token", "fake" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var logger = new Mock<ILogger<EmbeddingService>>();
            var svc = new EmbeddingService(config, logger.Object);

            // Act
            var result = await svc.GenerateEmbeddingAsync(string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
