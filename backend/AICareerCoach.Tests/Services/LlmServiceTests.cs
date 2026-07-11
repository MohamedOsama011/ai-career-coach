using AICareerCoach.BLL.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class LlmServiceTests
    {
        [Fact]
        public void Constructor_Throws_WhenTokenMissing()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var logger = new Mock<ILogger<LlmService>>();

            // Assert
            Assert.Throws<InvalidOperationException>(() => new LlmService(config, logger.Object));
        }
    }
}
