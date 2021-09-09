using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PostgresSamples.Hints
{
    public static class ForUpdateDbContextOptionsBuilderExtensions
    {
        public static IQueryable<T> ForUpdate<T>(this IQueryable<T> source) =>
            source.TagWith(ForUpdateInterceptor.QueryTag);
        
        public static DbContextOptionsBuilder UseLockModifiers(this DbContextOptionsBuilder builder) =>
            builder.AddInterceptors(new ForUpdateInterceptor());
    }
}