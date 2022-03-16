// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Repositories;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Repositories
{
    public class InformaticsGatewayRepositoryTest : IClassFixture<DatabaseFixture>
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        public DatabaseFixture Fixture { get; }

        public InformaticsGatewayRepositoryTest(DatabaseFixture fixture)
        {
            Fixture = fixture;

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(InformaticsGatewayContext)))
                .Returns(Fixture.DbContext);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
        }

        [Fact(DisplayName = "Constructor")]
        public void Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new InformaticsGatewayRepository<SourceApplicationEntity>(null));
        }

        [RetryFact(5, 250, DisplayName = "AsQueryable - returns IQueryable")]
        public void AsQueryable()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var result = repo.AsQueryable();

            Assert.True(result is IQueryable<SourceApplicationEntity>);
        }

        [RetryFact(5, 250, DisplayName = "AsQueryable - returns List")]
        public async Task ToListAsync()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var result = await repo.ToListAsync();

            Assert.True(result is List<SourceApplicationEntity>);
        }

        [RetryFact(5, 250, DisplayName = "FindAsync - lookup by key")]
        public async Task FindAsync()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var result = await repo.FindAsync("AET5");

            Assert.NotNull(result);
            Assert.Equal("AET5", result.Name);
            Assert.Equal("AET5", result.AeTitle);
            Assert.Equal("5.5.5.5", result.HostIp);
        }

        [RetryFact(5, 250, DisplayName = "Update")]
        public async Task Update()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var key = "AET2";
            var result = await repo.FindAsync(key);
            Assert.NotNull(result);

            result.HostIp = "20.20.20.20";
            repo.Update(result);
            await repo.SaveChangesAsync();
            var updated = await repo.FindAsync(key);

            Assert.Equal(result, updated);
        }

        [RetryFact(5, 250, DisplayName = "Remove")]
        public async Task Remove()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            for (int i = 8; i <= 10; i++)
            {
                var key = $"AET{i}";
                var result = await repo.FindAsync(key);
                repo.Remove(result);
                await repo.SaveChangesAsync();
            }
        }

        [RetryFact(5, 250, DisplayName = "AddAsync")]
        public async Task AddAsync()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            for (int i = 11; i <= 20; i++)
            {
                await repo.AddAsync(new SourceApplicationEntity
                {
                    Name = $"AET{i}",
                    AeTitle = $"AET{i}",
                    HostIp = $"Server{i}",
                });
            }
            await repo.SaveChangesAsync();

            for (int i = 11; i <= 20; i++)
            {
                var notNull = await repo.FindAsync($"AET{i}");
                Assert.NotNull(notNull);
            }
        }

        [RetryFact(5, 250, DisplayName = "FirstOrDefault")]
        public void FirstOrDefault()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var exists = repo.FirstOrDefault(p => p.HostIp == "1.1.1.1");
            Assert.NotNull(exists);

            var doesNotexist = repo.FirstOrDefault(p => p.AeTitle == "ABC");
            Assert.Null(doesNotexist);
        }

        [RetryFact(5, 250, DisplayName = "Detach")]
        public async Task Detach()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var toBeModified = await repo.FindAsync("AET1");
            repo.Detach(toBeModified);
            toBeModified.Name = "TEST";

            var existing = await repo.FindAsync("AET1");
            Assert.NotEqual(existing.Name, toBeModified.Name);
        }

        [RetryFact(5, 250, DisplayName = "Any")]
        public void Any()
        {
            var repo = new InformaticsGatewayRepository<SourceApplicationEntity>(_serviceScopeFactory.Object);

            var all = repo.Any(p => p.Name.StartsWith("AET"));
            Assert.True(all);
        }
    }

    public class DatabaseFixture : IDisposable
    {
        private bool _disposedValue;

        public InformaticsGatewayContext DbContext { get; }

        public DatabaseFixture()
        {
            DbContext = GetDatabaseContext();
        }

        public static InformaticsGatewayContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<InformaticsGatewayContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var databaseContext = new InformaticsGatewayContext(options);
            databaseContext.Database.EnsureDeleted();
            databaseContext.Database.EnsureCreated();
            if (!databaseContext.SourceApplicationEntities.Any())
            {
                for (int i = 1; i <= 10; i++)
                {
                    databaseContext.SourceApplicationEntities.Add(
                        new SourceApplicationEntity
                        {
                            Name = $"AET{i}",
                            AeTitle = $"AET{i}",
                            HostIp = $"{i}.{i}.{i}.{i}"
                        });
                }
            }
            databaseContext.SaveChanges();
            return databaseContext;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    DbContext.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
