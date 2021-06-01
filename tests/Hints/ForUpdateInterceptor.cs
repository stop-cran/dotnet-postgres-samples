using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PostgresSamples.Hints
{
    public class ForUpdateInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            PatchCommandText(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            PatchCommandText(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<object> result)
        {
            PatchCommandText(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            PatchCommandText(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public const string QueryTag = "Use for update";
        public const string QueryPrefix = "-- Use for update";

        private void PatchCommandText(DbCommand command)
        {
            if (command.CommandText.StartsWith(QueryPrefix, StringComparison.Ordinal))
            {
                int index = command.CommandText.IndexOfAny(Environment.NewLine.ToCharArray(), QueryPrefix.Length);
                command.CommandText = command.CommandText[index..].Trim() + Environment.NewLine + "FOR UPDATE";
            }
        }
    }
}