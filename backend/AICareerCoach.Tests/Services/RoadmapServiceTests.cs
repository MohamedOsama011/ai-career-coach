using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class RoadmapServiceTests
    {
        private readonly Mock<IRoadmapRepository> _roadmapRepoMock;
        private readonly Mock<IEmbeddingService> _embedMock;
        private readonly AICareerCoachDbContext _context;
        private readonly RoadmapService _service;

        public RoadmapServiceTests()
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            _context = new AICareerCoachDbContext(options);
            _roadmapRepoMock = new Mock<IRoadmapRepository>();
            _embedMock = new Mock<IEmbeddingService>();

            _service = new RoadmapService(_roadmapRepoMock.Object, _context, _embedMock.Object);
        }

        [Fact]
        public async Task CreateAsync_AddsRoadmap()
        {
            _roadmapRepoMock.Setup(x => x.AddAsync(It.IsAny<Roadmap>())).ReturnsAsync((Roadmap r) => { r.Id = 5; return r; });

            var dto = new CreateRoadmapDto { Track = "t", Title = "tt", Description = "d", OrderIndex = 1, Steps = new List<CreateRoadmapStepDto>() };

            var res = await _service.CreateAsync(dto);

            res.Should().NotBeNull();
            res.Id.Should().Be(5);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_Throws()
        {
            _roadmapRepoMock.Setup(x => x.GetByIdWithStepsAsync(It.IsAny<int>())).ReturnsAsync((Roadmap?)null);

            await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(() => _service.GetByIdAsync(1));
        }
    }
}
