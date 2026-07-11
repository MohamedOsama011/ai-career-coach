using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<AICareerCoach.DAL.Models.User>> _userManagerMock;
        private readonly AICareerCoachDbContext _context;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            _context = new AICareerCoachDbContext(options);

            var store = new Mock<IUserStore<AICareerCoach.DAL.Models.User>>();
            _userManagerMock = new Mock<UserManager<AICareerCoach.DAL.Models.User>>(store.Object, null, null, null, null, null, null, null, null);

            _service = new UserService(_userManagerMock.Object, _context);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsProfile()
        {
            var user = new AICareerCoach.DAL.Models.User { Id = "u1", FullName = "FN", Email = "e@x.com" };
            _context.Users.Add(user);
            _context.CVs.Add(new CV { CVId = 1, UserId = "u1" });
            _context.SaveChanges();

            _userManagerMock.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var profile = await _service.GetProfileAsync("u1");

            profile.Should().NotBeNull();
            profile.CvCount.Should().Be(1);
            profile.Roles.Should().Contain("Admin");
        }

        [Fact]
        public async Task GetProfileAsync_WhenNotFound_Throws()
        {
            _userManagerMock.Setup(x => x.FindByIdAsync("nope")).ReturnsAsync((AICareerCoach.DAL.Models.User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetProfileAsync("nope"));
        }
    }
}
