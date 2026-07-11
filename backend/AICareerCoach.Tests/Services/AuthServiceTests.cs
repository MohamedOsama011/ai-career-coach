using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task RegisterAsync_Throws_WhenEmailExists()
        {
            // Arrange
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "1", Email = "a@b.com" });

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var svc = new AuthService(userManager.Object, config, roleManager.Object, context);

            var dto = new RegisterDto { Email = "a@b.com", Password = "P@ssw0rd", FullName = "fn" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => svc.RegisterAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_Throws_WhenUserNotFound()
        {
            // Arrange
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var svc = new AuthService(userManager.Object, config, roleManager.Object, context);

            var dto = new LoginDto { Email = "no@user.com", Password = "x" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => svc.LoginAsync(dto));
        }

        [Fact]
        public async Task Logout_MarksTokenRevoked_WhenTokenExists()
        {
            // Arrange
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new AICareerCoachDbContext(options);

            // seed a refresh token (RefreshToken.Id is int PK)
            context.RefreshTokens.Add(new AICareerCoach.DAL.Entities.RefreshToken { Id = 1, Token = "t1", Userid = "1", IsRevoked = false, Expirydate = DateTime.UtcNow.AddDays(1) });
            context.SaveChanges();

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var svc = new AuthService(userManager.Object, config, roleManager.Object, context);

            var dto = new Refreshtokendto { token = "t1" };

            // Act
            var res = await svc.Logout(dto);

            // Assert
            Assert.True(res.Success);
            var token = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == "t1");
            Assert.True(token!.IsRevoked);
        }
    }
}
