using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace PostgresSamples.SqlGenerators
{
    class MagicTagPostgresQuerySqlGenerator : NpgsqlQuerySqlGenerator
    {
        public static readonly string ForUpdateTag = "FOR-UPDATE-" + Guid.NewGuid();
        private bool _forUpdate;

        public MagicTagPostgresQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies,
            bool reverseNullOrderingEnabled, Version postgresVersion) : base(dependencies, reverseNullOrderingEnabled,
            postgresVersion)
        {
        }

        protected override void GenerateTagsHeaderComment(SelectExpression selectExpression)
        {
            if (selectExpression.Tags.Contains(ForUpdateTag))
            {
                selectExpression.Tags.Remove(ForUpdateTag);
                _forUpdate = true;
            }

            base.GenerateTagsHeaderComment(selectExpression);
        }

        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            var result = base.VisitSelect(selectExpression);

            if (_forUpdate)
                Sql.AppendLine().Append("FOR UPDATE");

            return result;
        }
    }
}