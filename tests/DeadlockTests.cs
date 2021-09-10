using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using NUnit.Framework;
using Shouldly;

namespace PostgresSamples
{
    public class DeadlockTests
    {
        private NpgsqlConnection _connection = null!;
        private NpgsqlConnection _connection2 = null!;
        private CancellationTokenSource _cancel = null!;

        [SetUp]
        public async Task Setup()
        {
            _connection =
                new NpgsqlConnection("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=Test123!");
            _connection2 =
                new NpgsqlConnection("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=Test123!");
            _cancel = new CancellationTokenSource(10_000);

            await _connection.OpenAsync(_cancel.Token);
            await _connection2.OpenAsync(_cancel.Token);

            await _connection.ExecuteAsync(
                "DROP TABLE IF EXISTS public.test; CREATE TABLE public.test (id integer not null, name varchar(8) not null);",
                _cancel.Token);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _connection.DisposeAsync();
            await _connection2.DisposeAsync();
            _cancel.Dispose();
        }

        [Test]
        public async Task TestConcurrentSelectWithTransaction()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');INSERT INTO public.test (id, name) values (2, 'test2');",
                _cancel.Token);
            await using var transaction =
                await _connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, _cancel.Token);
            await using var transaction2 =
                await _connection2.BeginTransactionAsync(IsolationLevel.RepeatableRead, _cancel.Token);


            await Task.WhenAll(
                    _connection.ExecuteAsync(new CommandDefinition(
                        "select name from public.test where id = 1;select pg_sleep(0.2);select name from public.test where id = 2;",
                        transaction,
                        cancellationToken: _cancel.Token)),
                    _connection2.ExecuteAsync(new CommandDefinition(
                        "select name from public.test where id = 2;select pg_sleep(0.2);select name from public.test where id = 1;",
                        transaction2,
                        cancellationToken: _cancel.Token)))
                .CheckAnswerConcurrentSelectWithTransaction();
        }

        [Test]
        public async Task TestConcurrentSelect()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');INSERT INTO public.test (id, name) values (2, 'test2');",
                _cancel.Token);

            await Task.WhenAll(
                    _connection.ExecuteAsync(
                        "select name from public.test where id = 1;select pg_sleep(0.2);select name from public.test where id = 2;",
                        _cancel.Token),
                    _connection2.ExecuteAsync(
                        "select name from public.test where id = 2;select pg_sleep(0.2);select name from public.test where id = 1;",
                        _cancel.Token))
                .CheckAnswerConcurrentSelect();
        }

        [Test]
        public async Task TestConcurrentSelectForUpdate()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');INSERT INTO public.test (id, name) values (2, 'test2');",
                _cancel.Token);

            await Task.WhenAll(
                    _connection.ExecuteAsync(
                        "select name from public.test where id = 1 for update; select pg_sleep(0.2); update public.test set name = 'test1' where id = 2;",
                        _cancel.Token),
                    _connection2.ExecuteAsync(
                        "select name from public.test where id = 2 for update; select pg_sleep(0.2); update public.test set name = 'test2' where id = 1;",
                        _cancel.Token))
                .CheckAnswerConcurrentSelectForUpdate();
        }

        async Task ExecuteTransation(NpgsqlConnection c, string sql, IsolationLevel level)
        {
            await using var transaction =
                await c.BeginTransactionAsync(level, _cancel.Token);

            await c.ExecuteAsync(
                new CommandDefinition(sql, transaction, cancellationToken: _cancel.Token));
            await transaction.CommitAsync(_cancel.Token);
        }

        async Task RepeatableReadTransation(NpgsqlConnection c, string sql) =>
            await ExecuteTransation(c, sql, IsolationLevel.RepeatableRead); 

        async Task SerializableTransation(NpgsqlConnection c, string sql) =>
            await ExecuteTransation(c, sql, IsolationLevel.Serializable); 

        [Test]
        public async Task TestConcurrentRepeatableReadUpdate()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');",
                _cancel.Token);

            const string sql = "update public.test set name = 'test2' where id = 1; select pg_sleep(0.2);";

            await Task.WhenAll(RepeatableReadTransation(_connection, sql), RepeatableReadTransation(_connection2, sql))
                .CheckAnswerConcurrentRepeatableReadUpdate();
        }
        
        
        [Test]
        public async Task TestConcurrentRepeatableReadUpdateLock()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');",
                _cancel.Token);

            const string sql = "lock table public.test in share row exclusive mode; update public.test set name = 'test2' where id = 1; select pg_sleep(0.2);";

            await Task.WhenAll(RepeatableReadTransation(_connection, sql), RepeatableReadTransation(_connection2, sql))
                .CheckAnswerConcurrentRepeatableReadUpdateLock();
        }

        
        [Test]
        public async Task TestSerializationAnomaly()
        {
            await _connection.ExecuteAsync(
                "DROP TABLE IF EXISTS public.mytab; CREATE TABLE public.mytab (class integer not null, value integer not null);",
                _cancel.Token);

            await _connection.ExecuteAsync(
                @"INSERT INTO public.mytab (class, value) values (1, 10);
                     INSERT INTO public.mytab (class, value) values (1, 20);
                     INSERT INTO public.mytab (class, value) values (2, 100);
                     INSERT INTO public.mytab (class, value) values (2, 200);",
                _cancel.Token);

            await Task.WhenAll(
                    SerializableTransation(_connection,
                    "INSERT INTO public.mytab (class, value) select 2, sum(value) from public.mytab, pg_sleep(0.2) where class = 1"),
                    SerializableTransation(_connection2,
                    "INSERT INTO public.mytab (class, value) select 1, sum(value) from public.mytab, pg_sleep(0.2) where class = 2"))
                .CheckAnswerSerializationAnomaly();
        }


        [Test]
        public async Task TestConcurrentSelectSeparateRequests()
        {
            await _connection.ExecuteAsync(
                "INSERT INTO public.test (id, name) values (1, 'test1');INSERT INTO public.test (id, name) values (2, 'test2');",
                _cancel.Token);

            async Task Transation1()
            {
                await using var transaction =
                    await _connection.BeginTransactionAsync(_cancel.Token);

                await _connection.ExecuteAsync(new CommandDefinition(
                    "select name from public.test where id = 1 for update;",
                    transaction,
                    cancellationToken: _cancel.Token));
                await Task.Delay(500, _cancel.Token);
                await _connection.ExecuteAsync(new CommandDefinition(
                    "select name from public.test where id = 2 for update;",
                    transaction,
                    cancellationToken: _cancel.Token));
            }

            async Task Transation2()
            {
                await using var transaction =
                    await _connection2.BeginTransactionAsync(_cancel.Token);

                await _connection2.ExecuteAsync(new CommandDefinition(
                    "select name from public.test where id = 2 for update;",
                    transaction,
                    cancellationToken: _cancel.Token));
                await Task.Delay(500, _cancel.Token);
                await _connection2.ExecuteAsync(new CommandDefinition(
                    "select name from public.test where id = 1 for update;",
                    transaction,
                    cancellationToken: _cancel.Token));
            }

            await Task.WhenAll(Transation1(), Transation2())
                .CheckAnswerConcurrentSelectSeparateRequests();
        }
    }
}