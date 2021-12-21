using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace PostgresSamples.SqlGenerators
{
    class MagicTagQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        private readonly QuerySqlGeneratorDependencies _dependencies;
        private readonly INpgsqlOptions _npgsqlOptions;

        public MagicTagQuerySqlGeneratorFactory(
            QuerySqlGeneratorDependencies dependencies,
            INpgsqlOptions npgsqlOptions)
        {
            _dependencies = dependencies;
            _npgsqlOptions = npgsqlOptions;
        }

        public virtual QuerySqlGenerator Create()
            => new MagicTagPostgresQuerySqlGenerator(
                _dependencies,
                _npgsqlOptions.ReverseNullOrderingEnabled,
                _npgsqlOptions.PostgresVersion);
    }
}