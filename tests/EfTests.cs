using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using NUnit.Framework;
using PostgresSamples.Hints;
using PostgresSamples.Model;
using Shouldly;

namespace PostgresSamples
{
    public class EfTests
    {
        private SomeDbContext dbContext = null!;
        private CancellationTokenSource _cancel = null!;

        [SetUp]
        public async Task Setup()
        {
            _cancel = new CancellationTokenSource(10_000);

            using (var connection =
                new NpgsqlConnection("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=Test123!"))
            {
                await connection.OpenAsync(_cancel.Token);

                await connection.ExecuteAsync(
                    "DROP TABLE IF EXISTS public.\"SomeEntity\"; CREATE TABLE public.\"SomeEntity\" (\"Id\" integer not null, \"Name\" varchar(8) not null);" +
                    "DROP TABLE IF EXISTS public.\"Child1Entity\"; DROP TABLE IF EXISTS public.\"Child2Entity\"; DROP TABLE IF EXISTS public.\"ParentEntity\";" +
                    "CREATE TABLE public.\"ParentEntity\" (\"Id\" integer not null primary key, \"Name\" varchar(8) not null);" +
                    "CREATE TABLE public.\"Child1Entity\" (\"Id\" integer not null, \"Modified\" timestamp not null, \"ParentId\" integer not null, FOREIGN KEY(\"ParentId\") REFERENCES public.\"ParentEntity\"(\"Id\"));" +
                    "CREATE TABLE public.\"Child2Entity\" (\"Id\" integer not null, \"Modified\" timestamp not null, \"ParentId\" integer not null, FOREIGN KEY(\"ParentId\") REFERENCES public.\"ParentEntity\"(\"Id\"));" +
                    "INSERT INTO public.\"ParentEntity\" (\"Id\", \"Name\") values (1, 'test1');" +
                    "INSERT INTO public.\"Child1Entity\" (\"Id\", \"ParentId\", \"Modified\") values (1, 1, CURRENT_TIMESTAMP);" +
                    "INSERT INTO public.\"Child2Entity\" (\"Id\", \"ParentId\", \"Modified\") values (1, 1, CURRENT_TIMESTAMP);",
                    _cancel.Token);
            }

            dbContext = new SomeDbContext(new DbContextOptionsBuilder<SomeDbContext>()
                .UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=Test123!")
                .UseLockModifiers()
                .UseLoggerFactory(LoggerFactory.Create(builder =>
                    builder
                        .SetMinimumLevel(LogLevel.Error)
                        .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information)
                        .AddNUnit()))
                .Options);
        }

        [TearDown]
        public async Task TearDown()
        {
            await dbContext.DisposeAsync();
        }

        [Test]
        public async Task TestForUpdate()
        {
            var result = await dbContext
                .ParentEntities
                .Where(e => e.Id == 1)
                .ForUpdate()
                .ToListAsync(_cancel.Token);

            result.ShouldHaveSingleItem();
        }


        [Test]
        public async Task TestTransaction()
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, _cancel.Token);
            var entities = await dbContext.SomeEntities.ToListAsync(_cancel.Token);
            
            entities.ShouldBeEmpty();
            dbContext.SomeEntities.Add(new SomeEntity
            {
                Id = 1,
                Name = "srthsrt"
            });

            await dbContext.SaveChangesAsync(_cancel.Token);
            await transaction.CommitAsync(_cancel.Token);
        }

        [Test]
        public async Task TestSingleQuery()
        {
            var result = await dbContext
                .ParentEntities
                .Include(p => p.Children1)
                .Include(p => p.Children2)
                .Where(e => e.Id == 1)
                .ToListAsync(_cancel.Token);

            var item = result.ShouldHaveSingleItem();

            item.Children1.ShouldHaveSingleItem();
            item.Children2.ShouldHaveSingleItem();
        }

        [Test]
        public async Task TestSplitQuery()
        {
            var result = await dbContext
                .ParentEntities
                .Include(p => p.Children1)
                .Include(p => p.Children2)
                .AsSplitQuery()
                .Where(e => e.Id == 1)
                .ToListAsync(_cancel.Token);

            var item = result.ShouldHaveSingleItem();

            item.Children1.ShouldHaveSingleItem();
            item.Children2.ShouldHaveSingleItem();
        }


        [Test]
        public async Task TestTake()
        {
            var result = await dbContext
                .ParentEntities
                .AsNoTracking()
                .Take(10)
                .ToListAsync(_cancel.Token);

            var item = result.ShouldHaveSingleItem();
        }

        [Test]
        public async Task TestChangeTracker()
        {
            var result = await dbContext
                .ParentEntities
                .FirstAsync(_cancel.Token);
            dbContext.ChangeTracker.HasChanges().ShouldBeFalse();
            result.Name = "another name";
            dbContext.ChangeTracker.HasChanges().ShouldBeTrue();
        }


        [Test]
        public async Task TestQueryCaching()
        {
            async Task<IReadOnlyList<ParentEntity>> TestQueryCaching(params int[] ids) =>
                await dbContext
                    .ParentEntities
                    .AsNoTracking()
                    .Where(e => ids.Contains(e.Id))
                    .ForUpdate()
                    .ToListAsync(_cancel.Token);

            var cache = dbContext.GetService<IMemoryCache>().ShouldBeOfType<MemoryCache>();
            var cnt = cache.Count;

            await TestQueryCaching(1, 5, 9);
            cache.Count.ShouldBeGreaterThan(cnt);
            cnt = cache.Count;
            await TestQueryCaching(2, 6, 8);
            cache.Count.ShouldBe(cnt);
        }
    }
}