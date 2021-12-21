using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PostgresSamples.Model;
using PostgresSamples.SqlGenerators;
using Shouldly;

namespace PostgresSamples
{
    public class EfSqlGenerationTests
    {
        private SomeDbContext dbContext = null!;

        [SetUp]
        public async Task Setup()
        {
            dbContext = new SomeDbContext(new DbContextOptionsBuilder<SomeDbContext>()
                .UseNpgsql("Server=localhost")
                .UseLockModifiers()
                .Options);
        }

        [TearDown]
        public async Task TearDown()
        {
            await dbContext.DisposeAsync();
        }

        [Test]
        public void TestForUpdate()
        {
            var query = dbContext
                .ParentEntities
                .Where(e => e.Id == 1)
                .ForUpdate()
                .ToQueryString();

            query.ShouldEndWith("FOR UPDATE");
        }
   }
}