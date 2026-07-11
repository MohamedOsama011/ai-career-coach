using AICareerCoach.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.Tests
{
    public static class TestHelpers
    {
        public static AICareerCoachDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AICareerCoachDbContext(options);
        }

        public static AICareerCoachDbContext CreateSqlServerTestContext(string? dbName = null)
        {
            // Uses LocalDB for tests. Ensure LocalDB (MSSQLLocalDB) is available on the machine running tests.
            var name = dbName ?? Guid.NewGuid().ToString("N");
            var connectionString = $"Server=(localdb)\\mssqllocaldb;Database=Test_{name};Trusted_Connection=True;MultipleActiveResultSets=true";

            var optionsBuilder = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseSqlServer(connectionString);

            var context = new AICareerCoachDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
