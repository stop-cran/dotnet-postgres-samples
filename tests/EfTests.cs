using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
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
                .UseQueryHints()
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
    }
}