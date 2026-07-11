using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class JobServiceTests
    {
        private readonly Mock<IJobRepository> _jobRepoMock;
        private readonly Mock<IEmbeddingService> _embedMock;
        private readonly AICareerCoachDbContext _context;
        private readonly JobService _service;

        public JobServiceTests()
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            _context = new AICareerCoachDbContext(options);

            _jobRepoMock = new Mock<IJobRepository>();
            _embedMock = new Mock<IEmbeddingService>();

            _service = new JobService(_jobRepoMock.Object, _embedMock.Object, _context);
        }

        [Fact]
        public async Task CreateAsync_AddsJobAndEmbedding()
        {
            _jobRepoMock.Setup(x => x.AddAsync(It.IsAny<Job>()))
                .ReturnsAsync((Job j) => { j.Id = 100; return j; });

            _embedMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new float[] { 0.1f, 0.2f });

            var dto = new CreateJobDto { Title = "T", Company = "C", Description = "D", RequiredSkills = new List<string> { "s" }, Location = "L", Salary = 1 };

            var res = await _service.CreateAsync(dto);

            res.Should().NotBeNull();
            res.Id.Should().Be(100);

            var embeddings = await _context.JobEmbeddings.ToListAsync();
            embeddings.Should().HaveCount(1);
            embeddings[0].JobId.Should().Be(100);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_Throws()
        {
            _jobRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Job?)null);

            await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(() => _service.GetByIdAsync(999));
        }
    }
}
