using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace PostgresSamples.SqlGenerators
{
    public static class ForUpdateDbContextOptionsBuilderExtensions
    {
        public static IQueryable<T> ForUpdate<T>(this IQueryable<T> source) =>
            source.TagWith(MagicTagPostgresQuerySqlGenerator.ForUpdateTag);

        public static DbContextOptionsBuilder UseLockModifiers(this DbContextOptionsBuilder builder) =>
            builder.ReplaceService<IQuerySqlGeneratorFactory, NpgsqlQuerySqlGeneratorFactory, MagicTagQuerySqlGeneratorFactory>();
    }
}