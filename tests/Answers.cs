using System.Threading.Tasks;
using Npgsql;
using Shouldly;

namespace PostgresSamples
{
    public static class Answers
    {
        public static async Task CheckAnswerConcurrentSelect(this Task task)
        {
            await task;
        }

        public static async Task CheckAnswerConcurrentSelectWithTransaction(this Task task)
        {
            await task;
        }

        public static async Task CheckAnswerConcurrentSelectForUpdate(this Task task)
        {
            var ex = await task.ShouldThrowAsync<PostgresException>();
            
            ex.Severity.ShouldBe("ERROR");
            ex.SqlState.ShouldBe("40P01");
        }
        
        public static async Task CheckAnswerConcurrentRepeatableReadUpdate(this Task task)
        {
            var ex = await task.ShouldThrowAsync<PostgresException>();
            
            ex.Severity.ShouldBe("ERROR");
            ex.SqlState.ShouldBe("40001");
        }

        public static async Task CheckAnswerConcurrentRepeatableReadUpdateLock(this Task task)
        {
            await task;
        }

        public static async Task CheckAnswerConcurrentSelectSeparateRequests(this Task task)
        {
            var ex = await task.ShouldThrowAsync<PostgresException>();
            
            ex.Severity.ShouldBe("ERROR");
            ex.SqlState.ShouldBe("40P01");
        }
    }
}